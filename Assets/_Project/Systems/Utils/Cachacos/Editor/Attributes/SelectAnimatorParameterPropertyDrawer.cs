#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Cachacos
{
    [CustomPropertyDrawer(typeof(SelectAnimatorParameterAttribute))]
    public class SelectAnimatorParameterPropertyDrawer : BasePropertyDrawer
    {
        private Animator _animator;
        private AnimatorControllerParameterType _animatorControllerParameterType = 0;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float propertyHeight = base.GetPropertyHeight(property, label);
            if (property.isExpanded)
            {
                propertyHeight += EditorGUIUtility.singleLineHeight * 3;
                if (_animator != null)
                    propertyHeight += EditorGUIUtility.singleLineHeight;
            }
            return propertyHeight;
        }
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, GUIContent.none, property);
            rect.height = EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label, toggleOnLabelClick: true);
            if (property.isExpanded)
            {
                Rect animatorRect = CreateRect(rect, yOffset: rect.height * 2);
                AnimatorController animator = GetAnimatorController(animatorRect);
                string paramaeterName = GetParameter(property, animatorRect, animator);
                label.text = $"{paramaeterName}Hash";
                rect.y += animatorRect.height;
            }
            using (new EditorGUI.DisabledScope(true))
                EditorGUI.PropertyField(rect, property, label, false);
            EditorGUI.EndProperty();
        }
        private AnimatorController GetAnimatorController(Rect rect)
        {
            _animator = (Animator)EditorGUI.ObjectField(rect, _animator, typeof(Animator), true);
            if(_animator == null)
                return null;
            RuntimeAnimatorController runtimeController = _animator.runtimeAnimatorController;
            if (runtimeController is AnimatorOverrideController overrideController)
                runtimeController = overrideController.runtimeAnimatorController;
            return runtimeController as AnimatorController;
        }
        private string GetParameter(SerializedProperty property, Rect rect, AnimatorController animator)
        {
            Rect parameterTypeRect = CreateRect(rect, yOffset: rect.height);
            if (animator == null)
            {
                EditorGUI.HelpBox(parameterTypeRect, $"Please select an animatorContoller", MessageType.Warning);
                return lastParameterName;
            }
            GUIContent parameterTypeLabel = new GUIContent("Parameter Type");
            _animatorControllerParameterType = (AnimatorControllerParameterType)EditorGUI.EnumPopup(parameterTypeRect, parameterTypeLabel, _animatorControllerParameterType);
            Rect popUpRect = CreateRect(parameterTypeRect, yOffset: parameterTypeRect.height);
            if (_animatorControllerParameterType == 0)
            {
                EditorGUI.HelpBox(popUpRect, $"Please select an parameter type", MessageType.Error);
                return lastParameterName;
            }
            AnimatorControllerParameter[] animatorControllerparameters = animator.parameters;
            List<string> allparameters = GetAllParameters(animatorControllerparameters);
            if (allparameters.Count == 0)
            {
                EditorGUI.HelpBox(popUpRect, $"Controller does not contain parameters of type {_animatorControllerParameterType}", MessageType.Error);
                return lastParameterName;
            }
            string[] parameters = allparameters.ToArray();
            int currentIndex = GetIndex(parameters, property.intValue);
            if (ParametersPopup(popUpRect, "Parameter Name", currentIndex, parameters, out int newIndex))
                property.intValue = Animator.StringToHash(parameters[newIndex]);
            return lastParameterName;
        }

        private List<string> GetAllParameters(AnimatorControllerParameter[] animatorControllerparameters)
        {
            List<string> allparameters = new List<string>();
            foreach (AnimatorControllerParameter animatorControllerparameter in animatorControllerparameters)
            {
                if (animatorControllerparameter.type == _animatorControllerParameterType)
                    allparameters.Add(animatorControllerparameter.name);
            }
            return allparameters;
        }

        private int GetIndex(string[] parameters, int currentSelection)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                if (Animator.StringToHash(parameters[i]) == currentSelection)
                    return i;
            }
            return -1;
        }
    }
}
#endif