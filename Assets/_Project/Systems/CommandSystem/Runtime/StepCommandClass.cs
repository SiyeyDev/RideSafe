using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace StepCommand
{
    [Serializable]
    public abstract class StepCommandClass : IStepCommand
    {
        [Header("TaskCommand Settings")]
        [SerializeField] private bool _avoidUndo;
        [Tooltip("Enable this to allow the command to reverse when it fails.")]
        [SerializeField] private bool _reverse;
        [Tooltip("Set to 1 to repeat the command once. For values greater than 1, the system will reserve the specified amount minus 1.")]
        [ShowIf("_reverse")][SerializeField] private int _reverseAmount;
        public int ReverseAmount => _reverse ? _reverseAmount : 0;

        protected Action<bool, IStepCommand> onComplete;

        #region ICommand Methods
        public virtual IStepCommand Initialize() => this;
        public virtual void Execute(Action<bool, IStepCommand> onComplete) => this.onComplete = onComplete;
        public virtual void Exit() { }
        public void Undo()
        {
            if (_avoidUndo)
                return;
            InternalUndo();
        }
        public virtual void InternalUndo() { }
        protected virtual void Complete(bool right, IStepCommand stepCommand)
        {
            onComplete?.Invoke(right, stepCommand);
            if (this.ShouldResetMethod())
                onComplete = null;
        }
        #endregion
    }
}