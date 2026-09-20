using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace StepCommand
{
    [Serializable]
    public class UnityEventStepCommand : StepCommandClass
    {
        [Header("AddScoreStepCommand Settings")]
        [SerializeField] private UnityEvent _event;

        #region ICommand Methods
        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            _event?.Invoke();
            base.Execute(onComplete);
            CoroutineCaller.Instance.WaitForNextFrame(() => Complete(true, this));
        }

        [Button("Event_Invoke")]
        public void Event_Invoke()
        {
            _event?.Invoke();
        }
        #endregion
    }
}
