using Autohand;
using StepCommand;
using System;
using UnityEngine;

[Serializable]
public class HandGestureStepCommand : ParralleBaseActionStepCommand<HandGestureEvent>
{
    [SerializeField] private bool _considerStopPose;
    private Action _right;
    private Action _wrong;

    #region BaseAction Reusable Methods
    protected override void Set(bool state) => action.gameObject.SetActive(state);
    protected override void AddDelegates(Action right, Action wrong)
    {
        _right = right;
        _wrong = wrong;
        action.OnGestureStartEvent.AddListener(PoseStarted);
        if (_considerStopPose)
            action.OnGestureStopEvent.AddListener(PoseStopped);
    }
    protected override void RemoveDelegates(Action right, Action wrong)
    {
        _right = null;
        _wrong = null;
        action.OnGestureStartEvent.RemoveListener(PoseStarted);
        if (_considerStopPose)
            action.OnGestureStopEvent.RemoveListener(PoseStopped);
    }
    #endregion
    private void PoseStarted(Hand arg0, HandPoseGestureData arg1) => _right?.Invoke();
    private void PoseStopped(Hand arg0, HandPoseGestureData arg1) => _wrong?.Invoke();

}
