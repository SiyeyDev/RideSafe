using System;

namespace RideSafe.TaskSequence
{
    /// <summary>
    /// The engine. Walks a <see cref="TaskSequenceSO"/> step by step through
    /// Prepare -> Present -> WaitForInteraction -> Validate -> Feedback -> Complete -> Cleanup.
    /// <para>
    /// It is a plain object driven by <see cref="Tick"/>, not a MonoBehaviour and not an
    /// IStepCommand. Keeping it independent is what preserves the full lifecycle: the
    /// CommandSystem adapter lives outside and only flattens the FINAL result to a bool.
    /// </para>
    /// <para>
    /// There are no module branches in here. The runner knows Sequence, Step, Entity,
    /// Validator and Context, and nothing else.
    /// </para>
    /// </summary>
    public sealed class TaskSequenceRunner
    {
        private readonly TaskContextService _entities;
        private readonly Func<string, string> _contextLookup;

        private TaskSequenceSO _sequence;
        private int _stepIndex = -1;
        private TaskStepData _step;
        private TaskStepContext _context;
        private ITaskValidator _validator;
        private float _phaseElapsed;
        private bool _stepFailed;

        public TaskSequenceStatus Status { get; private set; } = TaskSequenceStatus.Idle;
        public TaskStepPhase Phase { get; private set; } = TaskStepPhase.None;
        public string SequenceId => _sequence != null ? _sequence.SequenceId : null;
        public string StepId => _step != null ? _step.StepId : null;
        public int StepIndex => _stepIndex;
        public int StepCount => _sequence != null ? _sequence.StepCount : 0;
        public bool IsRunning => Status == TaskSequenceStatus.Running;

        public event Action<TaskSequenceRunner> SequenceStarted;
        public event Action<TaskSequenceRunner, TaskStepData> StepStarted;
        public event Action<TaskSequenceRunner, TaskStepData, TaskStepPhase> StepPhaseChanged;
        public event Action<TaskSequenceRunner, TaskStepData, bool> StepCompleted;
        public event Action<TaskSequenceRunner, TaskSequenceStatus> SequenceFinished;

