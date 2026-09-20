#if UNITY_EDITOR
using Sirenix.OdinInspector;
using StepCommand.Debugging;
using UnityEngine;

namespace StepCommand.Editor
{
    public class EditorValidateStepCommandNode : EditorStepCommandNode
    {
        [FoldoutGroup("Steps", Order = 0)]
        [SerializeField, ReadOnly] private StepCommandWrapper _stepCommandValidation;
        [FoldoutGroup("Steps", Order = 0)]
        [SerializeField, ReadOnly] private StepCommandWrapper _commandValidation;
        public EditorValidateStepCommandNode(ValidateStepCommand targetComponent) : base(targetComponent)
        {
        }

        #region EditorStepCommandNode Methods
        protected override StepCommandWrapper[] GetCommands()
        {
            if (StepCommandDebugUtilities.TryGetVariable(TargetComponent, "_stepCommandValidation", out StepCommandWrapper stepCommandValidation))
                _stepCommandValidation = stepCommandValidation;
            if (StepCommandDebugUtilities.TryGetVariable(TargetComponent, "_commandToDo", out StepCommandWrapper commandValidation))
                _commandValidation = commandValidation;
            return new StepCommandWrapper[2] { _stepCommandValidation, _commandValidation };
        }
        protected override bool CanShow() => false;
        #endregion
    }
}
#endif