using System;
using UnityEngine;

namespace RideSafe.TaskSequence
{
    /// <summary>
    /// A key/value condition a sequence needs from the running context, for example
    /// key "vehicle" equals "escooter".
    /// <para>
    /// Deliberately stringly-typed: the core must not know what "vehicle" or "language"
    /// MEAN. It only compares pairs, so new axes (jurisdiction, posture, accessibility)
    /// are added as content, never as core code.
    /// </para>
    /// </summary>
    [Serializable]
    public struct ContextRequirement
    {
        [Tooltip("Context key, e.g. language / jurisdiction / vehicle / posture.")]
        [SerializeField] private string _key;

        [Tooltip("Value the context must report for this key.")]
        [SerializeField] private string _value;

        [Tooltip("When true the requirement passes only if the context value DIFFERS.")]
        [SerializeField] private bool _negate;

        public string Key => string.IsNullOrWhiteSpace(_key) ? string.Empty : _key.Trim().ToLowerInvariant();
        public string Value => string.IsNullOrWhiteSpace(_value) ? string.Empty : _value.Trim().ToLowerInvariant();

        public bool IsSatisfiedBy(string actualValue)
        {
            bool matches = string.Equals(
                string.IsNullOrWhiteSpace(actualValue) ? string.Empty : actualValue.Trim().ToLowerInvariant(),
                Value,
                StringComparison.Ordinal);
            return _negate ? !matches : matches;
        }

        public override string ToString() => $"{Key}{(_negate ? " != " : " == ")}{Value}";
    }
}
