using Autohand;
using System;
using UnityEngine;

public static class AnimationsExtension
{
    public static float GetClipDuration(this Animator animator, int clipHash)
    {
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller is AnimatorOverrideController overrideController)
            controller = overrideController.runtimeAnimatorController;
        foreach (AnimationClip clip in controller.animationClips)
        {
            if (Animator.StringToHash(clip.name) != clipHash)
                continue;
            return clip.length;
        }
        throw new Exception($"There is not clip with Hash: {clipHash} in the animator {animator.name}");
    }
    public static void ExcuteAfterAnim(this Animator animator, MonoBehaviour objectCalling, Action action, int clipHash)
    {
        animator.ExcuteAfterAnim(objectCalling, action, clipHash, Constants.k_crossfadeDuration);
    }
    public static void ExcuteAfterAnim(this Animator animator, MonoBehaviour objectCalling, Action action, int clipHash, float crossFadeDuration, bool fixedSpeed = false)
    {
        animator.CrossFade(clipHash, crossFadeDuration);
        if (fixedSpeed)
            objectCalling.WaitForNextFrame(() => objectCalling.CoroutineUpdateUntil(() => animator.IsPlayingState(clipHash), action));
        else
            objectCalling.CoroutineExecuteActionAfter(action, animator.GetClipDuration(clipHash));
    }
    public static bool IsPlayingState(this Animator animator, int stateHash)
    {
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (animator.IsInTransition(0))
            return true;
        if (state.shortNameHash != stateHash)
            return false;
        if (state.loop)
            return true;
        return state.normalizedTime < 1f;
    }
}
