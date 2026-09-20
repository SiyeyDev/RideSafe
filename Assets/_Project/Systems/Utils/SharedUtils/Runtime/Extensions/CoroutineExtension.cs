using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Provides extension methods for MonoBehaviour to simplify coroutine execution.
/// </summary>
public static class CoroutineExtension
{
    private static WaitForEndOfFrame _frameWait = new WaitForEndOfFrame();
    /// <summary>
    /// Starts a coroutine to execute an action after a specified duration.
    /// </summary>
    /// <param name="objectCalling">The MonoBehaviour instance calling this method.</param>
    /// <param name="action">The action to execute after the specified duration.</param>
    /// <param name="duration">The duration to wait before executing the action.</param>
    /// <param name="unscaledTime">Determines whether to use unscaled time for the duration. Default is false.</param>
    /// <returns>The Coroutine representing the coroutine started.</returns>
    public static Coroutine CoroutineExecuteActionAfter(this MonoBehaviour objectCalling, Action action, float duration, bool unescaledTime = false)
                                                        => objectCalling.StartCoroutine(ExecuteActionAfter(action, duration, unescaledTime));
    /// <summary>
    /// Starts a coroutine to execute an action until a specified exit condition is met.
    /// </summary>
    /// <param name="objectCalling">The MonoBehaviour instance calling this method.</param>
    /// <param name="exitCondition">The exit condition that determines when to stop executing the action.</param>
    /// <param name="actionAfterTime">An optional action to execute after the exit condition is met.</param>
    /// <returns>The Coroutine  representing the coroutine started.</returns>
    public static Coroutine CoroutineUpdateUntil(this MonoBehaviour objectCalling, Func<bool> exitCondition, Action actionAfterTime = null)
                                                 => objectCalling.StartCoroutine(UpdateUntil(exitCondition, actionAfterTime));
    public static Coroutine WaitForNextFrame(this MonoBehaviour objectCalling, Action action) => objectCalling.StartCoroutine(WaitNextFrame(action));

    private static IEnumerator ExecuteActionAfter(Action action, float duration, bool unescaledTime)
    {
        if (!unescaledTime)
            yield return new WaitForSeconds(duration);
        else
        {
            float startTime = Time.realtimeSinceStartup;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime = Time.realtimeSinceStartup - startTime;
                yield return null;
            }
        }
        action?.Invoke();
    }

    private static IEnumerator UpdateUntil(Func<bool> exitCondition, Action actionAfterTime)
    {
        while (exitCondition.Invoke())
            yield return null;
        actionAfterTime?.Invoke();
    }

    private static IEnumerator WaitNextFrame(Action action)
    {
        yield return _frameWait;
        action?.Invoke();
    }
    public static IEnumerator WaitForEndOfFrameFrames(int frameCount)
    {
        var wait = new WaitForEndOfFrame();
        for (int i = 0; i < frameCount; i++)
            yield return wait;
    }
}