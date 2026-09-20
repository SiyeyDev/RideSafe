# if UNITY_EDITOR
using Sirenix.OdinInspector;
using StepCommand.Debugging;
using UnityEngine;

namespace StepCommand.Editor
{
    public class EditroAttempStepCommandNode : EditorStepCommandNode
    {
        [FoldoutGroup("Steps", Order = 0)]
        [SerializeField, ReadOnly] private StepCommandWrapper _step;

        public EditroAttempStepCommandNode(AttemptsStepCommand targetComponent) : base(targetComponent)
        {
            StepCommandDebugUtilities.TryGetVariable(targetComponent, nameof(_step), out _step);
        }

        #region EditorStepCommandNode Methods
        protected override StepCommandWrapper[] GetCommands() => new StepCommandWrapper[1] { _step };
        protected override bool CanShow() => false;
        #endregion
    }
}
#endif