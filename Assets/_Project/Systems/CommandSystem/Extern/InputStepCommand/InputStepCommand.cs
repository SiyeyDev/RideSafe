using StepCommand;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InputStepCommand : StepCommandClass
{
    #region test
    //#region BaseAction Reusable Methods

    //protected override void AddDelegates(Action right, Action wrong) => action.AddDelegates(right, wrong);
    //protected override void RemoveDelegates(Action right, Action wrong) => action.RemoveDelegates(right, wrong);
    //protected override void Set(bool state) => action.Set(state);



    //#endregion
    //[Serializable]
    //public class Input
    //{


    //    [SerializeField] private InputActionProperty _inputReference;
    //    [SerializeField] private UnityEvent _onInputDown;
    //    [SerializeField] private UnityEvent _onInputRelease;
    //    private bool _subscribed;
    //    private Action _right;
    //    private Action _wrong;
    //    public void AddDelegates(Action right, Action wrong)
    //    {
    //        _right = right;
    //        _wrong = wrong;

    //        if (_subscribed)
    //            return;

    //        _subscribed = true;
    //        _inputReference.action.performed += OnInputDown;
    //        _inputReference.action.canceled += OnInputRelease;
    //    }

    //    public void RemoveDelegates(Action right, Action wrong)
    //    {
    //        _right = null;
    //        _wrong = null;

    //        if (!_subscribed)
    //            return;

    //        _subscribed = false;
    //        _inputReference.action.performed -= OnInputDown;
    //        _inputReference.action.canceled -= OnInputRelease;
    //    }

    //    public void Set(bool state)
    //    {
    //        if (_inputReference.action == null)
    //            return;

    //        if (state) _inputReference.action.Enable();
    //        else _inputReference.action.Disable();
    //    }
    //    private void OnInputDown(InputAction.CallbackContext ctx)
    //    {
    //        Debug.Log($"Input {_inputReference.action.name} DOWN");

    //        _onInputDown?.Invoke();
    //        _right?.Invoke();
    //    }

    //    private void OnInputRelease(InputAction.CallbackContext ctx)
    //    {
    //        Debug.Log($"Input {_inputReference.action.name} RELEASE");

    //        _onInputRelease?.Invoke();
    //        _wrong?.Invoke();
    //    }
    //}
    #endregion test
    [SerializeField] private InputActionReference _actionReference;

    public override void Execute(Action<bool, IStepCommand> onComplete)
    {
        base.Execute(onComplete);
        _actionReference.action.started += OnInput;
        _actionReference.action.Enable();
    }

    public override void Exit()
    {
        _actionReference.action.started -= OnInput;

        _actionReference.action.Disable();

    }

    #region Main Methods
    private void OnInput(InputAction.CallbackContext context)
    {
        _actionReference.action.started -= OnInput;
        Complete(true, this);
    }
    #endregion
}