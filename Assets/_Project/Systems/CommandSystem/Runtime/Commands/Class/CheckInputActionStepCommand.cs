using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StepCommand
{
    [Serializable]
    public class CheckInputActionStepCommand : StepCommandClass
    {
        [SerializeField] private InputActionProperty _inputReference;
        [EnumToggleButtons]
        [SerializeField] private Actions _actionType;
        private enum Actions
        {
            Started,
            Performed,
            Canceled
        }
        #region ICommand Methods
        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            base.Execute(onComplete);
            _inputReference.action.Enable();
            AddDelegates();
        }
        public override void Exit()
        {
            RemoveDelegates();
            base.Exit();
        }
        protected override void Complete(bool right, IStepCommand stepCommand)
        {
            Exit();
            base.Complete(right, stepCommand);
        }
        #endregion

        #region Main Mehods
        private void AddDelegates()
        {
            _inputReference.action.started += OnStaretd;
            _inputReference.action.performed += OnPerfomed;
            _inputReference.action.canceled += OnCanceled;
        }
        private void RemoveDelegates()
        {
            _inputReference.action.started -= OnStaretd;
            _inputReference.action.performed -= OnPerfomed;
            _inputReference.action.canceled -= OnCanceled;
            _inputReference.action.Disable();
        }
        private void OnStaretd(InputAction.CallbackContext ctx) => TryComplete(Actions.Started);
        private void OnPerfomed(InputAction.CallbackContext ctx) => TryComplete(Actions.Performed);
        private void OnCanceled(InputAction.CallbackContext ctx) => TryComplete(Actions.Canceled);
        private void TryComplete(Actions actions)
        {
            if (actions.Equals(_actionType))
                Complete(true, this);
        }
        #endregion

    }
}