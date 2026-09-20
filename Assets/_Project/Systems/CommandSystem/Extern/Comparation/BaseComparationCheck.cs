using Comparation;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

[Serializable]
public class BaseComparationCheck<T> : IStepComparation
{
    [SerializeField] private BaseComparation _comparation;
    [SerializeField][HideReferenceObjectPicker] private SharedData _firstValue;
    [SerializeField][HideReferenceObjectPicker] private SharedData _secondValue;

    public bool Evaluate()
    {
        if (_comparation == null)
            throw new System.ArgumentNullException($"There is not {typeof(BaseComparation)} assigned");
        return _comparation.IsValid(_firstValue.GetValue(), _secondValue.GetValue());
    }

    /// <summary>
    /// This inner <c>SharedData</c> class duplicates functionality of 
    /// <see cref="SharedData{T}"/>, but is kept here for compatibility with 
    /// existing steps that already reference it. 
    /// Replacing it directly with <see cref="SharedData{T}"/> would break those references.
    /// </summary>
    [Serializable]
    private class SharedData
    {
        [HideInInspector, SerializeField] private bool _useSharedData;
        [InlineButton("ToogleSharedData", "SharedData")]
        [HideIf("_useSharedData")][SerializeField] private T _value;
        [InlineButton("ToogleSharedData", "Float")]
        [InlineEditor, HideLabel()]
        [ShowIf("_useSharedData")][SerializeField] private BaseShareDataSO<T> _sharedData;

        public T GetValue() => _useSharedData ? _sharedData.Value : _value;
#if UNITY_EDITOR
        private void ToogleSharedData() => _useSharedData = !_useSharedData;
#endif
    }
#if UNITY_EDITOR
    private string _name;

    public BaseComparationCheck()
    {
        _name = $"Using {typeof(T).Name}";  
    }
#endif
}
