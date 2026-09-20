using Sirenix.OdinInspector;
using StepCommand;
using System;
using UnityEngine;

[AddComponentMenu("StepCommand/TimerStepCommand")]
public class TimerStepCommand : ParralleBaseMonoBehaviourStepCommand
{
    [Header("BaseAction Settings")]
    [SerializeField] private float _duration;
    [SerializeField] private bool _useTimerGuide;
    [ShowIf("_useTimerGuide")][SerializeField] private TimerProvider _timerProvider;
    private ITimer _timer;
    private bool _timerCompleted;
    private Coroutine _timerCorutine;

    #region ICommand Methods
    public override void Execute(Action<bool, IStepCommand> onComplete)
    {
        _timerCompleted = false;
        base.Execute(onComplete);
        if (!_useTimerGuide)
        {
            _timerCorutine = this.CoroutineExecuteActionAfter(TimerCompleted, _duration);
            return;
        }
        if (_timer == null)
            _timer = _timerProvider.GetTimer();
        _timer.StartTimer(_duration, TimerCompleted);
        return;
    }
    public override void Exit()
    {
        if (!enabled)
            return;
        base.Exit();
        if (_timerCompleted)
            return;
        if (!_useTimerGuide)
        {
            StopCoroutine(_timerCorutine);
            return;
        }
        if (_timer != null)
            _timer.StopTimer();
    }
    protected override void Complete(bool right, IStepCommand stepCommand, bool callDelegate = true)
    {
        if (!ActiveParrallel)
            Exit();
        base.Complete(right, stepCommand, callDelegate);
    }
    #endregion
    #region ParralleBaseMonoBehaviour Methods
    public override bool IsExecuted() => _timerCompleted;
    #endregion
    private void TimerCompleted()
    {
        _timerCompleted = true;
        Complete(true, this);
    }

}
