using Sirenix.OdinInspector;
using System;
using UnityEngine;

public class BaseShareDataSO<T> : ScriptableObject
{
    [SerializeField] private T _value;
    public T Value
    {
        get => _value;
        set { _value = value; }
    }
}
[Serializable, InlineProperty]
public class SharedData<T>
{
    [HideInInspector, SerializeField] private bool _useSharedData;
    [InlineButton("ToogleSharedData", "SharedData")]
    [HideIf("_useSharedData"), LabelText("")][SerializeField] private T _value;
    [InlineButton("ToogleSharedData", "Float")]
    [InlineEditor, HideLabel()]
    [ShowIf("_useSharedData")][SerializeField] private BaseShareDataSO<T> _sharedData;

    public T GetValue() => _useSharedData ? _sharedData.Value : _value;

#if UNITY_EDITOR

    private void ToogleSharedData() => _useSharedData = !_useSharedData;
#endif
}
