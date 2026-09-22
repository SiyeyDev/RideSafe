namespace RideSafe.TaskSequence
{
    /// <summary>
    /// Decides when a step's required interaction has happened.
    /// <para>
    /// Validators are polled, not event-driven, on purpose: an event-driven validator
    /// that forgets to unsubscribe leaks across scenes. Validators that wrap an event
    /// source subscribe in <see cref="Prepare"/>, latch a flag in the callback, and
    /// unsubscribe in <see cref="Cleanup"/>.
    /// </para>
    /// <para>
    /// CONTRACT: <see cref="Prepare"/> must reset all runtime state. The same instance is
    /// reused every time its sequence runs, so leftover state from a previous run would
    /// satisfy the step instantly. It also means input that happened BEFORE Prepare is
    /// never observed, which is what keeps early presses from skipping a step (CASE 05).
    /// </para>
    /// </summary>
    public interface ITaskValidator
    {
        /// <summary>Reset state and hook any sources. Called once, at step Prepare.</summary>
        void Prepare(TaskStepContext context);

        /// <summary>Polled once per frame while the step waits. True means satisfied.</summary>
        bool Evaluate(float deltaTime);

        /// <summary>Unhook sources and drop references. Always called, including on abort.</summary>
        void Cleanup();

        /// <summary>Human-readable summary used in logs and the inspector foldout.</summary>
        string Describe();
    }
}
