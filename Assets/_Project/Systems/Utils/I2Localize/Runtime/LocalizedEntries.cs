using I2.Loc;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LocalizedEntries<T> : ISerializationCallbackReceiver
{
    [SerializeField] private T _defaulValue;
    [SerializeField] private Entry[] _entries;
    private Dictionary<string, T> _entriesDictionary = new Dictionary<string, T>();
    [Serializable]
    internal class Entry
    {
        [HideInInspector] public string language;
        public T value;
    }
    #region ICallback Methods 
    public void OnBeforeSerialize() { }
    public void OnAfterDeserialize() => RebuildDictionary();
    #endregion
    private void RebuildDictionary()
    {
        _entriesDictionary = new Dictionary<string, T>();
        foreach (var entry in _entries)
        {
            if (!string.IsNullOrEmpty(entry.language))
                _entriesDictionary[entry.language] = entry.value;
        }
    }
    public T GetValue()
    {
        if (_entriesDictionary == null || _entriesDictionary.Count != _entries.Length)
            RebuildDictionary();
        return _entriesDictionary.TryGetValue(LocalizationManager.CurrentLanguage, out var val) ? val : _defaulValue;
    }
}
