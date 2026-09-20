using System;
using UnityEngine;

namespace StepCommand
{
    /// <summary>
    /// Represents an abstract base class for executing MonoBehaviour-based step commands 
    /// in a parallel workflow.
    /// </summary>
    public abstract class ParralleBaseMonoBehaviourStepCommand : StepCommandMonoBehaviour, IParallelStepCommand
    {
        [Header("Parrallel Settings")]
        [SerializeField] private bool _forceQuit;
        private Action<bool, IStepCommand> _completed;
        protected bool executed;
        #region ICommand Methods
        /// <summary>
        /// Execute the step command with a completion callback regarding if the parallel is active.
        /// </summary>
        /// <param name="onComplete">The action to invoke when the step is completed.</param>
        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            if (!ActiveParrallel)
            {
                base.Execute(onComplete);
                return;
            }
            _completed = onComplete;
            onComplete = null;
            base.Execute(onComplete);
        }
        /// <summary>
        /// Completes the step command regarding if the parallel is active.
        /// </summary>
        /// <param name="right">Indicates whether the step completed successfully.</param>
        /// <param name="stepCommand">The step command being completed.</param>
        /// <param name="callDelegate">If <c>true</c>, the delegate will be invoked. Defaults to <c>true</c>.</param>
        protected override void Complete(bool right, IStepCommand stepCommand, bool callDelegate = true)
        {
            if (ActiveParrallel)
            {
                executed = right;
                _completed?.Invoke(right, stepCommand);
                return;
            }
            base.Complete(right, stepCommand);
        }
        #endregion
        #region IParallelComand Methods
        public bool ActiveParrallel { get; set; }
        public bool ForceQuit() => _forceQuit;
        public abstract bool IsExecuted();
        public virtual void CompleteParallel()
        {
            Exit();
            base.Complete(true, this);
        }
        #endregion
    }
}