#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Cachacos
{
    public class OdinBasePropertyDrawer<TAttribute, TValue> : OdinAttributeDrawer<SelectAnimationClipAttribute, int>
    {
        protected bool isExpanded;
        protected string lastParameterName;

        protected void ShowFoldout(out Rect labelRect)
        {
            Rect rect = EditorGUILayout.GetControlRect();
            Rect foldoutRect = rect;
            foldoutRect.width = 14f;
            isExpanded = EditorGUI.Foldout(foldoutRect, isExpanded, GUIContent.none);
            labelRect = rect;
            labelRect.xMin += 16f;
        }
        protected bool ParametersPopup(string label, int selectedIndex, string[] parameters, out int newIndex)
        {
            newIndex = EditorGUILayout.Popup(label, selectedIndex, parameters);
            if (newIndex >= parameters.Length)
                return false;
            if (newIndex < 0)
                return false;
            lastParameterName = parameters[newIndex];
            return true;
        }
    }
}

#endif