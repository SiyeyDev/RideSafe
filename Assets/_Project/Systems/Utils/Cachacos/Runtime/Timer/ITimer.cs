
using System;

public interface ITimer
{
    public bool InCanvas { get; }
    public void StartTimer(float duration, Action afterTimer = null);
    public void StopTimer();

}
