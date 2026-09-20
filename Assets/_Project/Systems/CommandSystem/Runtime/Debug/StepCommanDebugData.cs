using System;
using System.Collections.Generic;

namespace StepCommand.Debugging
{
    public static class StepCommandDebugData
    {
        public static readonly string debugHeader = "::COMAND DEBUGGER:: ";
        public static readonly string getGuideName = "GetGuideName";
        public static readonly string debugMainColor = "#00FFFF";
        public static readonly string commandLableMainColor = "#FFFF00";

        public static string GetHeader() => $"<color={debugMainColor}><b>{debugHeader}</b></color>";

        public static readonly Dictionary<Type, Type> Nodes = new Dictionary<Type, Type>();
        public static readonly Dictionary<Type, Type> DebugComponent = new Dictionary<Type, Type>();
    }
    [Flags]
    public enum DebugType
    {
        None = 0,
        Everything = ~0,
        Log = 1 << 0,
        Warning = 1 << 1,
        Error = 1 << 2,
    }
}
