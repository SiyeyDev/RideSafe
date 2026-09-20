using StepCommand.Debugging;
using System;
using UnityEngine;

namespace StepCommand
{
    [Serializable]
    public class DebugPauseStepCommand : StepCommandClass
    {
        [SerializeField] private int delay;
        #region ICommand Methods

        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            if (EditorPauseBridge.IsAvailable)
            {
                EditorPauseBridge.Pause();
                CoroutineCaller.Instance.CoroutineExecuteActionAfter(() => Complete(true, this), delay);
            }
            else
            {
                StepCommandDebugUtilities.DebugLog($"This step of type {typeof(DebugPauseStepCommand)} should only be used in the editor.");
                CoroutineCaller.Instance.WaitForNextFrame(() => Complete(true, this));
            }
        }
        #endregion
    }
}