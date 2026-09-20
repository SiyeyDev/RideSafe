using Sirenix.OdinInspector;
using Sirenix.Serialization;
using StepCommand.Debugging;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StepCommand
{
    [AddComponentMenu("StepCommand/ParallelStepCommand")]
    public class ParallelStepCommand : StepCommandMonoBehaviour, IParallelStepCommand
    {
        [Header("Parallel Settings")]
        [HideReferenceObjectPicker, OdinSerialize] protected StepCommandWrapper[] _stepCommands;
        [SerializeField] private bool _forceQuit;
        private HashSet<IParallelStepCommand> _parralleStepCommand;
        private Action<bool, IStepCommand> _completeParallel;
        protected Action<bool, IStepCommand> _onCompletedInternalStep;
        private bool _parralledCompleted;
        #region ICommand Methods
        public override IStepCommand Initialize()
        {
            _parralleStepCommand = new HashSet<IParallelStepCommand>();
            InitializeCommands();
            return base.Initialize();
        }
        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            _completeParallel = onComplete;
            _onCompletedInternalStep = (right, stepCommand) => Complete(right, stepCommand);
            base.Execute(!ActiveParrallel ? _completeParallel : _onCompletedInternalStep);
            InternalExecute();
            _parralledCompleted = false;
        }
        public override void Exit()
        {
            foreach (StepCommandWrapper stepCommandWrapper in _stepCommands)
                stepCommandWrapper.StepCommand.Exit();
            base.Exit();
        }
        public override void InternalUndo()
        {
            foreach (StepCommandWrapper stepCommandWrapper in _stepCommands)
                stepCommandWrapper.StepCommand.Undo();
            base.InternalUndo();
        }
        protected override void Complete(bool right, IStepCommand stepCommand, bool callDelegate = true)
        {
            StepCommandDebugUtilities.DebugLog($"Complete in {gameObject.name} parallalel with {right} command {StepCommandDebugUtilities.GetBoldColorMessage(stepCommand.ToString(), StepCommandDebugData.commandLableMainColor)}");
            if (stepCommand is IParallelStepCommand curretnParrallelStepCommand && curretnParrallelStepCommand.ForceQuit())
            {
                Complete(right);
                return;
            }
            if (right)
            {
                if (!AllStepCommandsCompleted())
                    return;
                Complete(true);
                return;
            }
            if (!ActiveParrallel)
                return;
            if (!_parralledCompleted)
                return;
            _completeParallel?.Invoke(false, this);
        }
        #endregion
        #region IParallelComand Methods
        [field: ReadOnly][field: SerializeField] public bool ActiveParrallel { get; set; }
        public bool ForceQuit() => _forceQuit;
        public bool IsExecuted() => ActiveParrallel ? AllStepCommandsCompleted() : enabled;
        public void CompleteParallel()
        {
            foreach (IParallelStepCommand parrallelStepCommand in _parralleStepCommand)
                parrallelStepCommand.CompleteParallel();
            base.Complete(true, this, !ActiveParrallel);
        }
        #endregion
        #region Main Methods
        [Button("Complete Task")]
        public void ButtonComplete()
        {
            base.Complete(true, this);
        }

        protected void Complete(bool right)
        {
            _parralledCompleted = true;
            if (ActiveParrallel)
            {
                _completeParallel?.Invoke(right, this);
                return;
            }
            foreach (IParallelStepCommand parrallelStepCommand in _parralleStepCommand)
                parrallelStepCommand.CompleteParallel();
            base.Complete(true, this);
        }
        private void InitializeCommands()
        {
            foreach (StepCommandWrapper stepCommandWrapper in _stepCommands)
            {
                if (stepCommandWrapper.StepCommand is IParallelStepCommand parrallelStepCommand)
                {
                    _parralleStepCommand.Add(parrallelStepCommand);
                    parrallelStepCommand.ActiveParrallel = true;
                }
                StepCommandDebugUtilities.DebugLog($"Intiailize {StepCommandDebugUtilities.GetBoldColorMessage(stepCommandWrapper.StepCommand.ToString(), StepCommandDebugData.commandLableMainColor)} in {gameObject.name}");
                stepCommandWrapper.StepCommand.Initialize();
            }
        }
        protected virtual void InternalExecute()
        {
            foreach (StepCommandWrapper stepCommandWrapper in _stepCommands)
                stepCommandWrapper.StepCommand.Execute(_onCompletedInternalStep);
        }
        protected virtual bool AllStepCommandsCompleted()
        {
            foreach (IParallelStepCommand parrallelStepCommand in _parralleStepCommand)
            {
                if (!parrallelStepCommand.IsExecuted())
                    return false;
            }
            return true;
        }
        #endregion
    }
}