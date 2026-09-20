using I2.Loc;
using Cachacos;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SimpleI2LocalizeTranslate<,>), true)]
public abstract class SimpleI2LocalizeTranslateEditor<T, V> : Editor where T : Component where V : UnityEngine.Object
{
    private SimpleI2LocalizeTranslate<T, V> _simpleI2LoalizeTranslate;
    private string[] _languages;
    private SerializedProperty _targetField;
    private SerializedProperty _dataList;
    private GUIStyle _buttonStyle;
    private readonly Color _textColor = new Color(0.9f, 0.9f, 0.9f);
    private readonly Color _buttonColor = new Color(0.1f, 0.1f, 0.1f);
    private bool show;
    private void OnEnable()
    {
        _languages = LocalizationManager.GetAllLanguages().ToArray();
        Color _buttonColor = Color.cyan;
        _targetField = serializedObject.FindProperty("_target");
        if (_targetField.objectReferenceValue == null)
        {
            Component component = serializedObject.targetObject as Component;
            if (component != null)
                _targetField.objectReferenceValue = component.GetComponent<T>();
            serializedObject.ApplyModifiedProperties();
        }
        _dataList = serializedObject.FindProperty("_data");
        if (_simpleI2LoalizeTranslate == null)
            _simpleI2LoalizeTranslate = target as SimpleI2LocalizeTranslate<T, V>;
        if (!EditorApplication.isPlaying)
            _simpleI2LoalizeTranslate.SetEvent(false);
    }
    private void OnDisable()
    {
        if (!EditorApplication.isPlaying)
            _simpleI2LoalizeTranslate.SetEvent(true);
    }
    private void EnsureAllLanguagesExist()
    {
        Dictionary<string, V> tempDict = new Dictionary<string, V>();
        for (int i = 0; i < _dataList.arraySize; i++)
        {
            SerializedProperty entry = _dataList.GetArrayElementAtIndex(i);
            string lang = entry.FindPropertyRelative("language").stringValue;
            V value = (V)entry.FindPropertyRelative("value").objectReferenceValue;
            tempDict[lang] = value;
        }

        _dataList.ClearArray();
        _dataList.arraySize = _languages.Length;

        for (int i = 0; i < _languages.Length; i++)
        {
            SerializedProperty newEntry = _dataList.GetArrayElementAtIndex(i);
            string lang = _languages[i];
            newEntry.FindPropertyRelative("language").stringValue = lang;

            if (tempDict.TryGetValue(lang, out var existingValue))
                newEntry.FindPropertyRelative("value").objectReferenceValue = existingValue;
            else
                newEntry.FindPropertyRelative("value").objectReferenceValue = null;
        }
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EnsureAllLanguagesExist();
        _buttonStyle = EditorGUIUtils.GetButtonStyle(_textColor, _buttonColor);
        DrawTarget();
        EditorGUILayout.Space(1);
        GUILayout.BeginVertical(_buttonStyle);
        show = EditorGUILayout.Foldout(show, "Terms");
        if (show)
        {
            EditorGUI.indentLevel++;
            DrawObjectFieldGrid();
            EditorGUI.indentLevel--;
        }
        GUILayout.EndVertical();
        EditorGUILayout.Space(1);
        GUIContent buttonText = new GUIContent("Update");
        _simpleI2LoalizeTranslate.UpdateDictionary();
        if (GUILayout.Button(buttonText, _buttonStyle))
            _simpleI2LoalizeTranslate.UpdateLanguage();
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }
    private void DrawObjectFieldGrid()
    {
        GUILayout.BeginVertical(_buttonStyle);
        foreach (string language in _languages)
            ShowFieldObjectByType(language);
        GUILayout.EndVertical();
    }
    protected void ShowFieldObjectByType(string language)
    {
        Type type = typeof(V);
        _buttonStyle.name = language;
        EditorGUILayout.Space(1);
        int index = _languages.GetIndex(language);
        var entry = _dataList.GetArrayElementAtIndex(index);
        var langProp = entry.FindPropertyRelative("language");
        var valProp = entry.FindPropertyRelative("value");
        valProp.objectReferenceValue = (V)EditorGUILayout.ObjectField($"{language}", valProp.objectReferenceValue, type, false, GUILayout.Height(EditorGUIUtility.singleLineHeight));
    }
    private void DrawTarget()
    {
        GUILayout.BeginVertical(_buttonStyle);
        EditorGUILayout.PropertyField(_targetField, true);
        GUILayout.EndVertical();
    }
}
