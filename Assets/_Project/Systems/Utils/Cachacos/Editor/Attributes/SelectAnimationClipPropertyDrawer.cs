#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Cachacos
{
    public class SelectAnimationClipPropertyDrawer : OdinBasePropertyDrawer<SelectAnimationClipAttribute, int>
    {
        private Animator _animator;
        protected override void DrawPropertyLayout(GUIContent label)
        {
            SirenixEditorGUI.BeginBox();
            ShowFoldout(out Rect foldoutRect);
            using (new EditorGUI.DisabledScope(true))
                EditorGUI.IntField(foldoutRect, $"{lastParameterName} Hash", this.ValueEntry.SmartValue);
            if (isExpanded)
            {
                _animator = (Animator)SirenixEditorFields.UnityObjectField("Animator", _animator, typeof(Animator), true);
                string clipName = GetClip(_animator == null ? null : _animator.runtimeAnimatorController);
            }
            SirenixEditorGUI.EndBox();
        }
        private string GetClip(RuntimeAnimatorController animatorController)
        {
            if (_animator == null)
            {
                SirenixEditorGUI.ErrorMessageBox("Select an Animator first");
                return lastParameterName;
            }
            AnimationClip[] clips = GetAnimationClips(_animator.runtimeAnimatorController);
            if (clips.Length == 0)
            {
                SirenixEditorGUI.ErrorMessageBox($"Animator '{_animator.name}' has no clips");
                return lastParameterName;
            }
            string[] names = new string[clips.Length];
            for (int i = 0; i < clips.Length; i++)
                names[i] = clips[i].name;
            int currentIndex = GetIndex(names, this.ValueEntry.SmartValue);
            if (ParametersPopup("Clip Name", currentIndex, names, out int newIndex))
                this.ValueEntry.SmartValue = Animator.StringToHash(names[newIndex]);
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
        private AnimationClip[] GetAnimationClips(RuntimeAnimatorController runtimeController)
        {
            List<AnimationClip> clips = new List<AnimationClip>();
            if (runtimeController == null)
                return clips.ToArray();
            if (runtimeController is AnimatorOverrideController overrideController)
            {
                AnimationClip[] baseClips = GetAnimationClips(overrideController.runtimeAnimatorController);
                foreach (var baseClip in baseClips)
                {
                    if (overrideController[baseClip] != null)
                        clips.Add(overrideController[baseClip]);
                    else
                        clips.Add(baseClip);
                }
                return clips.ToArray();
            }
            if (runtimeController is AnimatorController animatorController)
            {
                foreach (AnimatorControllerLayer layer in animatorController.layers)
                {
                    foreach (ChildAnimatorState state in layer.stateMachine.states)
                    {
                        if (state.state.motion is AnimationClip clip)
                            clips.Add(clip);
                    }
                }
            }
            return clips.ToArray();
        }
    }
}
#endif