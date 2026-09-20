using System;
using UnityEngine;

namespace StepCommand
{
    /// <summary>
    /// Represents an abstract base class for executing parallel action step commands.
    /// This class provides functionality for managing the activation and deactivation of external actions,
    /// handling completion states, and integrating with the parallel step execution process.
    /// </summary>
    /// <typeparam name="T">The type of the external action to be controlled.</typeparam>
    public abstract class ParralleBaseActionStepCommand<T> : StepCommandClass, IParallelStepCommand
    {
        [Header("Parrallel Settings")]
        [Tooltip("Check this as true when working in the parallel step and you want to complete the parallel process without waiting for the other steps.")]
        [SerializeField] private bool _forceQuit;
        [Header("BaseAction Settings")]
        [Tooltip("Enable this to set the external class as inactive at the start.")]
        [SerializeField] private bool _startDeactive;
        [Tooltip("Enable this to set the external class as inactive when the step is completed.")]
        [SerializeField] private bool _endDeactive;
        [Tooltip("The external class to be integrated into the system.")]
        [SerializeField] protected T action;
        private bool did;

        #region ICommand Methods
        public override IStepCommand Initialize()
        {
            if (_startDeactive)
                Set(false);
            return base.Initialize();
        }
        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            did = false;
            base.Execute(onComplete);
            if (_startDeactive)
                Set(true);
            AddDelegates(Did, Stopped);
        }
        public override void Exit()
        {
            RemoveDelegates(Did, Stopped);
            if (_endDeactive)
                Set(false);
        }
        public override void InternalUndo()
        {
            RemoveDelegates(Did, Stopped);
            if (_startDeactive && !_endDeactive)
                Set(true);
        }
        protected override void Complete(bool right, IStepCommand stepCommand)
        {
            if (ActiveParrallel)
            {
                did = right;
                base.Complete(right, stepCommand);
                return;
            }
            base.Complete(right, stepCommand);
            Exit();
        }
        #endregion
        #region IParallelComand Methods
        public bool ActiveParrallel { get; set; }
        public bool ForceQuit() => _forceQuit;
        public bool IsExecuted() => did;
        public void CompleteParallel() => Exit();
        #endregion
        #region BaseAction Methods
        /// <summary>
        /// This method  is used to active and deactive the class <see cref="T"/>.
        /// </summary>
        /// <param name="state"></param>
        protected abstract void Set(bool state);
        /// <summary>
        /// Adds delegates to the external class of type <see cref="T"/> for handling completion and stop events.
        /// </summary>
        /// <param name="right">The delegate to invoke when the action completes successfully.</param>
        /// <param name="wrong">The delegate to invoke when the action stops or fails.</param>
        protected abstract void AddDelegates(Action right, Action wrong);
        /// <summary>
        /// Remove delegates to the external class of type <see cref="T"/> for handling completion and stop events.
        /// </summary>
        /// <param name="right">The delegate to invoke when the action completes successfully.</param>
        /// <param name="wrong">The delegate to invoke when the action stops or fails.</param>
        protected abstract void RemoveDelegates(Action right, Action wrong);
        #endregion
        private void Did() => Complete(true, this);
        private void Stopped() => Complete(false, this);

    }
}