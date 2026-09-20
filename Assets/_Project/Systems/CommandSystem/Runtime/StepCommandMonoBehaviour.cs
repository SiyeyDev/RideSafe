using Sirenix.OdinInspector;
using StepCommand.Debugging;
using System;
using UnityEngine;

namespace StepCommand
{
    public abstract class StepCommandMonoBehaviour : SerializedMonoBehaviour, IStepCommand
    {
        [Header("TaskCommand Settings")]
        [SerializeField] private bool _avoidUndo;
        [Tooltip("Enable this to allow the command to reverse when it fails.")]
        [SerializeField] private bool _reverse;
        [Tooltip("Set to 1 to repeat the command once. For values greater than 1, the system will reserve the specified amount minus 1.")]
        [ShowIf("_reverse")][SerializeField] private int _reverseAmount;
        public int ReverseAmount => _reverse ? _reverseAmount : 0;
        protected Action<bool, IStepCommand> onComplete;
        #region StepCommand Methods
        protected virtual void Start() { }
        #endregion
        #region ICommand Methods
        public virtual IStepCommand Initialize()
        {
            enabled = false;
            return this;
        }
        public virtual void Execute(Action<bool, IStepCommand> onComplete)
        {
            this.onComplete = onComplete;
            enabled = true;
        }
        public virtual void Exit() => enabled = false;
        public void Undo()
        {
            if (_avoidUndo)
                return;
            InternalUndo();
        }
        public virtual void InternalUndo() { }
        protected virtual void Complete(bool right, IStepCommand stepCommand, bool callDelegate = true)
        {
            if (callDelegate)
            {
                onComplete?.Invoke(right, stepCommand);
                if (this.ShouldResetMethod() && right)
                    onComplete = null;
            }
            enabled = false;
        }
        #endregion
    }
}
