#if UNITY_EDITOR
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using StepCommand.Debugging;

namespace StepCommand.Editor
{
    public class EditorTreeStepComandNode : EditorStepCommandNode
    {
        [FoldoutGroup("Tree")]
        [HideLabel]
        [OdinSerialize, ReadOnly] public StepCommandWrapper _stepCommandValidation;
        [FoldoutGroup("Tree"), HorizontalGroup("Tree/Tree")]
        [BoxGroup("Tree/Tree/Right")]
        [OdinSerialize, ReadOnly] public StepCommandWrapper _commandRight;
        [FoldoutGroup("Tree"), HorizontalGroup("Tree/Tree")]
        [BoxGroup("Tree/Tree/Wrong")]
        [OdinSerialize, ReadOnly] public StepCommandWrapper _commandWrong;
        public EditorTreeStepComandNode(TreeStepCommand targetComponent) : base(targetComponent)
        {
            StepCommandDebugUtilities.TryGetVariable(targetComponent, nameof(_stepCommandValidation), out _stepCommandValidation);
            StepCommandDebugUtilities.TryGetVariable(targetComponent, nameof(_commandRight), out _commandRight);
            StepCommandDebugUtilities.TryGetVariable(targetComponent, nameof(_commandWrong), out _commandWrong);

        }
        #region EditorStepCommandNode Methods

        protected override StepCommandWrapper[] GetCommands()
        {
            return new StepCommandWrapper[3] { _stepCommandValidation, _commandRight, _commandWrong };
        }
        protected override bool CanShow() => false;

        #endregion
    }
}
#endif