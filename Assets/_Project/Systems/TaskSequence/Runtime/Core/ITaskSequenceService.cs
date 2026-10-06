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
    /// Handle to ONE run. Exposes the runner for rich consumers (presentation, hints)
    /// and a flat completion callback for consumers that only need the outcome.
    /// <para>
    /// The runner is reused across runs, so once this run finishes the handle freezes its
    /// final status and ignores <see cref="Abort"/>: a stale handle can never report or
    /// stop somebody else's run.
    /// </para>
    /// </summary>
    public sealed class TaskSequenceHandle
    {
        private readonly TaskSequenceRunner _runner;
        private readonly string _sequenceId;
        private Action<TaskSequenceStatus> _onFinished;
        private bool _finished;
        private TaskSequenceStatus _finalStatus;

        /// <summary>
        /// Subscribes BEFORE the run starts, so a sequence that finishes synchronously
        /// inside Start (every step skipped) still reaches this handle.
        /// </summary>
        internal TaskSequenceHandle(TaskSequenceRunner runner, string sequenceId)
        {
            if (runner == null)
                throw new ArgumentNullException(nameof(runner));
            _runner = runner;
            _sequenceId = sequenceId;
            _runner.SequenceFinished += HandleFinished;
        }

        /// <summary>Starts <paramref name="sequence"/> on the runner. Returns null when it did not start.</summary>
        internal static TaskSequenceHandle Start(TaskSequenceRunner runner, TaskSequenceSO sequence)
        {
            if (runner == null || sequence == null)
                return null;

            TaskSequenceHandle handle = new TaskSequenceHandle(runner, sequence.SequenceId);
            if (runner.Start(sequence))
                return handle;

            handle.Release();
            return null;
        }

        /// <summary>Full lifecycle access for presentation.</summary>
        public TaskSequenceRunner Runner => _runner;

        public string SequenceId => _sequenceId;
        public TaskSequenceStatus Status => _finished ? _finalStatus : _runner.Status;
        public bool IsRunning => !_finished && _runner.IsRunning;

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
                callback(_finalStatus);
                return this;
            }
            _onFinished += callback;
            return this;
        }

        public void Abort(string reason = null)
        {
            if (!_finished)
                _runner.Abort(reason);
        }

        /// <summary>Drops the runner subscription when the run never started.</summary>
        internal void Release()
        {
            _runner.SequenceFinished -= HandleFinished;
            _onFinished = null;
        }

        private void HandleFinished(TaskSequenceRunner runner, TaskSequenceStatus status)
        {
            if (_finished)
                return;
            _finished = true;
            _finalStatus = status;
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
