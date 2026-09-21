using System;
using UnityEngine;

namespace RideSafe.Tutorial
{
    /// <summary>
    /// Abstract name for something the learner DOES, independent of hardware.
    /// <para>
    /// A sequence asks "was PrimarySelect performed?", never "was the right index trigger
    /// pressed?". The translation lives in <see cref="InputProfileSO"/>, so swapping the
    /// controller profile never touches a sequence asset (CASE 14).
    /// </para>
    /// </summary>
    [Serializable]
    public struct ActionId : IEquatable<ActionId>
    {
        [SerializeField] private string _value;

        public ActionId(string value) => _value = Normalize(value);

        public string Value => string.IsNullOrEmpty(_value) ? string.Empty : _value;
        public bool IsValid => !string.IsNullOrWhiteSpace(_value);

        public static readonly ActionId None = default;

        private static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();

        public bool Equals(ActionId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ActionId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => IsValid ? Value : "<none>";

        public static bool operator ==(ActionId a, ActionId b) => a.Equals(b);
        public static bool operator !=(ActionId a, ActionId b) => !a.Equals(b);
        public static implicit operator ActionId(string value) => new ActionId(value);
    }

    /// <summary>
    /// Canonical action names. Constants, not an enum, so Modules 2-5 can add
    /// vehicle actions as content without recompiling the core.
    /// </summary>
    public static class ActionIds
    {
        // Sprint 1 - handheld interaction grammar.
        public const string PrimarySelect = "primaryselect";
        public const string SecondarySelect = "secondaryselect";
        public const string Back = "back";
        public const string Confirm = "confirm";
        public const string Pause = "pause";
        public const string Recenter = "recenter";

        // Reserved for later sprints. Declared here only so naming stays consistent;
        // nothing in Sprint 1 binds or validates them.
        public const string Throttle = "throttle";
        public const string FrontBrake = "frontbrake";
        public const string RearBrake = "rearbrake";
        public const string BothBrakes = "bothbrakes";
        public const string Bell = "bell";
        public const string ContextAction = "contextaction";
        public const string Steering = "steering";
        public const string LookLeft = "lookleft";
        public const string LookRight = "lookright";
        public const string LookBehind = "lookbehind";
    }
}
