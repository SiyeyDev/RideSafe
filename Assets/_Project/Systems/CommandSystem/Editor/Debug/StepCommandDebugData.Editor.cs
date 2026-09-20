using UnityEditor;
using StepCommand.Editor;

namespace StepCommand.Debugging
{
    [InitializeOnLoad]
    internal static class StepCommandDebugDataEditorInstaller
    {
        static StepCommandDebugDataEditorInstaller()
        {
            StepCommandDebugData.Nodes[typeof(PathStepCommand)] = typeof(EditorPathStepCommandNode);
            StepCommandDebugData.Nodes[typeof(BlockStepCommand)] = typeof(EditorBlockStepCommandNode);
            StepCommandDebugData.Nodes[typeof(ParallelStepCommand)] = typeof(EditorParallelStepCommandNode);
            StepCommandDebugData.Nodes[typeof(SequenceParallelStepCommand)] = typeof(EditorSequenceStepCommandNode);
            StepCommandDebugData.Nodes[typeof(ValidateStepCommand)] = typeof(EditorValidateStepCommandNode);
            StepCommandDebugData.Nodes[typeof(CompleteMultiplesStepCommand)] = typeof(EditorCompleteMultiplesStepCommandNode);
            StepCommandDebugData.Nodes[typeof(TreeStepCommand)] = typeof(EditorTreeStepComandNode);
            StepCommandDebugData.Nodes[typeof(AttemptsStepCommand)] = typeof(EditroAttempStepCommandNode);

            StepCommandDebugData.DebugComponent[typeof(StepCommandHandler)] = typeof(StepCommandHandlerDebug);
            StepCommandDebugData.DebugComponent[typeof(BlockStepCommand)] = typeof(BlockStepCommandDebug);
            StepCommandDebugData.DebugComponent[typeof(ParallelStepCommand)] = typeof(ParallelStepCommandDebug);
            StepCommandDebugData.DebugComponent[typeof(SequenceParallelStepCommand)] = typeof(SequenceParallelStepCommandDebug);
            StepCommandDebugData.DebugComponent[typeof(ValidateStepCommand)] = typeof(ValidateStepCommandDebug);
            StepCommandDebugData.DebugComponent[typeof(CompleteMultiplesStepCommand)] = typeof(CompleteMultiplesStepCommandDebug);
            StepCommandDebugData.DebugComponent[typeof(TreeStepCommand)] = typeof(TreeStepCommandDebug);
            StepCommandDebugData.DebugComponent[typeof(PathStepCommand)] = typeof(PathStepCommandDebug);
            StepCommandDebugData.DebugComponent[typeof(AttemptsStepCommand)] = typeof(AttempStepCommandDebug);
        }
    }
}
