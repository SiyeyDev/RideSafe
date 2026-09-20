using Sirenix.OdinInspector;
using StepCommand;
using System;
using UnityEngine;

[Serializable]
public class SetParentTransformStepCommand : StepCommandClass
{
    [Header("SetParentTransformStepCommand settings")]
    [SerializeField] private Transform _targetTransform;
    [SerializeField] private Transform _parentTransform;
    [SerializeField] private bool _keepGlobal;
    [ HideIf("_keepGlobal")][SerializeField] private bool _cosiderLocalValues;
    private TransformParentSettings _defaultParentSettings;
    private TransformParentSettings _parentSettings;

    #region ICommand Methods
    public override IStepCommand Initialize()
    {
        _parentSettings = new TransformParentSettings(_parentTransform, !_cosiderLocalValues);
        _defaultParentSettings = new TransformParentSettings(_targetTransform.parent, false);
        return base.Initialize();
    }
    public override void Execute(Action<bool, IStepCommand> onComplete)
    {
        base.Execute(onComplete);
        _parentSettings.SetParent(_targetTransform, _keepGlobal);
        Complete(true, this);
    }
    public override void InternalUndo()
    {
        _defaultParentSettings.SetParent(_targetTransform);
        base.InternalUndo();
    }
    #endregion
}
