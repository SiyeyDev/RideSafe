#if UNITY_EDITOR
using StepCommand.Debugging;

namespace StepCommand.Editor
{
    public class EditorBlockStepCommandNode : EditorStepCommandNode
    {
        public EditorBlockStepCommandNode(BlockStepCommand targetComponent) : base(targetComponent)
        {
        }
        #region EditorStepCommandNode Methods
        protected override StepCommandWrapper[] GetCommands()
        {
            StepCommandDebugUtilities.TryGetVariable(TargetComponent, "_steps", out StepCommandWrapper[] steps);
            return steps;
        }
        protected override bool CanShow() => true;
        #endregion
    }
}
#endif