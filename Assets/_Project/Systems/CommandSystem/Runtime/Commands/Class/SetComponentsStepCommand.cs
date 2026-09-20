using System;
using System.Collections.Generic;
using UnityEngine;

namespace StepCommand
{
    [Serializable]
    public class SetComponentsStepCommand : StepCommandClass
    {
        [Header("Set Component Settings")]
        [SerializeField] List<ComponentData> _components;

        #region ICommand Methods
        public override IStepCommand Initialize()
        {
            foreach (ComponentData component in _components)
                component.Validate();
            return base.Initialize();
        }
        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            base.Execute(onComplete);
            foreach (ComponentData component in _components)
                component.Set();
            CoroutineCaller.Instance.WaitForNextFrame(() => Complete(true, this));
        }

        public override void InternalUndo()
        {
            foreach (ComponentData component in _components)
                component.Revert();
            base.InternalUndo();
        }

        #endregion

        [Serializable]
        private class ComponentData
        {
            [SerializeField] private MonoBehaviour _target;
            [SerializeField] private bool _state;
            private bool _lastState;

            public void Set()
            {
                _lastState = _target.enabled;
                _target.enabled = _state;
            }
            public void Revert() => _target.enabled = _lastState;
            public void Validate()
            {
                if (_target == null)
                    throw new Exception($"There is not target attached to list");
            }
        }

    }
}