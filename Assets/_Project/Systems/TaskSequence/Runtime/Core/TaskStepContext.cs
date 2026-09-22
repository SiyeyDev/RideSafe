using Cachacos;

namespace RideSafe.TaskSequence
{
    /// <summary>
    /// Everything a validator is allowed to know about the step it is validating.
    /// <para>
    /// Deliberately narrow: it exposes the resolved entity and a service lookup, never
    /// the module, the scene or the sequence asset. A validator that needs something else
    /// resolves it as a service, which keeps the core free of gameplay dependencies.
    /// </para>
    /// </summary>
    public sealed class TaskStepContext
    {
        public string SequenceId { get; }
        public string StepId { get; }

        /// <summary>Resolved entity for this step, or null when the step has no EntityId.</summary>
        public ITaskEntity Entity { get; }

        /// <summary>Registry, for validators that need to look at sibling entities.</summary>
        public TaskContextService Entities { get; }

        /// <summary>Seconds since the step entered WaitForInteraction.</summary>
        public float ElapsedInStep { get; internal set; }

        public TaskStepContext(string sequenceId, string stepId, ITaskEntity entity, TaskContextService entities)
        {
            SequenceId = sequenceId;
            StepId = stepId;
            Entity = entity;
            Entities = entities;
        }

        /// <summary>
        /// Service lookup seam. Validators in the tutorial layer use this to reach the
        /// input service without the core ever referencing it.
        /// </summary>
        public T GetService<T>() where T : class => ServiceLocator.Instance.RequestService<T>();

        public void LogError(string message) => TaskLog.Error(SequenceId, StepId, message);
        public void LogWarn(string message) => TaskLog.Warn(SequenceId, StepId, message);
    }
}
