using System;
using System.Collections.Generic;

namespace RideSafe.Tutorial
{
    /// <summary>
    /// Well-known context axes. Constants rather than an enum so a new jurisdiction or
    /// vehicle is content, not a recompile.
    /// </summary>
    public static class TutorialContextKeys
    {
        public const string Language = "language";
        public const string Jurisdiction = "jurisdiction";
        public const string Vehicle = "vehicle";
        public const string Posture = "posture";
        public const string Handedness = "handedness";
        public const string AccessibilityProfile = "accessibility";
        public const string InputProfile = "inputprofile";
    }

    /// <summary>
    /// Session state a sequence can be conditioned on: language, jurisdiction, vehicle,
    /// posture, handedness, accessibility, input profile.
    /// <para>
    /// Deliberately a string map. The core compares key/value pairs and never learns what
    /// "vehicle" MEANS, which is what lets one sequence run under many contexts without
    /// duplicating logic. Typed helpers below are conveniences only.
    /// </para>
    /// </summary>
    public sealed class TutorialContext
    {
        private readonly Dictionary<string, string> _values =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Raised with the key that changed, so presentation can re-resolve text.</summary>
        public event Action<string> ValueChanged;

        public string Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;
            string value;
            return _values.TryGetValue(key.Trim(), out value) ? value : null;
        }

        public bool Has(string key) => !string.IsNullOrEmpty(Get(key));

        public void Set(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            string normalizedKey = key.Trim();
            string normalizedValue = string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();

            string existing;
            if (_values.TryGetValue(normalizedKey, out existing) &&
                string.Equals(existing, normalizedValue, StringComparison.Ordinal))
                return;

            _values[normalizedKey] = normalizedValue;

            Action<string> handler = ValueChanged;
            if (handler != null)
                handler.Invoke(normalizedKey);
        }

        public string Language
        {
            get { return Get(TutorialContextKeys.Language); }
            set { Set(TutorialContextKeys.Language, value); }
        }

        public string Jurisdiction
        {
            get { return Get(TutorialContextKeys.Jurisdiction); }
            set { Set(TutorialContextKeys.Jurisdiction, value); }
        }

        public string Vehicle
        {
            get { return Get(TutorialContextKeys.Vehicle); }
            set { Set(TutorialContextKeys.Vehicle, value); }
        }

        public string Posture
        {
            get { return Get(TutorialContextKeys.Posture); }
            set { Set(TutorialContextKeys.Posture, value); }
        }

        public string Handedness
        {
            get { return Get(TutorialContextKeys.Handedness); }
            set { Set(TutorialContextKeys.Handedness, value); }
        }

        public string AccessibilityProfile
        {
            get { return Get(TutorialContextKeys.AccessibilityProfile); }
            set { Set(TutorialContextKeys.AccessibilityProfile, value); }
        }

        public string InputProfile
        {
            get { return Get(TutorialContextKeys.InputProfile); }
            set { Set(TutorialContextKeys.InputProfile, value); }
        }

        /// <summary>Lookup delegate handed to the sequence core for requirement checks.</summary>
        public Func<string, string> AsLookup() => Get;

        public override string ToString()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder("TutorialContext(");
            bool first = true;
            foreach (KeyValuePair<string, string> pair in _values)
            {
                if (!first)
                    builder.Append(", ");
                builder.Append(pair.Key).Append('=').Append(pair.Value);
                first = false;
            }
            return builder.Append(')').ToString();
        }

        public void Clear()
        {
            _values.Clear();
            ValueChanged = null;
        }
    }
}
