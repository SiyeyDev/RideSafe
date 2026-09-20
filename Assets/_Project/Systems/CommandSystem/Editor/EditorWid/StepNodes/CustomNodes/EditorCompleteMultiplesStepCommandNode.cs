#if UNITY_EDITOR
using Sirenix.OdinInspector;
using StepCommand.Debugging;
using System.Collections.Generic;
using UnityEngine;

namespace StepCommand.Editor
{
    public class EditorCompleteMultiplesStepCommandNode : EditorStepCommandNode
    {
        [FoldoutGroup("Steps", Order = 0)]
        [SerializeField, ReadOnly] private List<StepCommandWrapper> _steps;
        public EditorCompleteMultiplesStepCommandNode(CompleteMultiplesStepCommand targetComponent) : base(targetComponent)
        {
            _steps = new List<StepCommandWrapper>();
        }
        #region EditorStepCommandNode Methods

        protected override StepCommandWrapper[] GetCommands()
        {
            StepCommandDebugUtilities.TryGetVariable(TargetComponent, "_stepCommands", out StepCommandWrapper[] steps);
            foreach (StepCommandWrapper stepCommandWrapper in steps)
                _steps.Add(stepCommandWrapper);
            return steps;
        }
        protected override bool CanShow() => false;
        #endregion
    }
}
#endif