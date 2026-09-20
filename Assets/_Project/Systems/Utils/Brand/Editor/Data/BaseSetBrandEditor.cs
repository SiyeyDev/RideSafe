using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

//[CustomEditor(typeof(BaseSetBrand<,>), true)]
public class BaseSetBrandEditor<T, V> : Editor where T : Component
{
    private SerializedProperty _dataProp;
    private SerializedProperty _targetProp;

    private void OnEnable()
    {
        _dataProp = serializedObject.FindProperty("data");
        _targetProp = serializedObject.FindProperty("target");

    }
    public override void OnInspectorGUI()
    {
        Debug.Log($"Not Implemented");
        
        serializedObject.Update();
        EditorGUILayout.PropertyField(_dataProp);
       // EditorGUILayout.ObjectField(_dataProp.objectReferenceValue, typeof(T), false);
        EditorGUILayout.PropertyField(_targetProp);
        serializedObject.ApplyModifiedProperties();
    }
}
