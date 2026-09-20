
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BaseBrandData<>), true)]
public abstract class BaseBrandDataEditor<T> : Editor
{
    private List<BaseBrandData<T>.BrandEntry> _entries;
    private SerializedProperty _logoSettingProp;
    private bool _showBrands;
    private bool _addNewEntry;
    private BrandID _newKey;
    private T _newValue;

    private void OnEnable()
    {
        _logoSettingProp = serializedObject.FindProperty("_brandSetting");
        _entries = GetLogoEntryList();
        _showBrands = true;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        GUI.enabled = false;
        EditorGUILayout.PropertyField(_logoSettingProp);
        GUI.enabled = true;
        EditorGUILayout.Space();

        _showBrands = EditorGUILayout.Foldout(_showBrands, "Brand Entries");
        if (_showBrands)
        {
            if (_entries == null)
                _entries = GetLogoEntryList();
            ShowLogoEntryList();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void ShowLogoEntryList()
    {
        EditorGUI.indentLevel++;

        Dictionary<BrandID, int> seenIDs = new();
        List<int> toRemove = new();

        for (int i = 0; i < _entries.Count; i++)
        {
            BaseBrandData<T>.BrandEntry entry = _entries[i];
            EditorGUILayout.BeginHorizontal();
            entry.brandId = (BrandID)EditorGUILayout.ObjectField(entry.brandId, typeof(BrandID), false);
            entry.value = DrawGenericValue(entry.value);
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
                toRemove.Add(i);
            EditorGUILayout.EndHorizontal();
            if (seenIDs.ContainsKey(entry.brandId))
                EditorGUILayout.HelpBox($"Duplicate BranID '{entry.brandId}' found. Only the first one will be used.", MessageType.Warning);
            else
            {
                seenIDs.Add(entry.brandId, i);
                EditorUtility.SetDirty(target);
            }
        }
        foreach (int index in toRemove.OrderByDescending(i => i))
        {
            _entries.RemoveAt(index);
            EditorUtility.SetDirty(target);
        }
        EditorGUILayout.Space();
        _addNewEntry = EditorGUILayout.Foldout(_addNewEntry, "Add new Entries");

        if (!_addNewEntry)
            return;
        EditorGUILayout.BeginHorizontal();
        AddNewEntry();
        EditorGUILayout.EndHorizontal();
        EditorGUI.indentLevel--;
    }

    private List<BaseBrandData<T>.BrandEntry> GetLogoEntryList()
    {
        var field = EditorUtils.GetField(target.GetType(), "_brands");
        return field?.GetValue(target) as List<BaseBrandData<T>.BrandEntry>;
    }

    private void AddNewEntry()
    {
        _newKey = (BrandID)EditorGUILayout.ObjectField(_newKey, typeof(BrandID), false);
        _newValue = DrawGenericValue(_newValue);
        if (_newKey == null || _newValue == null)
        {
            EditorGUILayout.HelpBox($"Plese select {typeof(BrandID)} and {typeof(T)}", MessageType.Warning);
            return;
        }
        bool canAdd = _newValue != null && !_entries.Exists(e => e.brandId.Equals(_newKey));
        if (!canAdd)
        {
            EditorGUILayout.HelpBox($"Duplicate BrandID '{_newKey}' found. You can't add new one.", MessageType.Warning);
            return;
        }
        if (GUILayout.Button("Add", GUILayout.Width(70)))
        {
            _entries.Add(new BaseBrandData<T>.BrandEntry { brandId = _newKey, value = _newValue });
            EditorUtility.SetDirty(target);
            _newValue = default;
        }
    }
    #region BaseBrandDataEditor Methods
    protected abstract T DrawGenericValue(T value);
    #endregion
}
