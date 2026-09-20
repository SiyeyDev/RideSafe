using Autohand;
using StepCommand;
using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class GrabStepCommand : ParralleBaseActionStepCommand<GrabStepCommand.Grab>
{
    #region BaseAction Reusable Methods
    protected override void Set(bool state) => action.SetVisibility(state);
    protected override void AddDelegates(Action right, Action wrong) => action.AddDelegates(right, wrong);
    protected override void RemoveDelegates(Action right, Action wrong) => action.RemoveDelegates(right, wrong);
    #endregion
    [Serializable]
    public class Grab
    {
        [SerializeField] private Grabbable _grabbable;
        [SerializeField] private bool _hideObject;
        [SerializeField] private bool _bothHands;
        [SerializeField] private UnityEvent _onGrab;
        [SerializeField] private UnityEvent _onRelease;
        private bool _settedDelegates;
        private int _amountOfHands;
        private int AmountOfHands
        {
            get => _amountOfHands;
            set => _amountOfHands = Mathf.Clamp(value, 0, 2);
        }
        private Action _right;
        private Action _wrong;
        private Hand _lastHand;
        public void SetVisibility(bool state)
        {
            _amountOfHands = 0;
            _grabbable.ForceHandsRelease();
            _grabbable.enabled = state;
            if (!_hideObject)
                return;
            _grabbable.gameObject.SetActive(state);
        }

        public void AddDelegates(Action right, Action wrong)
        {
            _right = right;
            _wrong = wrong;
            if (_settedDelegates)
                return;
            _settedDelegates = true;
            _grabbable.OnGrabEvent += OnGrab;
            _grabbable.OnReleaseEvent += OnRelease;
        }
        public void RemoveDelegates(Action right, Action wrong)
        {
            _right = null;
            _wrong = null;
            if (!_settedDelegates)
                return;
            _settedDelegates = false;
            _grabbable.OnGrabEvent -= OnGrab;
            _grabbable.OnReleaseEvent -= OnRelease;
        }
        private void OnGrab(Hand hand, Grabbable grabbable)
        {
            AmountOfHands++;
            _lastHand = hand;
            if (!_bothHands)
            {
                Debug.Log($"Grab {_grabbable.gameObject.name} with {AmountOfHands}");
                _onGrab?.Invoke();
                _right?.Invoke();
                return;
            }
            if (_amountOfHands < 2)
                return;
            _amountOfHands = 2;
            Debug.Log($"Grab {_grabbable.gameObject.name} with {AmountOfHands}");
            _onGrab?.Invoke();
            _right?.Invoke();
        }
        private void OnRelease(Hand hand, Grabbable grabbable)
        {
            AmountOfHands--;
            if (_lastHand == hand)
                _lastHand = null;
            if (!_bothHands)
            {
                Debug.Log($"Release {_grabbable.gameObject.name} with {AmountOfHands}");
                _onRelease?.Invoke();
                _wrong?.Invoke();
                return;
            }
            if (_amountOfHands > 0)
                return;
            Debug.Log($"Release {_grabbable.gameObject.name} with {AmountOfHands}");
            _amountOfHands = 0;
            _wrong?.Invoke();
            _onRelease?.Invoke();
        }
    }
}
