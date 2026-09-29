using System;
using System.Collections.Generic;
using Autohand;
using Autohand.Demo;
using RideSafe.TaskSequence;
using UnityEngine;

namespace RideSafe.Tutorial.AutoHand
{
    /// <summary>
    /// <see cref="ITutorialInputService"/> over AutoHand's own controller links.
    /// <para>
    /// Buttons are read through <see cref="XRHandControllerLink.ButtonPressed"/>, the same
    /// call XRPlayer's grab, point and UI components use, so the tutorial and the player
    /// always agree on what a press is and on which device is each hand. The links come
    /// from AutoHand's static <see cref="HandControllerLink.handLeft"/>/<see cref="HandControllerLink.handRight"/>.
    /// </para>
    /// <para>
    /// AutoHand exposes button state, not button events (XRControllerEvent polls the same
    /// way), so edges are derived here once per frame, before TaskSequenceService ticks.
    /// </para>
    /// <para>
    /// The bindings are the ONLY place a physical button is named. Sequences ask for
    /// ActionIds such as primaryselect.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-300)]
    [AddComponentMenu("RideSafe/Tutorial/AutoHand Tutorial Input Service")]
    public class AutoHandTutorialInputService : MonoBehaviour, ITutorialInputService
    {
        [Serializable]
        public class Binding
        {
            [Tooltip("Abstract action, e.g. primaryselect. Use ActionIds constants.")]
            [SerializeField] private string _actionId;

            [Tooltip("LeftHand or RightHand.")]
            [SerializeField] private XRNodeRole _hand = XRNodeRole.RightHand;

            [SerializeField] private CommonButton _button = CommonButton.triggerButton;

            public Binding() { }

            public Binding(string actionId, XRNodeRole hand, CommonButton button)
            {
                _actionId = actionId;
                _hand = hand;
                _button = button;
            }

            public ActionId ActionId => new ActionId(_actionId);
            public XRNodeRole Hand => _hand;
            public CommonButton Button => _button;
            public bool IsUsable => ActionId.IsValid && _hand != XRNodeRole.Head && _button != CommonButton.none;
        }

        private sealed class ActionState
        {
            public Binding Binding;
            public bool Held;
            public bool PressedThisFrame;
        }

        [SerializeField] private List<Binding> _bindings = new List<Binding>();

        private readonly Dictionary<ActionId, ActionState> _states = new Dictionary<ActionId, ActionState>();
        private readonly HashSet<string> _unmappedReported = new HashSet<string>();
        private readonly List<ActionId> _performedBuffer = new List<ActionId>();
        private bool _registered;

        public event Action<ActionId> ActionPerformed;

        public string ProfileId => "autohand.xrplayer";

        protected virtual void Awake()
        {
            _registered = ServiceRegistration.TryRegister<ITutorialInputService>(this, this);
            if (!_registered)
            {
                enabled = false;
                return;
            }

            for (int i = 0; i < _bindings.Count; i++)
            {
                Binding binding = _bindings[i];
                if (binding != null && binding.IsUsable && !_states.ContainsKey(binding.ActionId))
                    _states.Add(binding.ActionId, new ActionState { Binding = binding });
            }
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
            if (_registered)
                ServiceRegistration.Deregister<ITutorialInputService>(this);
            _registered = false;
        }

        protected virtual void Update()
        {
            _performedBuffer.Clear();
            foreach (KeyValuePair<ActionId, ActionState> pair in _states)
            {
                ActionState state = pair.Value;
                XRHandControllerLink link = LinkFor(state.Binding.Hand);
                bool now = link != null && link.ButtonPressed(state.Binding.Button);

                state.PressedThisFrame = now && !state.Held;
                state.Held = now;
                if (state.PressedThisFrame)
                    _performedBuffer.Add(pair.Key);
            }

            // Notify after the sweep so a listener cannot invalidate the enumeration.
            Action<ActionId> handler = ActionPerformed;
            if (handler == null)
                return;
            for (int i = 0; i < _performedBuffer.Count; i++)
                handler.Invoke(_performedBuffer[i]);
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

            XRHandControllerLink link = LinkFor(state.Binding.Hand);
            if (link != null && state.Binding.Button == CommonButton.triggerButton)
                return link.GetAxis(CommonAxis.trigger);
            if (link != null && state.Binding.Button == CommonButton.gripButton)
                return link.GetAxis(CommonAxis.grip);
            return state.Held ? 1f : 0f;
        }

        #endregion

        /// <summary>
        /// Where an action lives physically, for presentation (label on the right hand,
        /// haptic pulse). Keeps the hardware translation inside this adapter.
        /// </summary>
        public bool TryGetPhysicalBinding(ActionId action, out HandControllerLink link, out CommonButton button)
        {
            link = null;
            button = CommonButton.none;
            ActionState state;
            if (!TryGetState(action, out state))
                return false;

            link = LinkFor(state.Binding.Hand);
            button = state.Binding.Button;
            return link != null;
        }

        private static XRHandControllerLink LinkFor(XRNodeRole hand)
        {
            if (hand == XRNodeRole.LeftHand)
                return HandControllerLink.handLeft as XRHandControllerLink;
            if (hand == XRNodeRole.RightHand)
                return HandControllerLink.handRight as XRHandControllerLink;
            return null;
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
                Debug.LogError("[Tutorial] UNMAPPED ActionId '" + action + "' on '" + name + "'.", this);
            return false;
        }

        /// <summary>
        /// Quest 3 + XRPlayer defaults. PrimarySelect is the right trigger because that is
        /// what XRPlayer's UI pointer and select use. Grip is left unbound on purpose:
        /// XRPlayer uses it to grab and to point, and the tutorial must not give it a
        /// second meaning.
        /// </summary>
        private void Reset()
        {
            _bindings = new List<Binding>
            {
                new Binding(ActionIds.PrimarySelect, XRNodeRole.RightHand, CommonButton.triggerButton),
                new Binding(ActionIds.SecondarySelect, XRNodeRole.LeftHand, CommonButton.triggerButton),
                new Binding(ActionIds.Confirm, XRNodeRole.RightHand, CommonButton.primaryButton),
                new Binding(ActionIds.Back, XRNodeRole.RightHand, CommonButton.secondaryButton)
            };
        }
    }
}
