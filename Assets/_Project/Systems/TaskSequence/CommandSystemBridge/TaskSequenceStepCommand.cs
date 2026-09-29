using System;
using Cachacos;
using RideSafe.TaskSequence;
using StepCommand;
using UnityEngine;

namespace RideSafe.TaskSequence.Bridge
{
    /// <summary>
    /// Lets an existing CommandSystem flow run a <see cref="TaskSequenceSO"/>.
    /// <para>
    /// This is the ONLY place where the two worlds touch. The runner keeps its full
    /// Prepare -> ... -> Cleanup lifecycle; this adapter just waits for the final status
    /// and flattens it to the bool that <see cref="IStepCommand"/> expects. Nothing about
    /// the sequence is degraded, and the core never references CommandSystem.
    /// </para>
    /// <code>
    /// TutorialDirector  -> TaskSequenceRunner
    /// CommandSystem     -> TaskSequenceStepCommand -> TaskSequenceRunner
    /// </code>
    /// </summary>
    [Serializable]
    public class TaskSequenceStepCommand : StepCommandClass
    {
        [Header("Task Sequence")]
        [Tooltip("Sequence asset to run. Leave empty to resolve _sequenceId through the catalog instead.")]
        [SerializeField] private TaskSequenceSO _sequence;

        [Tooltip("Used when no asset is assigned. Resolved via the registered catalog.")]
        [SerializeField] private string _sequenceId;

        [Tooltip("Statuses other than Completed/Skipped report failure to the CommandSystem.")]
        [SerializeField] private bool _treatSkipAsSuccess = true;

        [Tooltip("Abort the running sequence when this command exits early.")]
        [SerializeField] private bool _abortOnExit = true;

        // The run this command is currently waiting on. Callbacks from any other handle
        // (a previous Execute, or a run aborted by Exit) are ignored, so Complete is raised
        // at most once per Execute and never from inside Exit.
        private TaskSequenceHandle _handle;

        public TaskSequenceStepCommand() { }

        /// <summary>Code construction, used by EditMode tests.</summary>
        internal TaskSequenceStepCommand(TaskSequenceSO sequence, bool abortOnExit = true)
        {
            _sequence = sequence;
            _abortOnExit = abortOnExit;
        }

        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            base.Execute(onComplete);
            AbortPending("CommandSystem step re-executed");

            ITaskSequenceService service = ServiceLocator.Instance.RequestService<ITaskSequenceService>();
            if (service == null)
            {
                TaskLog.Error(DescribeTarget(), null,
                    "No ITaskSequenceService registered. Add a TaskSequenceService to the scene.");
                Complete(false, this);
                return;
            }

            if (service.IsRunning)
            {
                TaskLog.Error(DescribeTarget(), null,
                    "Cannot start: '" + service.CurrentSequenceId + "' is still running. " +
                    "Only one sequence runs at a time; reporting failure.");
                Complete(false, this);
                return;
            }

            TaskSequenceHandle handle = _sequence != null ? service.Run(_sequence) : service.RunById(_sequenceId);
            if (handle == null)
            {
                TaskLog.Error(DescribeTarget(), null, "Sequence could not be started; reporting failure.");
                Complete(false, this);
                return;
            }

            _handle = handle;
            // May fire synchronously when the run already finished inside Run.
            handle.OnFinished(status => HandleFinished(handle, status));
        }

        public override void Exit()
        {
            if (_abortOnExit)
                AbortPending("CommandSystem step exited");
            _handle = null;
            base.Exit();
        }

        /// <summary>Forgets the pending run BEFORE aborting it, so its callback is ignored.</summary>
        private void AbortPending(string reason)
        {
            TaskSequenceHandle pending = _handle;
            _handle = null;
            if (pending != null && pending.IsRunning)
                pending.Abort(reason);
        }

        private void HandleFinished(TaskSequenceHandle handle, TaskSequenceStatus status)
        {
            if (!ReferenceEquals(handle, _handle))
                return;
            _handle = null;

            bool success = status == TaskSequenceStatus.Completed ||
                           (_treatSkipAsSuccess && status == TaskSequenceStatus.Skipped);

            TaskLog.Info(DescribeTarget(), null,
                "Bridge result: " + status + " -> " + (success ? "true" : "false"));

            Complete(success, this);
        }

        private string DescribeTarget() =>
            _sequence != null ? _sequence.SequenceId : (string.IsNullOrEmpty(_sequenceId) ? "<unset>" : _sequenceId);
    }
}
