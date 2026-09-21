using System;

namespace RideSafe.TaskSequence
{
    /// <summary>
    /// The single seam used to run a sequence. Both the tutorial director and the
    /// CommandSystem adapter go through here, which is what keeps them from knowing
    /// about each other.
    /// </summary>
    public interface ITaskSequenceService
    {
        TaskContextService Entities { get; }

        /// <summary>Runs an asset directly. Returns null when it could not start.</summary>
        TaskSequenceHandle Run(TaskSequenceSO sequence);

        /// <summary>
        /// Runs by id, resolved through the registered catalog. This is the entry point
        /// an event-bus message uses, so no caller needs an asset reference.
        /// </summary>
        TaskSequenceHandle RunById(string sequenceId);

        /// <summary>Aborts whatever is running, if anything.</summary>
        void AbortCurrent(string reason = null);

        bool IsRunning { get; }
        string CurrentSequenceId { get; }
    }

    /// <summary>
    /// Handle to one run. Exposes the runner for rich consumers (presentation, hints)
    /// and a flat completion callback for consumers that only need the outcome.
    /// </summary>
    public sealed class TaskSequenceHandle
    {
        private readonly TaskSequenceRunner _runner;
        private Action<TaskSequenceStatus> _onFinished;
        private bool _finished;

        public TaskSequenceHandle(TaskSequenceRunner runner)
        {
            if (runner == null)
                throw new ArgumentNullException(nameof(runner));
            _runner = runner;
            _runner.SequenceFinished += HandleFinished;
        }

        /// <summary>Full lifecycle access. Presentation subscribes here in Phase 3.</summary>
        public TaskSequenceRunner Runner => _runner;

        public string SequenceId => _runner.SequenceId;
        public TaskSequenceStatus Status => _runner.Status;
        public bool IsRunning => _runner.IsRunning;

        /// <summary>
        /// Registers a completion callback. If the run already finished the callback is
        /// invoked immediately, so a late subscriber never hangs.
        /// </summary>
        public TaskSequenceHandle OnFinished(Action<TaskSequenceStatus> callback)
        {
            if (callback == null)
                return this;

            if (_finished)
            {
                callback(_runner.Status);
                return this;
            }
            _onFinished += callback;
            return this;
        }

        public void Abort(string reason = null) => _runner.Abort(reason);

        private void HandleFinished(TaskSequenceRunner runner, TaskSequenceStatus status)
        {
            if (_finished)
                return;
            _finished = true;
            _runner.SequenceFinished -= HandleFinished;

            Action<TaskSequenceStatus> callback = _onFinished;
            _onFinished = null;
            if (callback != null)
                callback.Invoke(status);
        }

        /// <summary>True when the run ended in a state callers should treat as success.</summary>
        public static bool IsSuccess(TaskSequenceStatus status) =>
            status == TaskSequenceStatus.Completed || status == TaskSequenceStatus.Skipped;
    }
}
