using UnityEngine;

public static class FadeLogger 
{
    private static readonly string r_fadeLoggerHeader = $"<color=\"#D900FF\"><b>::FADE UI:: </b></color>";
    public static void DebugLog(string message) => Debug.Log($"{r_fadeLoggerHeader}{message}");
    public static void DebugWarning(string message) => Debug.LogWarning($"{r_fadeLoggerHeader}{message}");
    public static void DebugError(string message) => Debug.LogError($"{r_fadeLoggerHeader}{message}");
}
