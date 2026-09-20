#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;


[CustomPropertyDrawer(typeof(Axis))]
public class AxisPropertyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float propertyHeight = base.GetPropertyHeight(property, label);
        if (property.isExpanded)
            propertyHeight += EditorGUIUtility.singleLineHeight * 3;
        return propertyHeight;
    }
    public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(rect, GUIContent.none, property);
        rect.height = EditorGUIUtility.singleLineHeight;
        property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label, toggleOnLabelClick: true);
        SerializedProperty xProperty = property.FindPropertyRelative("x");
        SerializedProperty yProperty = property.FindPropertyRelative("y");
        SerializedProperty zProperty = property.FindPropertyRelative("z");
        Vector3 vector3 = new Vector3(xProperty.floatValue, yProperty.floatValue, zProperty.floatValue);
        using (new EditorGUI.DisabledScope(true))
            EditorGUI.Vector3Field(rect, ".", vector3);
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            Rect fieldPositions = rect;
            fieldPositions.y += EditorGUIUtility.singleLineHeight;
            xProperty.floatValue = EditorGUI.Slider(fieldPositions, new GUIContent("X"), xProperty.floatValue, -1, 1);
            fieldPositions.y += EditorGUIUtility.singleLineHeight;
            yProperty.floatValue = EditorGUI.Slider(fieldPositions, new GUIContent("Y"), yProperty.floatValue, -1, 1);
            fieldPositions.y += EditorGUIUtility.singleLineHeight;
            zProperty.floatValue = EditorGUI.Slider(fieldPositions, new GUIContent("Z"), zProperty.floatValue, -1, 1);
            EditorGUI.indentLevel--;
        }
        EditorGUI.EndProperty();
    }
}
#endif