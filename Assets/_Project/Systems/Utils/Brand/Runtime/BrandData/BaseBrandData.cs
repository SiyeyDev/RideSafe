using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class BaseBrandData<T> : ScriptableObject 
{
    [SerializeField] private BrandSetting _brandSetting;
    [SerializeField] private List<BrandEntry> _brands = new();

    public T GetValue()
    {
        if (_brandSetting == null)
            return Error($"There is no {typeof(BrandSetting)} assigned in {name}");
        if (_brands == null)
            return Error($"Dictionary is not initialized");
        foreach (BrandEntry entry in _brands)
        {
            if (entry.brandId != _brandSetting.BrandID)
                continue;
            return entry.value;
        }
        return Error($"There is no brand with {_brandSetting.BrandID} attached");
    }
    public BrandID GetBrandID() => _brandSetting.BrandID;
    private T Error(string message) 
    {
        Debug.LogError(message);
        return default;
    }

    [Serializable]
    public class BrandEntry
    {
        public BrandID brandId;
        public T value;
    }
}
