namespace StepCommand
{
    /// <summary>
    /// Represents the interface for step commands that support parallel execution.
    /// </summary>
    public interface IParallelStepCommand
    {
        /// <summary>
        /// Gets or sets a value indicating whether the step command is part of a parallel process.
        /// </summary>
        public bool ActiveParrallel { get; set; }
        /// <summary>
        /// Determines whether the parallel process should forcefully terminate without waiting for other steps to complete.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the parallel process should terminate forcefully; otherwise, <c>false</c>.
        /// </returns>
        public bool ForceQuit();
        /// <summary>
        /// Marks the parallel step as completed and performs any necessary cleanup operations.
        /// </summary>
        public void CompleteParallel();
        /// <summary>
        /// Checks whether the step command has been executed.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the step command has been executed; otherwise, <c>false</c>.
        /// </returns>
        public bool IsExecuted();
    }
}