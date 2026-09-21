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

        private TaskSequenceHandle _handle;
        private bool _completed;

        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            base.Execute(onComplete);
            _completed = false;

            ITaskSequenceService service = ServiceLocator.Instance.RequestService<ITaskSequenceService>();
            if (service == null)
            {
                TaskLog.Error(DescribeTarget(), null,
                    "No ITaskSequenceService registered. Add a TaskSequenceService to the scene.");
                Complete(false, this);
                return;
            }

            _handle = _sequence != null ? service.Run(_sequence) : service.RunById(_sequenceId);

            if (_handle == null)
            {
                TaskLog.Error(DescribeTarget(), null, "Sequence could not be started; reporting failure.");
                Complete(false, this);
                return;
            }

            _handle.OnFinished(HandleFinished);
        }

        public override void Exit()
        {
            if (_abortOnExit && _handle != null && _handle.IsRunning)
                _handle.Abort("CommandSystem step exited");

            _handle = null;
            base.Exit();
        }

        private void HandleFinished(TaskSequenceStatus status)
        {
            if (_completed)
                return;
            _completed = true;
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
