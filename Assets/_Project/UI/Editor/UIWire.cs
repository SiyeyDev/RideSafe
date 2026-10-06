using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RideSafe.UI.EditorTools
{
    /// <summary>
    /// Assigns private [SerializeField] members through SerializedObject, so runtime scripts
    /// keep their fields private and the prefab still ships fully wired. Every setter fails
    /// loudly on a typo'd field name instead of leaving a silent null in the prefab.
    /// </summary>
    internal static class UIWire
    {
        public static void Ref(Object target, string field, Object value) =>
            Edit(target, field, p => p.objectReferenceValue = value);

        public static void Str(Object target, string field, string value) =>
            Edit(target, field, p => p.stringValue = value);

        public static void Bool(Object target, string field, bool value) =>
            Edit(target, field, p => p.boolValue = value);

        public static void Int(Object target, string field, int value) =>
            Edit(target, field, p => p.intValue = value);

        public static void Float(Object target, string field, float value) =>
            Edit(target, field, p => p.floatValue = value);

        public static void Col(Object target, string field, Color value) =>
            Edit(target, field, p => p.colorValue = value);

        public static void Vec2(Object target, string field, Vector2 value) =>
            Edit(target, field, p => p.vector2Value = value);

        public static void Refs<T>(Object target, string field, IList<T> values) where T : Object
        {
            Edit(target, field, p =>
            {
                p.arraySize = values.Count;
                for (int i = 0; i < values.Count; i++)
                    p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            });
        }

        public static void Strings(Object target, string field, IList<string> values)
        {
            Edit(target, field, p =>
            {
                p.arraySize = values.Count;
                for (int i = 0; i < values.Count; i++)
                    p.GetArrayElementAtIndex(i).stringValue = values[i];
            });
        }

        /// <summary>uGUI SpriteState struct stored in a private field (e.g. ToggleVisual).</summary>
        public static void SpriteStateField(Object target, string field, UnityEngine.UI.SpriteState state)
        {
            Edit(target, field, p =>
            {
                p.FindPropertyRelative("m_HighlightedSprite").objectReferenceValue = state.highlightedSprite;
                p.FindPropertyRelative("m_PressedSprite").objectReferenceValue = state.pressedSprite;
                p.FindPropertyRelative("m_SelectedSprite").objectReferenceValue = state.selectedSprite;
                p.FindPropertyRelative("m_DisabledSprite").objectReferenceValue = state.disabledSprite;
            });
        }

        /// <summary>List of ComparisonEntry structs.</summary>
        public static void Entries(Object target, string field, IList<ComparisonEntry> entries)
        {
            Edit(target, field, p =>
            {
                p.arraySize = entries.Count;
                for (int i = 0; i < entries.Count; i++)
                {
                    SerializedProperty element = p.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("Status").enumValueIndex = (int)entries[i].Status;
                    element.FindPropertyRelative("Title").stringValue = entries[i].Title ?? string.Empty;
                    element.FindPropertyRelative("Detail").stringValue = entries[i].Detail ?? string.Empty;
                }
            });
        }

        /// <summary>ChoicePanel option list (Toggle + id + display name).</summary>
        public static void Options(Object target, string field, IList<ChoicePanel.Option> options)
        {
            Edit(target, field, p =>
            {
                p.arraySize = options.Count;
                for (int i = 0; i < options.Count; i++)
                {
                    SerializedProperty element = p.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("Toggle").objectReferenceValue = options[i].Toggle;
                    element.FindPropertyRelative("Id").stringValue = options[i].Id;
                    element.FindPropertyRelative("DisplayName").stringValue = options[i].DisplayName;
                }
            });
        }

        /// <summary>ModuleUI panel list (id + root).</summary>
        public static void Panels(Object target, string field, IList<ModuleUI.Panel> panels)
        {
            Edit(target, field, p =>
            {
                p.arraySize = panels.Count;
                for (int i = 0; i < panels.Count; i++)
                {
                    SerializedProperty element = p.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("Id").stringValue = panels[i].Id;
                    element.FindPropertyRelative("Root").objectReferenceValue = panels[i].Root;
                }
            });
        }

        private static void Edit(Object target, string field, System.Action<SerializedProperty> apply)
        {
            if (target == null)
                throw new System.ArgumentNullException(nameof(target), "Cannot wire '" + field + "' on a null target.");

            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
                throw new System.InvalidOperationException(target.GetType().Name + " has no serialized field '" + field + "'.");
            apply(property);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
