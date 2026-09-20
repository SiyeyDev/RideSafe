using StepCommand;
using System;
using UnityEngine;

[Serializable]
public class SetMaterialPropertyStepCommand : StepCommandClass
{
    [Header("SetMaterialProperty Settings")]
    [SerializeField] private MaterialProperty _materialProperty;
    #region ICommand Methods
    public override IStepCommand Initialize()
    {
        _materialProperty.Initialize();
        return base.Initialize();
    }
    public override void Execute(Action<bool, IStepCommand> onComplete)
    {
        base.Execute(onComplete);
        _materialProperty.StartSmoothValue();
        _materialProperty.SetValue();
        CoroutineCaller.Instance.WaitForNextFrame(() => Complete(true, this));
    }
    public override void InternalUndo()
    {
        _materialProperty.ResetValue();
        base.InternalUndo();
    }
    public override void Exit()
    {
        _materialProperty.SetValue();
        base.Exit();
    }
    #endregion
}
