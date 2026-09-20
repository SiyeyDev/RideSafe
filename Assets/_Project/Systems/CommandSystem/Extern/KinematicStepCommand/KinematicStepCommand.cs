using StepCommand;
using System;
using UnityEngine;

[Serializable]
public class KinematicStepCommand : StepCommandClass
{
    [Header("KinematicStepCommand settings")]
    [SerializeField] private float _delay;
    [SerializeField] private BaseKinematic _kinematic;
    private Coroutine _coroutine;
    #region ICommand Methods
    public override void Execute(Action<bool, IStepCommand> onComplete)
    {
        base.Execute(onComplete);   
        if (_delay == 0)
        {
            Play();
            return;
        }
        _kinematic.CoroutineExecuteActionAfter(Play, _delay);
    }
    public override void Exit()
    {
        if (_coroutine != null)
            _kinematic.StopCoroutine(_coroutine);
        _kinematic.Stop();
    }
    #endregion
    private void Play()
    {
        _kinematic.Play();
        _kinematic.OnStop += StopKinematic;
    }
    private void StopKinematic()
    {
        Complete(true, this);
        _kinematic.OnStop -= StopKinematic;
    }
}
