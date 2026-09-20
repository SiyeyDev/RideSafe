using Sirenix.OdinInspector;
using StepCommand;
using System;
using UnityEngine;

[Serializable]
public class SetSharedFloatStepCommand : StepCommandClass
{
    [Header("SetSharedFloatStepCommand Settings")]
    [SerializeField] private FloatShareDataSO _floatSharedToChange;
    [HideInInspector, SerializeField] private bool _useSharedData;
    [InlineButton("ToogleSharedData", "Float")]
    [InlineEditor, LabelText("New Shared Float")]
    [ShowIf("_useSharedData")]
    [SerializeField] private FloatShareDataSO _floatSharedValue;
    [InlineButton("ToogleSharedData", "SharedData")]
    [HideIf("_useSharedData"), LabelText("New Float")][SerializeField] private float _value;
    [SerializeField] private bool _addValue;
    private float _lastValue;
    #region ICommand Methods
    
    public override void Execute(Action<bool, IStepCommand> onComplete)
    {
        base.Execute(onComplete);
        SetValue();
        CoroutineCaller.Instance.WaitForNextFrame(() => Complete(true, this));
    }
    public override void InternalUndo()
    {
        ResetValue();
        base.InternalUndo();
    }
    #endregion
    private void SetValue()
    {
        if (_addValue)
        {
            _floatSharedToChange.Value += GetValue();
            return;
        }
        _lastValue = _floatSharedToChange.Value;
        _floatSharedToChange.Value = GetValue();
    }
    private void ResetValue()
    {
        if (_addValue)
        {
            _floatSharedToChange.Value -= GetValue();
            return;
        }
        _floatSharedToChange.Value = _lastValue;
    }

    private float GetValue() => _useSharedData ? _floatSharedValue.Value : _value;

#if UNITY_EDITOR
    private void ToogleSharedData() => _useSharedData = !_useSharedData;
#endif
}
