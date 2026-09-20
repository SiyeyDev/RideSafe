using System;

namespace StepCommand.Debugging
{
    public static class EditorPauseBridge
    {
        public static bool IsAvailable => Pause != null;
        public static Action Pause;
    }
}
