#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Cachacos
{
    [CustomPropertyDrawer(typeof(SelectAnimationStateAttribute))]
    public class SelectAnimationStatePropertyDrawer : BasePropertyDrawer
    {
        private Animator _animator;
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
            if (property.isExpanded)
            {
                Rect animatorRect = CreateRect(rect, yOffset: rect.height * 2);
                _animator = (Animator)EditorGUI.ObjectField(animatorRect, _animator, typeof(Animator), true);
                string clipName = GetClip(property, animatorRect, _animator == null ? null : _animator.runtimeAnimatorController);
                rect.y += animatorRect.height;
                label.text = $"{clipName}Hash";
            }
            using (new EditorGUI.DisabledScope(true))
                EditorGUI.PropertyField(rect, property, label, false);
            EditorGUI.EndProperty();
        }
        private string GetClip(SerializedProperty property, Rect rect, RuntimeAnimatorController animatorController)
        {
            Rect popUpRect = CreateRect(rect, yOffset: rect.height);
            if (_animator == null)
            {
                EditorGUI.HelpBox(popUpRect, $"Please select an Animator", MessageType.Warning);
                return lastParameterName;
            }
            AnimatorState[] animatorControllerparameters = GetAnimationClips(animatorController);
            if (animatorControllerparameters.Length == 0)
            {
                EditorGUI.HelpBox(popUpRect, $"Animator Controller {_animator.name} does not have any Clip", MessageType.Warning);
                return lastParameterName;
            }
            string[] parameters = new string[animatorControllerparameters.Length];
            for (int i = 0; i < animatorControllerparameters.Length; i++)
                parameters[i] = animatorControllerparameters[i].name;
            int currentIndex = GetIndex(parameters, property.intValue);
            if (ParametersPopup(popUpRect, "Parameter Name", currentIndex, parameters, out int newIndex))
                property.intValue = Animator.StringToHash(parameters[newIndex]);
            return lastParameterName;
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
        private AnimatorState[] GetAnimationClips(RuntimeAnimatorController runtimeController)
        {
            List<AnimatorState> states = new List<AnimatorState>();
            if (runtimeController is AnimatorOverrideController overrideController)
                runtimeController = overrideController.runtimeAnimatorController;
            AnimatorController animatorController = runtimeController as AnimatorController;
            if (animatorController == null)
            {
                Debug.LogWarning("The controller is not an AnimatorController.");
                return states.ToArray();
            }
            foreach (AnimatorControllerLayer layer in animatorController.layers)
            {
                foreach (ChildAnimatorState state in layer.stateMachine.states)
                {
                    if (state.state.motion is AnimationClip clip)
                        states.Add(state.state);
                }
            }
            return states.ToArray();
        }
    }
}
#endif
