using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace RideSafe.Tutorial
{
    /// <summary>
    /// Project-default <see cref="ITutorialInputService"/>. Reads controllers through
    /// <see cref="InputDevices"/> + <see cref="CommonUsages"/>, the same way AutoHand's
    /// XRPlayer (<c>XRHandControllerLink</c>) does, so the tutorial and the player always
    /// agree on what a button press is.
    /// <para>
    /// Owns no Input System actions and enables nothing, so it cannot interfere with the
    /// player's input in any direction.
    /// </para>
    /// <para>
    /// Runs before <c>TaskSequenceService</c> (order -300 vs -200) so validators polled in
    /// the same frame see this frame's button edges, not last frame's.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-300)]
    [AddComponentMenu("RideSafe/Tutorial XR Device Input Service")]
    public class XRDeviceInputService : MonoBehaviour, ITutorialInputService
    {
        private sealed class ActionState
        {
            public XRDeviceInputProfileSO.Binding Binding;
            public bool Held;
            public bool PressedThisFrame;
        }

        [Tooltip("Active controller profile. Swapping it must never require editing a sequence.")]
        [SerializeField] private XRDeviceInputProfileSO _profile;

        private readonly Dictionary<ActionId, ActionState> _states = new Dictionary<ActionId, ActionState>();
        private readonly HashSet<string> _unmappedReported = new HashSet<string>();
        private readonly List<InputDevice> _deviceBuffer = new List<InputDevice>();
        private readonly List<ActionId> _performedBuffer = new List<ActionId>();

        private InputDevice _left;
        private InputDevice _right;
        private bool _registered;

        public event Action<ActionId> ActionPerformed;

        public string ProfileId => _profile != null ? _profile.ProfileId : "<no profile>";
        public XRDeviceInputProfileSO Profile => _profile;

        protected virtual void Awake()
        {
            _registered = TutorialServiceRegistration.TryRegister<ITutorialInputService>(this, this);
            if (!_registered)
            {
                enabled = false;
                return;
            }
            BuildStates();
        }

        protected virtual void OnDisable()
        {
            // A disabled service must not keep reporting a button as held.
            foreach (ActionState state in _states.Values)
            {
                state.Held = false;
                state.PressedThisFrame = false;
            }
        }

        protected virtual void OnDestroy()
        {
            ActionPerformed = null;
            _states.Clear();
            if (_registered)
                TutorialServiceRegistration.Deregister<ITutorialInputService>(this);
            _registered = false;
        }

        protected virtual void Update()
        {
            _left = FetchDevice(XRNode.LeftHand);
            _right = FetchDevice(XRNode.RightHand);

            _performedBuffer.Clear();
            foreach (KeyValuePair<ActionId, ActionState> pair in _states)
            {
                ActionState state = pair.Value;
                bool now = ReadButton(state.Binding);

                state.PressedThisFrame = now && !state.Held;
                state.Held = now;

                if (state.PressedThisFrame)
                    _performedBuffer.Add(pair.Key);
            }

            // Notify after the sweep, so a listener that swaps the profile cannot
            // invalidate the dictionary mid-enumeration.
            Action<ActionId> handler = ActionPerformed;
            if (handler == null)
                return;
            for (int i = 0; i < _performedBuffer.Count; i++)
                handler.Invoke(_performedBuffer[i]);
        }

        /// <summary>Swaps the controller profile at runtime. Sequences are unaffected.</summary>
        public void SetProfile(XRDeviceInputProfileSO profile)
        {
            _profile = profile;
            _unmappedReported.Clear();
            BuildStates();
        }

        #region ITutorialInputService

        public bool IsMapped(ActionId action)
        {
            ActionState state;
            return TryGetState(action, out state);
        }

        public bool WasPerformedThisFrame(ActionId action)
        {
            ActionState state;
            return TryGetState(action, out state) && state.PressedThisFrame;
        }

        public bool IsHeld(ActionId action)
        {
            ActionState state;
            return TryGetState(action, out state) && state.Held;
        }

        public float ReadValue(ActionId action)
        {
            ActionState state;
            if (!TryGetState(action, out state))
                return 0f;

            InputFeatureUsage<float> axis;
            if (XRDeviceInputProfileSO.TryGetAxisUsage(state.Binding.Button, out axis))
            {
                InputDevice device = DeviceFor(state.Binding.Hand);
                float value;
                if (device.isValid && device.TryGetFeatureValue(axis, out value))
                    return value;
            }
            return state.Held ? 1f : 0f;
        }

        #endregion

        private void BuildStates()
        {
            _states.Clear();
            if (_profile == null)
            {
                Debug.LogWarning("[Tutorial] '" + name + "' has no XRDeviceInputProfile; every ActionId will report unmapped.", this);
                return;
            }

            IReadOnlyList<XRDeviceInputProfileSO.Binding> bindings = _profile.Bindings;
            if (bindings == null)
                return;

            for (int i = 0; i < bindings.Count; i++)
            {
                XRDeviceInputProfileSO.Binding binding = bindings[i];
                if (binding == null || !binding.IsUsable || _states.ContainsKey(binding.ActionId))
                    continue;
                _states.Add(binding.ActionId, new ActionState { Binding = binding });
            }
        }

        /// <summary>Reports an unmapped action once per id, then stays quiet (CASE 04).</summary>
        private bool TryGetState(ActionId action, out ActionState state)
        {
            state = null;
            if (!action.IsValid)
                return false;
            if (_states.TryGetValue(action, out state))
                return true;

            if (_unmappedReported.Add(action.Value))
            {
                string detail = _profile == null
                    ? "no XRDeviceInputProfile assigned on '" + name + "'"
                    : "profile '" + _profile.name + "' does not bind it.\nMapped actions:\n" + _profile.DumpMappedActions();
                Debug.LogError("[Tutorial] UNMAPPED ActionId '" + action + "': " + detail, this);
            }
            return false;
        }

        private bool ReadButton(XRDeviceInputProfileSO.Binding binding)
        {
            InputDevice device = DeviceFor(binding.Hand);
            if (!device.isValid)
                return false;

            bool pressed;
            return device.TryGetFeatureValue(XRDeviceInputProfileSO.ToButtonUsage(binding.Button), out pressed) && pressed;
        }

        private InputDevice DeviceFor(XRNodeRole hand) =>
            hand == XRNodeRole.LeftHand ? _left : (hand == XRNodeRole.RightHand ? _right : default);

        private InputDevice FetchDevice(XRNode node)
        {
            InputDevices.GetDevicesAtXRNode(node, _deviceBuffer);
            return _deviceBuffer.Count > 0 ? _deviceBuffer[0] : default;
        }
    }
}