        public TaskSequenceRunner(TaskContextService entities, Func<string, string> contextLookup = null)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));
            _entities = entities;
            _contextLookup = contextLookup;
        }

        #region Control

        public bool Start(TaskSequenceSO sequence)
        {
            if (sequence == null)
            {
                TaskLog.Error(null, null, "Start called with a null sequence.");
                return false;
            }
            if (IsRunning)
            {
                TaskLog.Error(sequence.SequenceId, null,
                    "Cannot start: runner is already running " + SequenceId + ". Abort it first.");
                return false;
            }
            if (sequence.StepCount == 0)
            {
                TaskLog.Error(sequence.SequenceId, null, "Sequence has no steps.");
                return false;
            }

            string unmet;
            if (!sequence.MatchesContext(_contextLookup, out unmet))
            {
                TaskLog.Warn(sequence.SequenceId, null,
                    "Context requirement not met (" + unmet + "). Sequence not started.");
                return false;
            }

            _sequence = sequence;
            _stepIndex = -1;
            _stepFailed = false;
            Status = TaskSequenceStatus.Running;

            TaskLog.Info(SequenceId, null, "START (" + sequence.StepCount + " steps)");
            if (SequenceStarted != null)
                SequenceStarted.Invoke(this);

            AdvanceToNextStep();
            return true;
        }

        /// <summary>Drive the machine. Call once per frame from a host component.</summary>
        public void Tick(float deltaTime)
        {
            if (!IsRunning || _step == null)
                return;

            _phaseElapsed += deltaTime;
            if (_context != null)
                _context.ElapsedInStep += deltaTime;

            switch (Phase)
            {
                case TaskStepPhase.Present:
                    // Presentation is fire-and-forget in the core; Phase 3 gates this.
                    SetPhase(TaskStepPhase.WaitForInteraction);
                    break;

                case TaskStepPhase.WaitForInteraction:
                    TickWait(deltaTime);
                    break;

                case TaskStepPhase.Feedback:
                    if (_phaseElapsed >= _step.FeedbackDuration)
                        CompleteStep(!_stepFailed);
                    break;
            }
        }

        private void TickWait(float deltaTime)
        {
            if (_validator == null)
            {
                // A step with no validator is a presentation-only beat: it passes at once.
                SetPhase(TaskStepPhase.Validate);
                OnValidated(true);
                return;
            }

            bool satisfied;
            try
            {
                satisfied = _validator.Evaluate(deltaTime);
            }
            catch (Exception exception)
            {
                TaskLog.Error(SequenceId, StepId,
                    "Validator " + _validator.GetType().Name + " threw and the step was failed: " + exception);
                OnValidated(false);
                return;
            }

            if (satisfied)
            {
                SetPhase(TaskStepPhase.Validate);
                OnValidated(true);
                return;
            }

            if (_step.Timeout > 0f && _phaseElapsed >= _step.Timeout)
            {
                TaskLog.Warn(SequenceId, StepId,
                    "Timed out after " + _step.Timeout.ToString("0.##") + "s waiting for " + _validator.Describe() + ".");
                SetPhase(TaskStepPhase.Validate);
                OnValidated(false);
            }
        }

        /// <summary>Completes the current step externally. Required by StepCompletionMode.Manual.</summary>
        public void CompleteCurrentStep(bool success = true)
        {
            if (!IsRunning || _step == null)
                return;
            OnValidated(success);
        }

        /// <summary>Skips the current step if its policy allows it.</summary>
        public bool SkipCurrentStep()
        {
            if (!IsRunning || _step == null)
                return false;

            if (_step.SkipPolicy == SkipPolicy.NotSkippable)
            {
                TaskLog.Warn(SequenceId, StepId, "Skip refused: step is marked NotSkippable.");
                return false;
            }

            if (_step.SkipPolicy == SkipPolicy.SkipWholeSequence)
            {
                TaskLog.Info(SequenceId, StepId, "SKIP -> whole sequence");
                Finish(TaskSequenceStatus.Skipped);
                return true;
            }

            TaskLog.Info(SequenceId, StepId, "SKIP step");
            CleanupStep();
            AdvanceToNextStep();
            return true;
        }

        /// <summary>
        /// Stops immediately and cleans up. Safe to call when idle; always leaves the
        /// runner with no validator hooked and no step context held.
        /// </summary>
        public void Abort(string reason = null)
        {
            if (Status != TaskSequenceStatus.Running)
            {
                CleanupStep();
                return;
            }
            TaskLog.Info(SequenceId, StepId,
                "ABORT" + (string.IsNullOrEmpty(reason) ? string.Empty : " (" + reason + ")"));
            Finish(TaskSequenceStatus.Aborted);
        }

        #endregion

        #region Step machine

        private void AdvanceToNextStep()
        {
            _stepIndex++;
            if (_sequence == null || _stepIndex >= _sequence.StepCount)
            {
                Finish(_stepFailed ? TaskSequenceStatus.Failed : TaskSequenceStatus.Completed);
                return;
            }

            _step = _sequence.Steps[_stepIndex];
            if (_step == null)
            {
                TaskLog.Warn(SequenceId, null, "Step " + _stepIndex + " is null; skipping.");
                AdvanceToNextStep();
                return;
            }

            PrepareStep();
        }

        private void PrepareStep()
        {
            SetPhase(TaskStepPhase.Prepare);
            TaskLog.Info(SequenceId, StepId, "PREPARE (" + (_stepIndex + 1) + "/" + StepCount + ")");

            ITaskEntity entity = null;
            if (_step.EntityId.IsValid && !_entities.TryGetEntity(_step.EntityId, out entity))
            {
                // CASE 02: never throws, always names the id and dumps what IS registered.
                TaskLog.Error(SequenceId, StepId,
                    "EntityId '" + _step.EntityId + "' is not registered. Policy = " + _step.MissingEntityPolicy +
                    ".\nCurrently registered:\n" + _entities.DumpIds());

                if (_step.MissingEntityPolicy == MissingEntityPolicy.SkipStep)
                {
                    _stepFailed = true;
                    CleanupStep();
                    AdvanceToNextStep();
                    return;
                }
                if (_step.MissingEntityPolicy == MissingEntityPolicy.FailSequence)
                {
                    _stepFailed = true;
                    CleanupStep();
                    Finish(TaskSequenceStatus.Failed);
                    return;
                }
            }

            _context = new TaskStepContext(SequenceId, StepId, entity, _entities);
            _validator = _step.Validator;

            if (_validator != null)
            {
                try
                {
                    _validator.Prepare(_context);
                }
                catch (Exception exception)
                {
                    TaskLog.Error(SequenceId, StepId, "Validator Prepare threw: " + exception);
                    _validator = null;
                }
            }
            else
            {
                TaskLog.Info(SequenceId, StepId, "No validator: presentation-only step.");
            }

            if (StepStarted != null)
                StepStarted.Invoke(this, _step);
            SetPhase(TaskStepPhase.Present);
        }

        private void OnValidated(bool success)
        {
            if (!success)
                _stepFailed = true;

            TaskLog.Info(SequenceId, StepId, success ? "VALIDATED" : "VALIDATION FAILED");
            SetPhase(TaskStepPhase.Feedback);

            if (_step.CompletionMode == StepCompletionMode.Immediate || _step.FeedbackDuration <= 0f)
                CompleteStep(success);
            // AfterFeedback dwells in Tick; Manual waits for CompleteCurrentStep.
        }

        private void CompleteStep(bool success)
        {
            SetPhase(TaskStepPhase.Complete);
            TaskLog.Info(SequenceId, StepId, success ? "COMPLETE" : "COMPLETE (failed)");

            TaskStepData completed = _step;
            if (StepCompleted != null)
                StepCompleted.Invoke(this, completed, success);

            CleanupStep();
            AdvanceToNextStep();
        }

        /// <summary>
        /// Unhooks the validator and drops the context. Called on every exit path
        /// (success, failure, skip, abort) so no validator is ever left subscribed.
        /// </summary>
        private void CleanupStep()
        {
            if (_step == null && _validator == null)
                return;

            SetPhase(TaskStepPhase.Cleanup);

            if (_validator != null)
            {
                try
                {
                    _validator.Cleanup();
                }
                catch (Exception exception)
                {
                    TaskLog.Error(SequenceId, StepId, "Validator Cleanup threw: " + exception);
                }
                _validator = null;
            }

            _context = null;
            _step = null;
            Phase = TaskStepPhase.None;
        }

        private void Finish(TaskSequenceStatus status)
        {
            CleanupStep();
            Status = status;
            TaskLog.Info(SequenceId, null, "FINISHED -> " + status);
            if (SequenceFinished != null)
                SequenceFinished.Invoke(this, status);
        }

        private void SetPhase(TaskStepPhase phase)
        {
            if (Phase == phase)
                return;
            Phase = phase;
            _phaseElapsed = 0f;
            if (_step != null && StepPhaseChanged != null)
                StepPhaseChanged.Invoke(this, _step, phase);
        }

        #endregion

        /// <summary>Drops every external subscriber. Called by the session scope on teardown.</summary>
        public void ClearSubscribers()
        {
            SequenceStarted = null;
            StepStarted = null;
            StepPhaseChanged = null;
            StepCompleted = null;
            SequenceFinished = null;
        }
    }
}
