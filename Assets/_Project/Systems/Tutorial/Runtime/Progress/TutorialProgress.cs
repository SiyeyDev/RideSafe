using System;
using System.Collections.Generic;

namespace RideSafe.Tutorial
{
    /// <summary>
    /// Which tutorial sequences the learner has finished.
    /// <para>
    /// These three types are deliberately separate and NOT interchangeable. Finishing a
    /// tutorial says nothing about completing a module, and completing a module says
    /// nothing about passing it. Collapsing them into one "Completed" flag is how a
    /// learner ends up marked as competent for watching an explanation, so the type
    /// system is used to make that mistake impossible to express.
    /// </para>
    /// </summary>
    public sealed class TutorialProgress
    {
        private readonly HashSet<string> _completed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _skipped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public event Action<string> SequenceCompleted;

        public bool IsTutorialCompleted(string sequenceId) =>
            !string.IsNullOrWhiteSpace(sequenceId) && _completed.Contains(sequenceId.Trim());

        public bool WasSkipped(string sequenceId) =>
            !string.IsNullOrWhiteSpace(sequenceId) && _skipped.Contains(sequenceId.Trim());

        public void MarkCompleted(string sequenceId)
        {
            if (string.IsNullOrWhiteSpace(sequenceId))
                return;
            if (!_completed.Add(sequenceId.Trim()))
                return;

            Action<string> handler = SequenceCompleted;
            if (handler != null)
                handler.Invoke(sequenceId.Trim());
        }

        public void MarkSkipped(string sequenceId)
        {
            if (!string.IsNullOrWhiteSpace(sequenceId))
                _skipped.Add(sequenceId.Trim());
        }

        public IEnumerable<string> CompletedSequences => _completed;

        public void Clear()
        {
            _completed.Clear();
            _skipped.Clear();
            SequenceCompleted = null;
        }
    }

    /// <summary>
    /// How far a learner got through a module's content. Distinct from
    /// <see cref="TutorialProgress"/>: reaching the end of Module 1 is not the same as
    /// having been taught how to interact with it.
    /// </summary>
    public sealed class ModuleProgress
    {
        private readonly HashSet<string> _reached = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _completed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public bool IsModuleReached(string moduleId) =>
            !string.IsNullOrWhiteSpace(moduleId) && _reached.Contains(moduleId.Trim());

        public bool IsModuleCompleted(string moduleId) =>
            !string.IsNullOrWhiteSpace(moduleId) && _completed.Contains(moduleId.Trim());

        public void MarkReached(string moduleId)
        {
            if (!string.IsNullOrWhiteSpace(moduleId))
                _reached.Add(moduleId.Trim());
        }

        public void MarkCompleted(string moduleId)
        {
            if (!string.IsNullOrWhiteSpace(moduleId))
                _completed.Add(moduleId.Trim());
        }

        public void Clear()
        {
            _reached.Clear();
            _completed.Clear();
        }
    }

    /// <summary>
    /// Outcome of a scored attempt. Separate from both progress types, and intentionally
    /// left thin in Sprint 1: thresholds, scoring and transfer are out of scope, so this
    /// only records that an attempt happened and how it ended.
    /// </summary>
    public sealed class AssessmentResult
    {
        public string ModuleId { get; }
        public string AttemptId { get; }
        public DateTime CompletedAtUtc { get; }

        /// <summary>
        /// Null until a scoring rule exists. Deliberately nullable so "not yet evaluated"
        /// cannot be mistaken for "failed".
        /// </summary>
        public bool? Passed { get; }

        public AssessmentResult(string moduleId, string attemptId, bool? passed)
        {
            ModuleId = moduleId;
            AttemptId = attemptId;
            Passed = passed;
            CompletedAtUtc = DateTime.UtcNow;
        }

        public override string ToString() =>
            "AssessmentResult(" + ModuleId + ", attempt=" + AttemptId + ", passed=" +
            (Passed.HasValue ? Passed.Value.ToString() : "not-evaluated") + ")";
    }
}
