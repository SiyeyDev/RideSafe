using UnityEditor;

namespace StepCommand.Debugging
{
    [InitializeOnLoad]
    internal static class EditorPauseBridgeInstaller
    {
        static EditorPauseBridgeInstaller()
        {
            EditorPauseBridge.Pause = () => EditorApplication.isPaused = true;
        }
    }
}
