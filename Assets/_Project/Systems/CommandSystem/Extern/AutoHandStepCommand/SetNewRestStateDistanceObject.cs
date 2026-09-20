using StepCommand;
using System;
using UnityEngine;

[Serializable]
public class SetNewRestStateDistanceObject : StepCommandClass
{
    [SerializeField] private Transform _target;
    [SerializeField] private KeepPositionDistanceObject _keepPositionDistanceObject;

    private Vector3 _lastRestPosition;
    private Quaternion _lastRestRotation;

    #region ICommand Methods
    public override void Execute(Action<bool, IStepCommand> onComplete)
    {
        base.Execute(onComplete);
        _lastRestPosition = _keepPositionDistanceObject.GetRestPosition();
        _lastRestRotation = _keepPositionDistanceObject.GetRestRotation();
        _keepPositionDistanceObject.SetNewRestValues(_target.position, _target.rotation);
        CoroutineCaller.Instance.WaitForNextFrame(() => Complete(true, this));
    }
    public override void InternalUndo()
    {
        _keepPositionDistanceObject.SetNewRestValues(_lastRestPosition, _lastRestRotation);
        base.InternalUndo();
    }
    #endregion
}
