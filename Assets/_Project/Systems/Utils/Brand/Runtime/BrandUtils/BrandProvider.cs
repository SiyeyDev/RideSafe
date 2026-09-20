using Sirenix.OdinInspector;
using System;
using UnityEngine;

[Serializable]
public class BrandProvider<T>
{
    [SerializeField] private bool _useBrand;
    [HideIf("_useBrand")][SerializeField] private T _value;
    [ShowIf("_useBrand")][SerializeField] private BaseBrandData<T> _branData;

    public T GetValue() => _useBrand ? _branData.GetValue() : _value;
}
