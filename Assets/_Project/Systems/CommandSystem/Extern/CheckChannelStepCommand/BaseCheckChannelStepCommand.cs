using Comparation;
using StepCommand;
using System;
using UnityEngine;

public abstract class BaseCheckChannelStepCommand<T> : ParralleBaseActionStepCommand<BaseCheckChannelStepCommand<T>.CheckChannel>
{
    protected override void Set(bool state) { }
    protected override void AddDelegates(Action right, Action wrong) => action.AddDelegates(right, wrong);
    protected override void RemoveDelegates(Action right, Action wrong) => action.RemoveDelegates(right, wrong);
    [Serializable]
    public class CheckChannel
    {
        [SerializeField] private BaseEventChannelSO<T> _eventChannel;
        [SerializeField] private BaseComparation _comparation;
        [SerializeField] private T _targetValue;

        private bool _lastState;
        private Action _right;
        private Action _wrong;
        private bool _settedDelegates;

        public void AddDelegates(Action right, Action wrong)
        {
            _right = right;
            _wrong = wrong;
            if (_settedDelegates)
                return;
            _settedDelegates = true;
            _eventChannel.OnEventRaised += ReciveEvent;
        }
        public void RemoveDelegates(Action right, Action wrong)
        {
            _right = null;
            _wrong = null;
            if (!_settedDelegates)
                return;
            _settedDelegates = false;
            _eventChannel.OnEventRaised -= ReciveEvent;
        }
        private void ReciveEvent(T newValue)
        {
            bool isValid = _comparation.IsValid(newValue, _targetValue);
            if (isValid != _lastState)
                return;
            _lastState = isValid;
            if (isValid)
                _right.Invoke();
            else
                _wrong.Invoke();
        }

    }
}
