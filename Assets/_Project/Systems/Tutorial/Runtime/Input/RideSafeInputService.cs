using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RideSafe.Tutorial
{
    /// <summary>
    /// Input System backed <see cref="ITutorialInputService"/>: reads an
    /// <see cref="InputProfileSO"/> and exposes per-ActionId state.
    /// <para>
    /// NOT the project default. RideSafe drives the player with AutoHand's XRPlayer, which
    /// reads controllers through <c>InputDevices</c>; use <see cref="XRDeviceInputService"/>
    /// so the tutorial reads exactly what the player reads. This backend remains for
    /// Input System based tooling (desktop simulation, tests).
    /// </para>
    /// <para>
    /// Actions can be shared with other systems. On teardown this service only disables
    /// the actions IT enabled; anything that was already enabled when it arrived is left
    /// running, so it can never switch off another system's input.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-150)]
    [AddComponentMenu("RideSafe/Tutorial Input Service")]
    public class RideSafeInputService : MonoBehaviour, ITutorialInputService
    {
        [Tooltip("Active hardware profile. Swapping this must never require editing a sequence.")]
        [SerializeField] private InputProfileSO _profile;

        private readonly Dictionary<ActionId, InputAction> _resolved = new Dictionary<ActionId, InputAction>();
        private readonly HashSet<ActionId> _performedThisFrame = new HashSet<ActionId>();
        private readonly HashSet<string> _unmappedReported = new HashSet<string>();
        private readonly List<InputAction> _enabled = new List<InputAction>();

        // Subset of _enabled that was disabled when we took it, i.e. that WE switched on.
        // Only these are switched off again on teardown.
        private readonly HashSet<InputAction> _enabledByThisService = new HashSet<InputAction>();

        private bool _registered;

        public event Action<ActionId> ActionPerformed;

        public string ProfileId => _profile != null ? _profile.ProfileId : "<no profile>";
        public InputProfileSO Profile => _profile;

        protected virtual void Awake()
        {
            _registered = TutorialServiceRegistration.TryRegister<ITutorialInputService>(this, this);
            if (!_registered)
            {
                enabled = false;
                return;
            }
            BindProfile();
        }

        protected virtual void OnDestroy()
        {
            UnbindProfile();
            ActionPerformed = null;
            if (_registered)
                TutorialServiceRegistration.Deregister<ITutorialInputService>(this);
            _registered = false;
        }

        protected virtual void LateUpdate()
        {
            // Cleared at the END of the frame so every validator polled during Update sees
            // the same edge, regardless of script execution order.
            _performedThisFrame.Clear();
        }

        /// <summary>Swaps the hardware profile at runtime. Sequences are unaffected.</summary>
        public void SetProfile(InputProfileSO profile)
        {
            UnbindProfile();
            _profile = profile;
            _unmappedReported.Clear();
            BindProfile();
        }

        #region ITutorialInputService

        public bool IsMapped(ActionId action)
        {
            InputAction resolved;
            return TryResolve(action, out resolved);
        }

        public bool WasPerformedThisFrame(ActionId action)
        {
            InputAction resolved;
            if (!TryResolve(action, out resolved))
                return false;
            return _performedThisFrame.Contains(action) || resolved.WasPressedThisFrame();
        }

        public bool IsHeld(ActionId action)
        {
            InputAction resolved;
            return TryResolve(action, out resolved) && resolved.IsPressed();
        }

        public float ReadValue(ActionId action)
        {
            InputAction resolved;
            if (!TryResolve(action, out resolved))
                return 0f;

            try
            {
                return resolved.ReadValue<float>();
            }
            catch (InvalidOperationException)
            {
                // Action is not float-typed (button/vector). Fall back to a binary read.
                return resolved.IsPressed() ? 1f : 0f;
            }
        }

        #endregion

        /// <summary>
        /// Resolves once and caches. An unmapped action is reported a single time per id
        /// so a step that waits forever still leaves one actionable error (CASE 04).
        /// </summary>
        private bool TryResolve(ActionId action, out InputAction inputAction)
        {
            inputAction = null;
            if (!action.IsValid)
                return false;

            if (_resolved.TryGetValue(action, out inputAction) && inputAction != null)
                return true;

            if (_profile == null)
            {
                ReportUnmapped(action, "no InputProfile assigned on '" + name + "'");
                return false;
            }

            if (!_profile.TryResolve(action, out inputAction))
            {
                ReportUnmapped(action, "profile '" + _profile.name + "' does not bind it.\nMapped actions:\n" +
                                       _profile.DumpMappedActions());
                return false;
            }

            _resolved[action] = inputAction;
            EnableAction(inputAction);
            return true;
        }

        private void ReportUnmapped(ActionId action, string detail)
        {
            if (!_unmappedReported.Add(action.Value))
                return;
            Debug.LogError("[Tutorial] UNMAPPED ActionId '" + action + "': " + detail, this);
        }

        private void BindProfile()
        {
            _resolved.Clear();
            if (_profile == null)
            {
                Debug.LogWarning("[Tutorial] '" + name + "' has no InputProfile; every ActionId will report unmapped.", this);
                return;
            }

            IReadOnlyList<InputProfileSO.ActionBinding> bindings = _profile.Bindings;
            if (bindings == null)
                return;

            for (int i = 0; i < bindings.Count; i++)
            {
                InputProfileSO.ActionBinding binding = bindings[i];
                if (binding == null || !binding.ActionId.IsValid)
                    continue;

                InputAction action;
                if (!_profile.TryResolve(binding.ActionId, out action))
                    continue;

                _resolved[binding.ActionId] = action;
                EnableAction(action);
            }
        }

        private void EnableAction(InputAction action)
        {
            if (action == null || _enabled.Contains(action))
                return;

            action.performed += HandlePerformed;

            // Remember ownership BEFORE enabling: an action another system already turned
            // on (AutoHand, a UI module, a simulator) must never be turned off by us.
            if (!action.enabled)
            {
                action.Enable();
                _enabledByThisService.Add(action);
            }
            _enabled.Add(action);
        }

        private void UnbindProfile()
        {
            for (int i = 0; i < _enabled.Count; i++)
            {
                InputAction action = _enabled[i];
                if (action == null)
                    continue;

                action.performed -= HandlePerformed;
                if (_enabledByThisService.Contains(action))
                    action.Disable();
            }
            _enabled.Clear();
            _enabledByThisService.Clear();
            _resolved.Clear();
            _performedThisFrame.Clear();
        }

        private void HandlePerformed(InputAction.CallbackContext context)
        {
            // Reverse lookup: the callback gives us the concrete action, we publish the
            // abstract id so nothing downstream learns the hardware name.
            foreach (KeyValuePair<ActionId, InputAction> pair in _resolved)
            {
                if (pair.Value != context.action)
                    continue;

                _performedThisFrame.Add(pair.Key);
                Action<ActionId> handler = ActionPerformed;
                if (handler != null)
                    handler.Invoke(pair.Key);
                return;
            }
        }
    }
}
