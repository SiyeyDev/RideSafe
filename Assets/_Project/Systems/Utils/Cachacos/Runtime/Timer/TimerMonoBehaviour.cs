using System;
using UnityEngine;

public abstract class TimerMonoBehaviour : MonoBehaviour, ITimer
{
    #region Itimer Methods
    public virtual bool InCanvas => true;

    public abstract void StartTimer(float duration, Action afterTimer = null);

    public abstract void StopTimer();

    #endregion
}
