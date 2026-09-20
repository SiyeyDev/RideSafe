using UnityEditor;
using UnityEngine;
[CustomPropertyDrawer(typeof(LocalizedEntries<float>))]

public class LocalizedEntriesFloatPropertyDrawer : LocalizedEntriesPropertyDrawer<float>
{
    #region  LocalizedEntriesPropertyDrawer Methods
    protected override void ShowField(Rect rect, string labelLanguage, SerializedProperty valueProp) => valueProp.floatValue = EditorGUI.FloatField(rect, labelLanguage, valueProp.floatValue);
    protected override float GetValue(SerializedProperty valueProp) => valueProp.floatValue;
    protected override void SetValue(SerializedProperty valueProp, float oldValue) => valueProp.floatValue = oldValue;
    #endregion
}
