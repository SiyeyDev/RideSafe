using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RideSafe.Tutorial
{
    /// <summary>
    /// ActionId -> InputAction mapping for one hardware profile.
    /// <para>
    /// Swapping this asset is how the project supports a different controller without
    /// editing a single sequence: sequences only ever name ActionIds.
    /// </para>
    /// <para>
    /// The <see cref="InputActionReference"/> slots stay empty until the Phase 2 follow-up
    /// creates <c>RideSafeXR.inputactions</c>. An unbound entry reports as unmapped, which
    /// surfaces as a clear error instead of a step that never completes.
    /// </para>
    /// </summary>
    [CreateAssetMenu(fileName = "SO_InputProfile", menuName = "RideSafe/Input Profile", order = 10)]
    public class InputProfileSO : ScriptableObject
    {
        [Serializable]
        public class ActionBinding
        {
            [Tooltip("Abstract action, e.g. primaryselect. Use ActionIds constants.")]
            [SerializeField] private string _actionId;

            [Tooltip("Concrete Input System action. The ONLY place hardware is named.")]
            [SerializeField] private InputActionReference _reference;

            public ActionId ActionId => new ActionId(_actionId);
            public InputActionReference Reference => _reference;
            public InputAction Action => _reference != null ? _reference.action : null;
        }

        [Tooltip("Profile identity, e.g. quest3. Surfaced in logs and the tutorial context.")]
        [SerializeField] private string _profileId = "quest3";

        [SerializeField] private List<ActionBinding> _bindings = new List<ActionBinding>();

        private Dictionary<ActionId, InputAction> _lookup;

        public string ProfileId => string.IsNullOrWhiteSpace(_profileId) ? name : _profileId.Trim().ToLowerInvariant();
        public IReadOnlyList<ActionBinding> Bindings => _bindings;

        public bool TryResolve(ActionId action, out InputAction inputAction)
        {
            inputAction = null;
            if (!action.IsValid)
                return false;

            BuildLookup();
            return _lookup.TryGetValue(action, out inputAction) && inputAction != null;
        }

        /// <summary>Every action this profile can resolve. Used by the unmapped-id error.</summary>
        public string DumpMappedActions()
        {
            BuildLookup();
            if (_lookup.Count == 0)
                return "(profile has no usable bindings)";

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            foreach (KeyValuePair<ActionId, InputAction> pair in _lookup)
                builder.AppendLine("  - " + pair.Key + "  ->  " + pair.Value.name);
            return builder.ToString();
        }

        public void Invalidate() => _lookup = null;

        private void BuildLookup()
        {
            if (_lookup != null)
                return;

            _lookup = new Dictionary<ActionId, InputAction>();
            if (_bindings == null)
                return;

            for (int i = 0; i < _bindings.Count; i++)
            {
                ActionBinding binding = _bindings[i];
                if (binding == null || !binding.ActionId.IsValid)
                    continue;

                if (_lookup.ContainsKey(binding.ActionId))
                {
                    Debug.LogError("[Tutorial] Input profile '" + name + "' maps ActionId '" +
                                   binding.ActionId + "' more than once. Only the first wins.", this);
                    continue;
                }

                InputAction action = binding.Action;
                if (action == null)
                {
                    // Expected while RideSafeXR.inputactions does not exist yet.
                    Debug.LogWarning("[Tutorial] Input profile '" + name + "' has ActionId '" +
                                     binding.ActionId + "' with no InputActionReference assigned.", this);
                    continue;
                }
                _lookup.Add(binding.ActionId, action);
            }
        }
    }
}
