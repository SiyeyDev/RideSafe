using Sirenix.OdinInspector;
using Sirenix.Serialization;
using StepCommand.Debugging;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StepCommand
{
    [AddComponentMenu("StepCommand/BlockStepCommand")]
    public class BlockStepCommand : StepCommandMonoBehaviour
    {
        [Header("Block Settings")]
        [HideReferenceObjectPicker, OdinSerialize] private StepCommandWrapper[] _steps;
        private Stack<StepCommandWrapper> _stepsToDo;
        private Stack<StepCommandWrapper> _stepsCompleted;
        private StepCommandWrapper _currentStep;

        #region ICommand Methods
        public override IStepCommand Initialize()
        {
            SetSteps();
            return base.Initialize();
        }
        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            SetSteps(false);
            base.Execute(onComplete);
            ProcessNextStep();
        }
        public override void Exit()
        {
            _currentStep?.StepCommand.Exit();
            base.Exit();
        }
        public override void InternalUndo()
        {
            _currentStep?.StepCommand.Undo();
            foreach (StepCommandWrapper stepToBack in _stepsCompleted)
                stepToBack.StepCommand.Undo();
            base.InternalUndo();
        }
        #endregion
        #region Main Methods
        protected void SetSteps(bool intializeCommand = true)
        {
            _stepsToDo = new Stack<StepCommandWrapper>(_steps.Length);
            _stepsCompleted = new Stack<StepCommandWrapper>(_steps.Length);
            for (int i = _steps.Length - 1; i >= 0; i--)
            {
                _stepsToDo.Push(_steps[i]);
                if (intializeCommand)
                {
                    StepCommandDebugUtilities.DebugLog($"Intiailize {StepCommandDebugUtilities.GetBoldColorMessage(_steps[i].StepCommand.ToString(), StepCommandDebugData.commandLableMainColor)} in {gameObject.name}");
                    _steps[i].StepCommand.Initialize();
                }
            }
        }

        protected void ProcessNextStep(bool right = true, IStepCommand currenStepCommand = null)
        {
            if (currenStepCommand != null && GetStepCommandWrapper(currenStepCommand) != null)
                StepCommandDebugUtilities.DebugLog($"Complete  in {gameObject.name} block with {right} command {StepCommandDebugUtilities.GetBoldColorMessage(GetStepCommandWrapper(currenStepCommand).GetGuideName(), StepCommandDebugData.commandLableMainColor)}");
            if (_currentStep != null)
                _stepsCompleted.Push(_currentStep);
            if (!right && _currentStep.StepCommand.ReverseAmount > 0)
            {
                this.HandleReverse(ref _currentStep, ref _stepsCompleted, ref _stepsToDo);
                _currentStep.StepCommand.Execute(ProcessNextStep);
                return;
            }
            if (_stepsToDo.Count == 0)
            {
                _currentStep = null;
                StepCommandDebugUtilities.DebugLog($"Complete BLOCK {StepCommandDebugUtilities.GetBoldColorMessage(gameObject.name, StepCommandDebugData.commandLableMainColor)}");
                Complete(right, this);
                return;
            }
            DoNextStep();
        }
        private void DoNextStep()
        {
            _currentStep = _stepsToDo.Pop();
            StepCommandDebugUtilities.DebugLog($"EXECUTE {StepCommandDebugUtilities.GetBoldColorMessage(_currentStep.GetGuideName(), StepCommandDebugData.commandLableMainColor)} in {gameObject.name}");
            _currentStep.StepCommand.Execute(ProcessNextStep);
        }
        #endregion

        private StepCommandWrapper GetStepCommandWrapper(IStepCommand stepCommand)
        {
            for (int i = 0; i < _steps.Length; i++)
            {
                if (_steps[i].StepCommand == stepCommand)
                    return _steps[i];
            }
            return null;
        }
    }
}