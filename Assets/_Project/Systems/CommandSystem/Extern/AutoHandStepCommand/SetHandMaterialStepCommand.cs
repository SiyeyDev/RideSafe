using Sirenix.OdinInspector;
using StepCommand;
using System;
using UnityEngine;

[Serializable]
public class SetHandMaterialStepCommand : StepCommandClass
{
    [Header("SetHandMaterial Settings")]
    [SerializeField] private HandMaterialProvider _handMaterialProvider;
    [SerializeField] private Material _newMaterial;
    [SerializeField] private bool _oneHand;
    [ShowIf("_oneHand")][SerializeField] private bool _rightHand;

    #region ICommand Methods
    public override void Execute(Action<bool, IStepCommand> onComplete)
    {
        base.Execute(onComplete);
        Set();
        CoroutineCaller.Instance.WaitForNextFrame(() => Complete(true, this));
    }
    public override void InternalUndo()
    {
        Recover();
        base.InternalUndo();
    }
    #endregion

    private void Set()
    {
        if (!_oneHand)
        {
            _handMaterialProvider.SetMaterial(_newMaterial);
            return;
        }
        if (_rightHand)
            _handMaterialProvider.SetRightMaterial(_newMaterial);
        else
            _handMaterialProvider.SetLeftMaterial(_newMaterial);
    }
    private void Recover()
    {
        if (!_oneHand)
        {
            _handMaterialProvider.RecoverLastMaterial();
            return;
        }
        if (_rightHand)
            _handMaterialProvider.RecoverLastRightMaterial();
        else
            _handMaterialProvider.RecoverLastLeftMaterial();
    }
}
