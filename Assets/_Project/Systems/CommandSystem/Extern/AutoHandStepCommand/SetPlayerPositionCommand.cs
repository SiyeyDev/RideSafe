using StepCommand;
using System;
using UnityEngine;

[Serializable]
public class SetPlayerPositionCommand : StepCommandClass
{
    [Header("SetPlayerPositionCommand settings")]
    [SerializeField] private SetPlayerPosition _setPlayerPosition;
    #region ICommand Methods
    public override void Execute(Action<bool, IStepCommand> onComplete)
    {
        base.Execute(onComplete);
        _setPlayerPosition.Move();
        Complete(true, this);
    }
    #endregion
}
