using StepCommand;
using System;
using UnityEngine;

[Serializable]
public class ValidationStepCommand : StepCommandClass
{
    [Header("Validation Settings")]
    [SerializeField] private BaseValidation _validation;

    #region ICommand Methods
    public override void Execute(Action<bool, IStepCommand> onComplete)
    {
        base.Execute(onComplete);
        CoroutineCaller.Instance.WaitForNextFrame(() => Complete(_validation.IsValid(), this));
    }
    #endregion
}
