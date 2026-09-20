using StepCommand;
using System;
using UnityEngine;

[Serializable]
public class SetNewRestStateGrabbable : StepCommandClass
{
    [SerializeField] private Transform _target;
    [SerializeField] private KeepPositionGrababble _keepPositionGrababble;

    private Vector3 _lastRestPosition;
    private Quaternion _lastRestRotation;

    #region ICommand Methods
    public override void Execute(Action<bool, IStepCommand> onComplete)
    {
        base.Execute(onComplete);
        _lastRestPosition = _keepPositionGrababble.GetRestPosition();
        _lastRestRotation = _keepPositionGrababble.GetRestRotation();
        _keepPositionGrababble.SetNewRestValues(_target.position, _target.rotation);
        CoroutineCaller.Instance.WaitForNextFrame(() => Complete(true, this));
    }
    public override void InternalUndo()
    {
        _keepPositionGrababble.SetNewRestValues(_lastRestPosition, _lastRestRotation);
        base.InternalUndo();
    }
    #endregion
}
