using I2.Loc;
using Cachacos;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public abstract class LocalizedEntriesPropertyDrawer<T> : BasePropertyDrawer
{
    private readonly string _entriesName = "_entries";
    private readonly string _languageName = "language";
    private readonly string _valueName = "value";
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        if (property.isExpanded)
            height += EditorGUIUtility.singleLineHeight * (LocalizationManager.GetAllLanguages().Count + 2);
        return height;
    }
    public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(rect, label, property);
        rect.height = EditorGUIUtility.singleLineHeight;
        property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label, toggleOnLabelClick: true);
        SerializedProperty defaultValueProp = property.FindPropertyRelative("_defaulValue");
        using (new EditorGUI.DisabledScope(true))
            EditorGUI.LabelField(rect, ".", $"Default value {GetValue(defaultValueProp)}");
        if (property.isExpanded)
        {
            string[] languages = LocalizationManager.GetAllLanguages().ToArray();
            SerializedProperty entriesProp = property.FindPropertyRelative(_entriesName);
            EnsureEntriesMatchLanguages(entriesProp, languages);
            rect.y += EditorGUIUtility.singleLineHeight;
            ShowField(rect, "Defaul Value", defaultValueProp);
            rect.y += EditorGUIUtility.singleLineHeight;
            for (int i = 0; i < languages.Length; i++)
            {
                SerializedProperty entryProp = entriesProp.GetArrayElementAtIndex(i);
                SerializedProperty langProp = entryProp.FindPropertyRelative(_languageName);
                SerializedProperty valueProp = entryProp.FindPropertyRelative(_valueName);
                rect.y += EditorGUIUtility.singleLineHeight;
                ShowField(rect, languages[i], valueProp);
                langProp.stringValue = languages[i];
            }
        }
        EditorGUI.EndProperty();

    }
    private void EnsureEntriesMatchLanguages(SerializedProperty entriesProp, IList<string> languages)
    {
        Dictionary<string, T> existingValues = new Dictionary<string, T>();
        for (int i = 0; i < entriesProp.arraySize; i++)
        {
            SerializedProperty entry = entriesProp.GetArrayElementAtIndex(i);
            SerializedProperty langProp = entry.FindPropertyRelative(_languageName);
            SerializedProperty valueProp = entry.FindPropertyRelative(_valueName);
            existingValues[langProp.stringValue] = GetValue(valueProp);
        }
        entriesProp.ClearArray();
        for (int i = 0; i < languages.Count; i++)
            entriesProp.InsertArrayElementAtIndex(i);
        for (int i = 0; i < languages.Count; i++)
        {
            SerializedProperty entry = entriesProp.GetArrayElementAtIndex(i);
            SerializedProperty langProp = entry.FindPropertyRelative(_languageName);
            SerializedProperty valueProp = entry.FindPropertyRelative(_valueName);
            string lang = languages[i];
            langProp.stringValue = lang;
            if (existingValues.TryGetValue(lang, out T oldValue))
            {
                SetValue(valueProp, oldValue);
            }
        }
    }

    #region  LocalizedEntriesPropertyDrawer Methods
    protected abstract void ShowField(Rect rect, string labelLanguage, SerializedProperty valueProp);
    protected abstract T GetValue(SerializedProperty valueProp);
    protected abstract void SetValue(SerializedProperty valueProp, T oldValue);
    #endregion
}
