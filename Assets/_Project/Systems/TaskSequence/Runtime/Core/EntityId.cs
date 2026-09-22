using System;
using UnityEngine;

namespace RideSafe.TaskSequence
{
    /// <summary>
    /// Stable, scene-agnostic identity for a tutorializable object (for example
    /// "tutorial.target.circle" or "ui.language.english").
    /// <para>
    /// Sequences reference entities exclusively through this id, which is what keeps
    /// <see cref="TaskSequenceSO"/> assets free of scene references.
    /// </para>
    /// </summary>
    [Serializable]
    public struct EntityId : IEquatable<EntityId>
    {
        [SerializeField] private string _value;

        public EntityId(string value) => _value = Normalize(value);

        /// <summary>Normalized id, or <see cref="string.Empty"/> when unset.</summary>
        public string Value => string.IsNullOrEmpty(_value) ? string.Empty : _value;

        /// <summary>False when the id is unset or blank; callers should skip resolution.</summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(_value);

        public static readonly EntityId None = default;

        /// <summary>
        /// Ids are compared case-insensitively, so authoring casing never causes a
        /// silent resolution miss. Whitespace is trimmed.
        /// </summary>
        private static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();

        public bool Equals(EntityId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is EntityId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => IsValid ? Value : "<none>";

        public static bool operator ==(EntityId a, EntityId b) => a.Equals(b);
        public static bool operator !=(EntityId a, EntityId b) => !a.Equals(b);
        public static implicit operator EntityId(string value) => new EntityId(value);
    }
}
