using I2.Loc;
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class SimpleI2LocalizeTranslate<T, V> : MonoBehaviour where T : Component where V : UnityEngine.Object
{
    [SerializeField] private T _target;
    [SerializeField] private LocalizedEntry[] _data;
    private Dictionary<string, V> _dictionary;
    private bool _setted;
    public void Start()
    {
        SetEvent(true);
        UpdateLanguage();
    }
    private void OnEnable()
    {
        UpdateDictionary();
        UpdateLanguage();
    }
    public void OnDestroy()
    {
        SetEvent(false);
    }
    public void UpdateLanguage()
    {
        if (_dictionary == null )
            return;
        if (_dictionary == null )
            return;
        if (_target == null)
            return;
        UpdateDictionary();
        SetTerm(_target, _dictionary[LocalizationManager.CurrentLanguage]);
    }
    #region SimpleI2Localize Methods
    protected abstract void SetTerm(T target, V value);
    #endregion
    public void UpdateDictionary()
    {
        if (_data == null)
            return ;
        _dictionary = new Dictionary<string, V>();
        foreach(LocalizedEntry entry in _data)
            _dictionary.Add(entry.language,entry.value);
    }
    public void SetEvent(bool state)
    {
        if (_setted == state)
            return;
        _setted = state;
        if (_setted)
            LocalizationManager.OnLocalizeEvent += UpdateLanguage;
        else

            LocalizationManager.OnLocalizeEvent -= UpdateLanguage;
    }
    [Serializable]
    public class LocalizedEntry
    {
        [HideInInspector]public string language;
        public V value;
    }


}
