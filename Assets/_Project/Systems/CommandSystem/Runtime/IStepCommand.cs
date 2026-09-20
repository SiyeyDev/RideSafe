using System;

namespace StepCommand
{
    /// <summary>
    /// Represents an interface for step commands, defining the methods and properties required for command execution and management.
    /// </summary>
    public interface IStepCommand
    {
        /// <summary>
        /// Gets the number of reversals the step command can perform.
        /// </summary>
        /// <remarks>
        /// This value should be 1 if the step can only be reversed once, and greater than 1 if the system needs to reverse multiple steps.
        /// </remarks>
        public int ReverseAmount { get; }
        #region TaskCommandMethods
        /// <summary>
        /// Initializes the step command with a completion callback.
        /// </summary>
        /// <returns>The initialized step command instance.</returns>
        public IStepCommand Initialize();
        /// <summary>
        /// Executes the step command.
        /// </summary>
        /// <param name="onComplete">The action to invoke when the step command is completed. 
        /// The callback receives a boolean indicating success and the instance of the completed step command.</param>
        public void Execute(Action<bool, IStepCommand> onComplete);
        /// <summary>
        /// Reverses the effects of the step command.
        /// </summary>
        public void Undo();
        /// <summary>
        /// Exits and cleans up resources or states associated with the step command.
        /// </summary>
        public void Exit();
        #endregion
    }
}
