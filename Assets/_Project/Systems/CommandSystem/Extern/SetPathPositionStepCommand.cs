using PathCreation.Examples;
using StepCommand;
using System;
using UnityEngine;

[Serializable]
public class SetPathPositionStepCommand : StepCommandClass
{
    [Header("PathPostion Settings")]
    [SerializeField] private PathFollower _pahFollower;
    [SerializeField] private float _distanceTraveled;
    private float _lastDistanceTraveled;

    #region ICommand Methods
    public override void Execute(Action<bool, IStepCommand> onComplete)
    {
        _lastDistanceTraveled = _pahFollower.GetDistanceTraveled();
        _pahFollower.SetDistanceTraveled(_distanceTraveled);
        base.Execute(onComplete);
        CoroutineCaller.Instance.WaitForNextFrame(() => Complete(true, this));
    }
    public override void InternalUndo()
    {
        _pahFollower.SetDistanceTraveled(_lastDistanceTraveled);
    }
    #endregion

}
