using Sirenix.OdinInspector;
using Sirenix.Serialization;
using StepCommand.Debugging;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace StepCommand
{
    public class WaitForMinAmmountCompletedStepCommand : StepCommandMonoBehaviour
    {
        [Header("CompleteMultiple Settings")]
        [HideReferenceObjectPicker, OdinSerialize] private StepCommandWrapper[] _stepCommands;
        [SerializeField] private int _minAmount;

        private int _amountCompleted;
        private HashSet<IStepCommand> _defaultsteps;
        private HashSet<IStepCommand> _remainingSteps;
        private void Awake()
        {
            if (_minAmount > _stepCommands.Length)
                _minAmount = _stepCommands.Length;
        }

        #region ICommand Methods
        public override IStepCommand Initialize()
        {
            InitializeCommands();
            _defaultsteps = new HashSet<IStepCommand>();
            foreach (StepCommandWrapper stepCommand in _stepCommands)
                _defaultsteps.Add(stepCommand.StepCommand);
            return base.Initialize();
        }
        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            _amountCompleted = 0;
            _remainingSteps = new HashSet<IStepCommand>(_defaultsteps);
            InternalExecute();
            base.Execute(onComplete);
        }
        public override void InternalUndo()
        {
            foreach (IStepCommand stepCommand in _defaultsteps)
            {
                Debug.Log($"Undo In Multiple {stepCommand}");
                stepCommand.Undo();
            }
            base.InternalUndo();
        }
        public override void Exit()
        {
            ExitRemainingSteps();
            base.Exit();
        }

        private void ExitRemainingSteps()
        {
            foreach (IStepCommand stepCommand in _remainingSteps)
                stepCommand.Exit();
        }
        #endregion
        private void InitializeCommands()
        {
            foreach (StepCommandWrapper stepCommandWrapper in _stepCommands)
            {
                StepCommandDebugUtilities.DebugLog($"Intiailize {StepCommandDebugUtilities.GetBoldColorMessage(stepCommandWrapper.StepCommand.ToString(), StepCommandDebugData.commandLableMainColor)} in {gameObject.name}");
                stepCommandWrapper.StepCommand.Initialize();
            }
        }
        private void InternalExecute()
        {
            foreach (StepCommandWrapper stepCommandWrapper in _stepCommands)
                stepCommandWrapper.StepCommand.Execute(CompletStepCommand);
        }

        private void CompletStepCommand(bool right, IStepCommand command)
        {
            _remainingSteps.Remove(command);
            if (command is IParallelStepCommand parrallelStepCommand && parrallelStepCommand.ForceQuit())
            {
                ExitRemainingSteps();
                _amountCompleted++;
                Complete(right, this);
                return;
            }
            _amountCompleted++;
            if (_amountCompleted < _minAmount)
                return;
            ExitRemainingSteps();
            Complete(true, this);
            Exit();
        }

    }
}