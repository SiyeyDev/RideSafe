using Autohand;
using StepCommand;
using System;
using UnityEngine;

[Serializable]
public class OnStickStepCommand : ParralleBaseActionStepCommand<OnStickStepCommand.StickStep>
{
    #region BaseAction Reusable Methods
    protected override void Set(bool state) => action.SetTouchVisibility(state);
    protected override void AddDelegates(Action right, Action wrong) => action.AddDelegates(right, wrong);
    protected override void RemoveDelegates(Action right, Action wrong) => action.RemoveDelegates(right, wrong);

    #endregion
    [Serializable]
    public class StickStep
    {
        [SerializeField] private Sticky _sticky;
        private bool _settedDelegates;
        private Action _right;

        public void SetTouchVisibility(bool visibility)
        {
            _sticky.gameObject.SetActive(visibility);
        }
        public void AddDelegates(Action right, Action wrong)
        {
            _right = right;
            if (_settedDelegates)
                return;
            _settedDelegates = true;
            _sticky.OnStick.AddListener(OnStick);
        }
        public void RemoveDelegates(Action right, Action wrong)
        {
            _right = null;
            if (!_settedDelegates)
                return;
            _settedDelegates = false;
            _sticky.OnStick.RemoveListener(OnStick);
        }
        private void OnStick() => _right?.Invoke();
    }
}
