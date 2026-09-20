using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace StepCommand
{
    [AddComponentMenu("StepCommand/ValidateStepCommand")]
    public class ValidateStepCommand : StepCommandMonoBehaviour
    {
        [Header("Wrong Settings")]
        [SerializeField] private bool _shouldBeRight = false;
        [HideReferenceObjectPicker, OdinSerialize] private StepCommandWrapper _stepCommandValidation;
        [Space(1)]
        [HideReferenceObjectPicker, OdinSerialize] private StepCommandWrapper _commandToDo;
        private IStepCommand _stepCommand;

        #region ICommand Methods
        public override IStepCommand Initialize()
        {
            _stepCommandValidation.StepCommand.Initialize();
            _commandToDo.StepCommand.Initialize();
            return base.Initialize();
        }
        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            base.Execute(onComplete);
            _stepCommand = _stepCommandValidation.StepCommand;
            _stepCommand.Execute(CompleteCommandValidation);
        }
        public override void InternalUndo()
        {
            _stepCommandValidation.StepCommand.Undo();
            _commandToDo.StepCommand.Undo();
            base.InternalUndo();
        }
        public override void Exit()
        {
            _stepCommand?.Exit();
            base.Exit();
        }
        protected override void Complete(bool right, IStepCommand stepCommand, bool callDelegate = true)
        {
            base.Complete(right, stepCommand, callDelegate);
        }
        #endregion
        #region Main Methods
        private void CompleteCommandValidation(bool right, IStepCommand stepCommand)
        {
            if (right != _shouldBeRight)
            {
                Complete(right, this);
                return;
            }
            _stepCommand = _commandToDo.StepCommand;
            _stepCommand.Execute(CompleteToDoStep);
        }
        protected virtual void CompleteToDoStep(bool right, IStepCommand stepCommand) => Complete(ReverseAmount == 0, this);
        #endregion
    }
}