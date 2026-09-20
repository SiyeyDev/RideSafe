using Autohand;
using Sirenix.OdinInspector;
using StepCommand;
using System;
using UnityEngine;

[Serializable]
public class TouchStepCommand : ParralleBaseActionStepCommand<TouchStepCommand.HandInteractionStep>
{
    #region BaseAction Reusable Methods
    protected override void Set(bool state) => action.SetTouchVisibility(state);
    protected override void AddDelegates(Action right, Action wrong) => action.AddDelegates(right, wrong);
    protected override void RemoveDelegates(Action right, Action wrong) => action.RemoveDelegates(right, wrong);

    #endregion
    [Serializable]
    public class HandInteractionStep
    {
        [SerializeField] private bool _useTrigger;
        [HideIf("_useTrigger")][SerializeField] private HandTouchEvent _handTouchEvent;
        [ShowIf("_useTrigger")][SerializeField] private bool _onlyFingers;
        [ShowIf("_useTrigger")][SerializeField] private FingerTriggerAreaEvents _triggerEvent;
        private bool _settedDelegates;
        private Action _right;
        private Action _wrong;
        public void SetTouchVisibility(bool visibility)
        {
            if (!_useTrigger)
            {
                _handTouchEvent.gameObject.SetActive(visibility);
                return;
            }
            _triggerEvent.gameObject.SetActive(visibility);
        }
        public void AddDelegates(Action right, Action wrong)
        {
            _right = right;
            _wrong = wrong;
            if (_settedDelegates)
                return;
            _settedDelegates = true;
            if (!_useTrigger)
            {
                _handTouchEvent.HandStartTouchEvent += HandStartTouchStartAnim;
                _handTouchEvent.HandStartTouchEvent += HandStopTouchStartAnim;
                return;
            }
            _triggerEvent.FingerEnterEvent.AddListener(HandStartFingerStartAnim);
            _triggerEvent.FingerExitEvent.AddListener(HandStopFingerStartAnim);
            if (_onlyFingers)
                return;
            _triggerEvent.HandEnterEvent += HandStartTriggerStartAnim;
            _triggerEvent.HandExitEvent += HandStopTriggerStartAnim;
        }
        public void RemoveDelegates(Action right, Action wrong)
        {
            _right = null;
            _wrong = null;
            if (!_settedDelegates)
                return;
            _settedDelegates = false;
            if (!_useTrigger)
            {
                _handTouchEvent.HandStartTouchEvent -= HandStartTouchStartAnim;
                _handTouchEvent.HandStartTouchEvent -= HandStopTouchStartAnim;
                return;
            }
            _triggerEvent.FingerEnterEvent.RemoveListener(HandStartFingerStartAnim);
            _triggerEvent.FingerExitEvent.RemoveListener(HandStopFingerStartAnim);
            if (_onlyFingers)
                return;
            _triggerEvent.HandEnterEvent -= HandStartTriggerStartAnim;
            _triggerEvent.HandExitEvent -= HandStopTriggerStartAnim;
        }
        private void HandStartTouchStartAnim(Hand hand) => _right?.Invoke();
        private void HandStartTriggerStartAnim(Hand hand, HandTriggerAreaEvents handTriggerAreaEvent) => _right?.Invoke();
        private void HandStartFingerStartAnim(Finger finger, FingerTriggerAreaEvents fingerTriggerAreaEvents) => _right?.Invoke();
        private void HandStopTouchStartAnim(Hand hand) => _wrong?.Invoke();
        private void HandStopTriggerStartAnim(Hand hand, HandTriggerAreaEvents handTriggerAreaEvent) => _wrong?.Invoke();
        private void HandStopFingerStartAnim(Finger finger, FingerTriggerAreaEvents fingerTriggerAreaEvents) => _wrong?.Invoke();
    }
}
