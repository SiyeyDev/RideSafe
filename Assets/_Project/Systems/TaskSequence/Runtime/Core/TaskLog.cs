using UnityEngine;

namespace RideSafe.TaskSequence
{
    /// <summary>
    /// Single funnel for sequence diagnostics. Every message carries the SequenceId and
    /// StepId so a log line is actionable without opening the asset.
    /// </summary>
    public static class TaskLog
    {
        private const string k_prefix = "<b>[TaskSequence]</b>";

        public static bool Verbose = true;

        private static string Tag(string sequenceId, string stepId)
        {
            if (string.IsNullOrEmpty(stepId))
                return $"{k_prefix} <b>{Safe(sequenceId)}</b>";
            return $"{k_prefix} <b>{Safe(sequenceId)}</b> / <b>{Safe(stepId)}</b>";
        }

        private static string Safe(string value) => string.IsNullOrEmpty(value) ? "<unnamed>" : value;

        public static void Info(string sequenceId, string stepId, string message)
        {
            if (!Verbose)
                return;
            Debug.Log($"{Tag(sequenceId, stepId)} {message}");
        }

        public static void Warn(string sequenceId, string stepId, string message, Object context = null)
            => Debug.LogWarning($"{Tag(sequenceId, stepId)} {message}", context);

        public static void Error(string sequenceId, string stepId, string message, Object context = null)
            => Debug.LogError($"{Tag(sequenceId, stepId)} {message}", context);
    }
}
