using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace StepCommand
{
    [AddComponentMenu("StepCommand/TreeStepCommand")]
    public class TreeStepCommand : StepCommandMonoBehaviour
    {
        [Header("Wrong Settings")]
        [HideReferenceObjectPicker, OdinSerialize] private StepCommandWrapper _stepCommandValidation;
        [Space(1)]
        [HideReferenceObjectPicker, OdinSerialize] private StepCommandWrapper _commandRight;
        [HideReferenceObjectPicker, OdinSerialize] private StepCommandWrapper _commandWrong;
        private IStepCommand _stepCommand;
        private bool _madeItRight;

        #region ICommand Methods
        public override IStepCommand Initialize()
        {
            _stepCommandValidation.StepCommand.Initialize();
            _commandRight.StepCommand.Initialize();
            _commandWrong.StepCommand.Initialize();
            return base.Initialize();
        }
        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            _madeItRight = false;
            base.Execute(onComplete);
            _stepCommand = _stepCommandValidation.StepCommand;
            _stepCommand.Execute(CompleteCommandValidation);
        }
        public override void InternalUndo()
        {
            if (_stepCommand != _stepCommandValidation.StepCommand)
            {
                if (_madeItRight)
                    _commandRight.StepCommand.Undo();
                else
                    _commandWrong.StepCommand.Undo();
            }
            _stepCommandValidation.StepCommand.Undo();
            base.InternalUndo();
        }
        public override void Exit()
        {
            _stepCommand?.Exit();
            base.Exit();
        }
        #endregion
        #region Main Methods
        private void CompleteCommandValidation(bool right, IStepCommand stepCommand)
        {
            _madeItRight = true;
            _stepCommand = right ? _commandRight.StepCommand : _commandWrong.StepCommand;
            _stepCommand.Execute(CompleteToDoStep);
        }
        protected virtual void CompleteToDoStep(bool right, IStepCommand stepCommand) => Complete(right, this);
        #endregion
    }
}