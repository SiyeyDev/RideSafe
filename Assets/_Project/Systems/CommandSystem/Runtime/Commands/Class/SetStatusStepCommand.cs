using System;
using UnityEngine;

namespace StepCommand
{
    [Serializable]
    public class SetStatusStepCommand : StepCommandClass
    {
        [Header("SetStatusStepCommand Settings")]
        [SerializeField] private bool _right;
     
        #region ICommand Methods

        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            base.Execute(onComplete);
            CoroutineCaller.Instance.WaitForNextFrame(() => Complete(_right, this));
        }
        #endregion
    }
}