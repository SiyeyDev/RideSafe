#if UNITY_EDITOR
using StepCommand.Debugging;

namespace StepCommand.Editor
{
    public class EditorSequenceStepCommandNode : EditorStepCommandNode
    {
        public EditorSequenceStepCommandNode(SequenceParallelStepCommand targetComponent) : base(targetComponent)
        {
        }
        #region EditorStepCommandNode Methods

        protected override StepCommandWrapper[] GetCommands()
        {
            StepCommandDebugUtilities.TryGetVariable(TargetComponent, "_stepCommands", out StepCommandWrapper[] steps);
            return steps;
        }
        protected override bool CanShow() => true;
        #endregion
    }
}
#endif