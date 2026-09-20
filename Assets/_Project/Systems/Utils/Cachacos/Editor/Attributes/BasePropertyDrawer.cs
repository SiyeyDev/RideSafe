#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Cachacos
{
    public class BasePropertyDrawer : PropertyDrawer
    {
        protected string lastParameterName = "No Selected";
        protected Rect CreateRect(Rect baseRect, float xOffset = 0, float yOffset = 0, float widthOffset = 0, float heightOffset = 0)
        {
            return new Rect()
            {
                x = baseRect.x + xOffset,
                y = baseRect.y + yOffset,
                width = baseRect.width + widthOffset,
                height = EditorGUIUtility.singleLineHeight + heightOffset,
            };
        }
        protected bool ParametersPopup(Rect position, string label, int selectedIndex, string[] parameters, out int newIndex)
        {
            newIndex = EditorGUI.Popup(position, label, selectedIndex, parameters);
            if (newIndex >= parameters.Length)
                return false;
            if (newIndex < 0)
                return false;
            lastParameterName = parameters[newIndex];
            return true;
        }
        protected void ShowHeader(ref Rect position, string label)
        {
            position.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(position, new GUIContent(label), EditorStyles.boldLabel);
        }
        protected SerializedProperty ShowProperty(ref Rect position, SerializedProperty property, string name, bool isArray = false) => ShowPoperty(ref position, property.FindPropertyRelative(name), isArray);
        protected SerializedProperty ShowPoperty(ref Rect position, SerializedProperty property, bool isArray = false)
        {
            position.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(position, property,isArray);
            if (isArray && property.isExpanded)
                position.y += EditorGUI.GetPropertyHeight(property,true);
            return property;
        }
        protected void ShowParentProperties(ref Rect position, SerializedProperty property)
        {
            SerializedProperty copy = property.Copy();
            SerializedProperty end = property.GetEndProperty();
            int startDepth = copy.depth;
            while (copy.NextVisible(true) && !SerializedProperty.EqualContents(copy, end))
            {
                if (copy.depth > startDepth + 1)
                    continue;
                if (IsPropertyDeclaredInClass(copy, fieldInfo.FieldType))
                    continue;
                EditorGUI.PropertyField(position, copy, true);
                position.y += EditorGUI.GetPropertyHeight(copy);
            }
        }
        protected void GettingParnetPropiertiesHeight(ref float height, SerializedProperty property)
        {
            SerializedProperty copy = property.Copy();
            SerializedProperty end = property.GetEndProperty();
            int startDepth = copy.depth;
            while (copy.NextVisible(true) && !SerializedProperty.EqualContents(copy, end))
            {
                if (copy.depth > startDepth + 1)
                    continue;
                if (IsPropertyDeclaredInClass(copy, fieldInfo.FieldType))
                    continue;
                height += EditorGUI.GetPropertyHeight(copy, true);
            }
        }
        private bool IsPropertyDeclaredInClass(SerializedProperty property, Type targetType)
        {
            Type objectType = targetType;
            while (objectType != null)
            {
                if (objectType.GetField(property.name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) != null)
                    return objectType == targetType;
                objectType = objectType.BaseType;
            }
            return false;
        }
        protected void ShowDefaultsPorpierties(ref Rect position, SerializedProperty property, SerializedProperty matchProperty)
        {
            SerializedProperty copy = property.Copy();
            int startDepth = copy.depth;
            while (copy.NextVisible(true) && !SerializedProperty.EqualContents(copy, matchProperty))
            {
                if (copy.depth > startDepth + 1)
                    continue;
                EditorGUI.PropertyField(position, copy, true);
                position.y += EditorGUI.GetPropertyHeight(copy);
            }
        }
        protected void GettingDefaultPropiertiesHeight(ref float height, SerializedProperty property, SerializedProperty matchProperty)
        {
            SerializedProperty copy = property.Copy();
            int startDepth = copy.depth;
            while (copy.NextVisible(true) && !SerializedProperty.EqualContents(copy, matchProperty))
            {
                if (copy.depth > startDepth + 1)
                    continue;
                height += EditorGUI.GetPropertyHeight(copy, true);
            }
        }
        protected void ShowRangeProperties(ref Rect position, SerializedProperty property, string startName, string endName)
        {
            SerializedProperty copy = property.Copy();
            SerializedProperty startProperty = property.FindPropertyRelative(startName);
            SerializedProperty enProperty = property.FindPropertyRelative(endName);
            int startDepth = copy.depth;
            bool doLoop = true;
            bool showPropierties = false;
            while (copy.NextVisible(true) && doLoop)
            {
                if (!SerializedProperty.EqualContents(copy, startProperty) && !showPropierties)
                    continue;
                showPropierties = true;
                if (SerializedProperty.EqualContents(copy, enProperty))
                    doLoop = false;
                EditorGUI.PropertyField(position, copy, true);
                if (doLoop)
                    position.y += EditorGUI.GetPropertyHeight(copy);
            }
        }
        protected float GetRangePropertiesHeight(SerializedProperty property, string startName, string endName)
        {
            float height = 0;
            SerializedProperty copy = property.Copy();
            SerializedProperty startProperty = property.FindPropertyRelative(startName);
            SerializedProperty enProperty = property.FindPropertyRelative(endName);
            int startDepth = copy.depth;
            bool doLoop = true;
            bool showPropierties = false;
            while (copy.NextVisible(true) && doLoop)
            {
                if (!SerializedProperty.EqualContents(copy, startProperty) && !showPropierties)
                    continue;
                showPropierties = true;
                if (SerializedProperty.EqualContents(copy, enProperty))
                    doLoop = false;
                height += EditorGUI.GetPropertyHeight(copy, true);
            }
            return height;
        }

        protected bool ShowBoolfoldout(Rect position, SerializedProperty property, string propertyName) => ShowBoolfoldout(position, property, property.FindPropertyRelative(propertyName));
        protected bool ShowBoolfoldout(Rect position, SerializedProperty property, SerializedProperty foldoutProperty)
        {
            if (!foldoutProperty.boolValue)
            {
                EditorGUI.PropertyField(position, foldoutProperty);
                return false;
            }
            Rect fouldoutRect = position;
            fouldoutRect.width = EditorGUIUtility.labelWidth - EditorGUIUtility.singleLineHeight;
            EditorGUI.indentLevel++;
            SetfoldoutExpanded(foldoutProperty, EditorGUI.Foldout(fouldoutRect, IsfoldoutExpanded(foldoutProperty), foldoutProperty.displayName, toggleOnLabelClick: true));
            EditorGUI.indentLevel--;
            EditorGUI.PropertyField(position, foldoutProperty, new GUIContent($" "));
            position.y += EditorGUIUtility.singleLineHeight;
            return IsfoldoutExpanded(foldoutProperty);
        }
        protected bool Boolfoldout(SerializedProperty property, string propertyName) => Boolfoldout(property.FindPropertyRelative(propertyName));
        protected bool Boolfoldout(SerializedProperty property) => property.boolValue && IsfoldoutExpanded(property);
        protected bool IsfoldoutExpanded(SerializedProperty property) => EditorPrefs.GetBool(property.GetPath(true), false);
        protected void SetfoldoutExpanded(SerializedProperty property, bool isExpanded) => EditorPrefs.SetBool(property.GetPath(true), isExpanded);

    }
}
#endif
