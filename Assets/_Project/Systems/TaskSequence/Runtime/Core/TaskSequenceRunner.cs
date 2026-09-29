using System;
using System.Collections.Generic;

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
    /// <para>
    /// Events may be raised re-entrantly: a listener is allowed to Abort, Skip or even Start
    /// a new sequence from inside a callback, and the runner stops touching the old step.
    /// </para>
    /// </summary>
    public sealed class TaskSequenceRunner
    {
        private readonly TaskContextService _entities;

        // Per-runner session memory for RestartPolicy. Not persisted.
        private readonly HashSet<string> _completedSequences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _resumeIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private TaskSequenceSO _sequence;
        private int _stepIndex = -1;
        private TaskStepData _step;
        private TaskStepContext _context;
        private ITaskValidator _validator;
        private float _phaseElapsed;
        private bool _anyStepFailed;
        private bool _pendingSuccess;

        public TaskSequenceStatus Status { get; private set; } = TaskSequenceStatus.Idle;
        public TaskStepPhase Phase { get; private set; } = TaskStepPhase.None;
        public string SequenceId => _sequence != null ? _sequence.SequenceId : null;
        public string StepId => _step != null ? _step.StepId : null;
        public int StepIndex => _stepIndex;
        public int StepCount => _sequence != null ? _sequence.StepCount : 0;
        public bool IsRunning => Status == TaskSequenceStatus.Running;

        /// <summary>1 on the first try of the current step, +1 on every Retry. Presentation can escalate help with it.</summary>
        public int StepAttempt { get; private set; }

        /// <summary>
        /// Supplies context values to sequence requirements. Null means no context is wired:
        /// requirements are then ignored with a warning instead of blocking every sequence.
        /// </summary>
        public Func<string, string> ContextLookup { get; set; }

        public event Action<TaskSequenceRunner> SequenceStarted;
        public event Action<TaskSequenceRunner, TaskStepData> StepStarted;
        public event Action<TaskSequenceRunner, TaskStepData, TaskStepPhase> StepPhaseChanged;
        public event Action<TaskSequenceRunner, TaskStepData, bool> StepCompleted;
        public event Action<TaskSequenceRunner, TaskSequenceStatus> SequenceFinished;

        public TaskSequenceRunner(TaskContextService entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));
            _entities = entities;
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
            if (sequence.RestartPolicy == RestartPolicy.RunOnce && _completedSequences.Contains(sequence.SequenceId))
            {
                TaskLog.Warn(sequence.SequenceId, null, "RestartPolicy.RunOnce: already completed this session. Not started.");
                return false;
            }
            if (!PassesContext(sequence))
                return false;

            int startIndex = 0;
            int resumeAt;
            if (sequence.RestartPolicy == RestartPolicy.Resume &&
                _resumeIndex.TryGetValue(sequence.SequenceId, out resumeAt) &&
                resumeAt > 0 && resumeAt < sequence.StepCount)
            {
                startIndex = resumeAt;
            }
            _resumeIndex.Remove(sequence.SequenceId);

            _sequence = sequence;
            _stepIndex = startIndex - 1;
            _anyStepFailed = false;
            Status = TaskSequenceStatus.Running;

            TaskLog.Info(SequenceId, null, "START (" + sequence.StepCount + " steps" +
                                           (startIndex > 0 ? ", resuming at " + (startIndex + 1) : string.Empty) + ")");
            if (SequenceStarted != null)
                SequenceStarted.Invoke(this);
            if (!IsRunning || _sequence != sequence)
                return true;

            AdvanceToNextStep();
            return true;
        }

        /// <summary>Drive the machine. Call once per frame from a host component.</summary>
        public void Tick(float deltaTime)
        {
            if (!IsRunning || _step == null)
                return;

            _phaseElapsed += deltaTime;

            switch (Phase)
            {
                case TaskStepPhase.Present:
                    // Presentation is fire-and-forget in the core.
                    SetPhase(TaskStepPhase.WaitForInteraction);
                    break;

                case TaskStepPhase.WaitForInteraction:
                    if (_context != null)
                        _context.ElapsedInStep += deltaTime;
                    TickWait(deltaTime);
                    break;

                case TaskStepPhase.Feedback:
                    // Only AfterFeedback dwells here on a timer; Manual waits for CompleteCurrentStep.
                    if (_step.CompletionMode == StepCompletionMode.AfterFeedback &&
                        _phaseElapsed >= _step.FeedbackDuration)
                        ResolveFeedback();
                    break;
            }
        }

        private void TickWait(float deltaTime)
        {
            if (_validator == null)
            {
                // A step with no validator is a presentation-only beat: it passes at once.
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
                FailStep("validator " + _validator.GetType().Name + " threw: " + exception, recoverable: false);
                return;
            }

            if (_validator.IsBroken)
            {
                FailStep("validator " + _validator.GetType().Name + " is broken (" + _validator.Describe() + ").",
                    recoverable: false);
                return;
            }

            if (satisfied)
            {
                OnValidated(true);
                return;
            }

            if (_step.Timeout > 0f && _phaseElapsed >= _step.Timeout)
            {
                TaskLog.Warn(SequenceId, StepId,
                    "Timed out after " + _step.Timeout.ToString("0.##") + "s waiting for " + _validator.Describe() + ".");
                OnValidated(false);
            }
        }

        /// <summary>
        /// Completes the current step from outside, in any phase. This is how a Manual step
        /// advances; passing false routes through the step's failure policy.
        /// </summary>
        public void CompleteCurrentStep(bool success = true)
        {
            if (!IsRunning || _step == null)
                return;

            if (success)
                CompleteStep();
            else
                FailStep("completed externally as failed.", recoverable: true);
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

        /// <summary>Skips the whole sequence unless the sequence is NotSkippable.</summary>
        public bool SkipSequence()
        {
            if (!IsRunning)
                return false;

            if (_sequence.SkipPolicy == SkipPolicy.NotSkippable)
            {
                TaskLog.Warn(SequenceId, StepId, "Skip refused: sequence is marked NotSkippable.");
                return false;
            }

            TaskLog.Info(SequenceId, StepId, "SKIP whole sequence");
            Finish(TaskSequenceStatus.Skipped);
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

            if (_sequence.RestartPolicy == RestartPolicy.Resume && _stepIndex > 0)
                _resumeIndex[_sequence.SequenceId] = _stepIndex;

            Finish(TaskSequenceStatus.Aborted);
        }

        #endregion

        #region Step machine

        private void AdvanceToNextStep()
        {
            _stepIndex++;
            if (_sequence == null || _stepIndex >= _sequence.StepCount)
            {
                Finish(_anyStepFailed ? TaskSequenceStatus.Failed : TaskSequenceStatus.Completed);
                return;
            }

            _step = _sequence.Steps[_stepIndex];
            if (_step == null)
            {
                TaskLog.Warn(SequenceId, null, "Step " + _stepIndex + " is null; skipping.");
                AdvanceToNextStep();
                return;
            }

            StepAttempt = 1;
            PrepareStep();
        }

        private void PrepareStep()
        {
            TaskStepData step = _step;
            SetPhase(TaskStepPhase.Prepare);
            TaskLog.Info(SequenceId, StepId, "PREPARE (" + (_stepIndex + 1) + "/" + StepCount + ")" +
                                             (StepAttempt > 1 ? " attempt " + StepAttempt : string.Empty));

            ITaskEntity entity = null;
            if (step.EntityId.IsValid && !_entities.TryGetEntity(step.EntityId, out entity))
            {
                // CASE 02: never throws, always names the id and dumps what IS registered.
                TaskLog.Error(SequenceId, StepId,
                    "EntityId '" + step.EntityId + "' is not registered. Policy = " + step.MissingEntityPolicy +
                    ".\nCurrently registered:\n" + _entities.DumpIds());

                if (step.MissingEntityPolicy == MissingEntityPolicy.SkipStep)
                {
                    _anyStepFailed = true;
                    CleanupStep();
                    AdvanceToNextStep();
                    return;
                }
                if (step.MissingEntityPolicy == MissingEntityPolicy.FailSequence)
                {
                    _anyStepFailed = true;
                    Finish(TaskSequenceStatus.Failed);
                    return;
                }
            }

            _context = new TaskStepContext(SequenceId, StepId, entity, _entities);
            _validator = step.Validator;

            string brokenReason = null;
            if (_validator != null)
            {
                try
                {
                    _validator.Prepare(_context);
                    if (_validator.IsBroken)
                        brokenReason = "validator " + _validator.GetType().Name + " is broken after Prepare.";
                }
                catch (Exception exception)
                {
                    brokenReason = "validator Prepare threw: " + exception;
                }
            }
            else
            {
                TaskLog.Info(SequenceId, StepId, "No validator: presentation-only step.");
            }

            if (StepStarted != null)
                StepStarted.Invoke(this, step);
            if (!IsCurrent(step))
                return;

            SetPhase(TaskStepPhase.Present);

            // Fail fast: a validator that cannot succeed must never leave the learner waiting.
            if (brokenReason != null)
                FailStep(brokenReason, recoverable: false);
        }

        private void OnValidated(bool success)
        {
            TaskLog.Info(SequenceId, StepId, success ? "VALIDATED" : "VALIDATION FAILED");
            SetPhase(TaskStepPhase.Validate);
            _pendingSuccess = success;
            SetPhase(TaskStepPhase.Feedback);

            if (_step.CompletionMode == StepCompletionMode.AfterFeedback && _step.FeedbackDuration > 0f)
                return; // Tick resolves once the feedback has been shown.
            if (success && _step.CompletionMode == StepCompletionMode.Manual)
                return; // CompleteCurrentStep resolves.

            ResolveFeedback();
        }

        private void ResolveFeedback()
        {
            if (_pendingSuccess)
                CompleteStep();
            else
                FailStep("validation failed.", recoverable: true);
        }

        private void CompleteStep()
        {
            TaskStepData completed = _step;
            SetPhase(TaskStepPhase.Complete);
            TaskLog.Info(SequenceId, StepId, "COMPLETE");

            if (StepCompleted != null)
                StepCompleted.Invoke(this, completed, true);
            if (!IsCurrent(completed))
                return;

            CleanupStep();
            AdvanceToNextStep();
        }

        /// <summary>
        /// Single failure path. <paramref name="recoverable"/> is false when trying again
        /// cannot help (broken or throwing validator), in which case Retry degrades to Skip.
        /// </summary>
        private void FailStep(string reason, bool recoverable)
        {
            TaskStepData failed = _step;
            StepFailurePolicy policy = failed.FailurePolicy;

            if (policy == StepFailurePolicy.Retry && recoverable)
            {
                TaskLog.Info(SequenceId, StepId, "RETRY (" + reason + ")");
                CleanupStep();
                _step = failed;
                StepAttempt++;
                PrepareStep();
                return;
            }

            if (recoverable)
                TaskLog.Warn(SequenceId, StepId, "STEP FAILED (" + reason + ") Policy = " + policy + ".");
            else
                TaskLog.Error(SequenceId, StepId, "STEP FAILED, not retryable (" + reason + ") Policy = " + policy + ".");

            _anyStepFailed = true;
            SetPhase(TaskStepPhase.Complete);
            if (StepCompleted != null)
                StepCompleted.Invoke(this, failed, false);
            if (!IsCurrent(failed))
                return;

            if (policy == StepFailurePolicy.FailSequence)
            {
                Finish(TaskSequenceStatus.Failed);
                return;
            }

            CleanupStep();
            AdvanceToNextStep();
        }

        /// <summary>
        /// Unhooks the validator and drops the context. Called on every exit path
        /// (success, failure, retry, skip, abort) so no validator is ever left subscribed.
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
            if (status == TaskSequenceStatus.Completed && _sequence != null)
                _completedSequences.Add(_sequence.SequenceId);

            TaskLog.Info(SequenceId, null, "FINISHED -> " + status);
            if (SequenceFinished != null)
                SequenceFinished.Invoke(this, status);
        }

        private bool PassesContext(TaskSequenceSO sequence)
        {
            if (!sequence.HasContextRequirements)
                return true;

            if (ContextLookup == null)
            {
                TaskLog.Warn(sequence.SequenceId, null,
                    "Sequence has context requirements but no ContextLookup is wired; requirements ignored.");
                return true;
            }

            string unmet;
            if (sequence.MatchesContext(ContextLookup, out unmet))
                return true;

            TaskLog.Warn(sequence.SequenceId, null, "Context requirement not met (" + unmet + "). Sequence not started.");
            return false;
        }

        /// <summary>False once a listener aborted, skipped or restarted from inside a callback.</summary>
        private bool IsCurrent(TaskStepData step) => IsRunning && ReferenceEquals(_step, step);

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

        /// <summary>Drops every external subscriber. Called by the host on teardown.</summary>
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
