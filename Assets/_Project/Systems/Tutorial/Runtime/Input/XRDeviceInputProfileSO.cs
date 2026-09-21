using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace RideSafe.Tutorial
{
    /// <summary>
    /// Physical controller buttons as exposed by <see cref="CommonUsages"/>, the same API
    /// AutoHand's XRPlayer reads. Deliberately independent of AutoHand's own CommonButton
    /// enum, which lives in its Examples assembly.
    /// </summary>
    public enum XRControllerButton
    {
        Trigger = 0,
        Grip = 1,
        /// <summary>A on the right Quest controller, X on the left.</summary>
        Primary = 2,
        /// <summary>B on the right Quest controller, Y on the left.</summary>
        Secondary = 3,
        /// <summary>Only usable on the LEFT Quest controller; the right one is reserved by the OS.</summary>
        Menu = 4,
        ThumbstickClick = 5,
        ThumbstickTouch = 6,
        PrimaryTouch = 7,
        SecondaryTouch = 8
    }

    /// <summary>
    /// ActionId -> (hand, CommonUsages button) mapping. The project-default input profile,
    /// because it reads controllers exactly the way AutoHand's XRPlayer does.
    /// <para>
    /// This is the ONLY place a physical button is named. Sequences name ActionIds, so
    /// remapping a button or supporting new hardware never touches a sequence (CASE 14).
    /// </para>
    /// </summary>
    [CreateAssetMenu(fileName = "SO_XRDeviceInputProfile", menuName = "RideSafe/XR Device Input Profile", order = 11)]
    public class XRDeviceInputProfileSO : ScriptableObject
    {
        [Serializable]
        public class Binding
        {
            [Tooltip("Abstract action, e.g. primaryselect. Use ActionIds constants.")]
            [SerializeField] private string _actionId;

            [Tooltip("LeftHand or RightHand. Head is not a controller and is treated as unmapped.")]
            [SerializeField] private XRNodeRole _hand = XRNodeRole.RightHand;

            [SerializeField] private XRControllerButton _button = XRControllerButton.Trigger;

            public Binding() { }

            public Binding(string actionId, XRNodeRole hand, XRControllerButton button)
            {
                _actionId = actionId;
                _hand = hand;
                _button = button;
            }

            public ActionId ActionId => new ActionId(_actionId);
            public XRNodeRole Hand => _hand;
            public XRControllerButton Button => _button;
            public bool IsUsable => ActionId.IsValid && _hand != XRNodeRole.Head;

            public override string ToString() => ActionId + " -> " + _hand + "." + _button;
        }

        [Tooltip("Profile identity shown in logs and the tutorial context.")]
        [SerializeField] private string _profileId = "quest3.xrplayer";

        [SerializeField] private List<Binding> _bindings = new List<Binding>();

        public string ProfileId => string.IsNullOrWhiteSpace(_profileId) ? name : _profileId.Trim().ToLowerInvariant();
        public IReadOnlyList<Binding> Bindings => _bindings;

        public bool TryGetBinding(ActionId action, out Binding binding)
        {
            binding = null;
            if (!action.IsValid || _bindings == null)
                return false;

            for (int i = 0; i < _bindings.Count; i++)
            {
                Binding candidate = _bindings[i];
                if (candidate != null && candidate.IsUsable && candidate.ActionId == action)
                {
                    binding = candidate;
                    return true;
                }
            }
            return false;
        }

        public string DumpMappedActions()
        {
            if (_bindings == null || _bindings.Count == 0)
                return "(profile has no bindings)";

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < _bindings.Count; i++)
            {
                if (_bindings[i] != null && _bindings[i].IsUsable)
                    builder.AppendLine("  - " + _bindings[i]);
            }
            return builder.Length == 0 ? "(profile has no usable bindings)" : builder.ToString();
        }

        public static InputFeatureUsage<bool> ToButtonUsage(XRControllerButton button)
        {
            switch (button)
            {
                case XRControllerButton.Grip: return CommonUsages.gripButton;
                case XRControllerButton.Primary: return CommonUsages.primaryButton;
                case XRControllerButton.Secondary: return CommonUsages.secondaryButton;
                case XRControllerButton.Menu: return CommonUsages.menuButton;
                case XRControllerButton.ThumbstickClick: return CommonUsages.primary2DAxisClick;
                case XRControllerButton.ThumbstickTouch: return CommonUsages.primary2DAxisTouch;
                case XRControllerButton.PrimaryTouch: return CommonUsages.primaryTouch;
                case XRControllerButton.SecondaryTouch: return CommonUsages.secondaryTouch;
                default: return CommonUsages.triggerButton;
            }
        }

        /// <summary>Analog counterpart, for buttons that have one (trigger and grip).</summary>
        public static bool TryGetAxisUsage(XRControllerButton button, out InputFeatureUsage<float> usage)
        {
            if (button == XRControllerButton.Trigger)
            {
                usage = CommonUsages.trigger;
                return true;
            }
            if (button == XRControllerButton.Grip)
            {
                usage = CommonUsages.grip;
                return true;
            }
            usage = default;
            return false;
        }

        /// <summary>
        /// Provisional Quest 3 defaults, applied when the asset is created or reset.
        /// PrimarySelect is the right trigger because that is XRPlayer's selectInput.
        /// Grip is left unbound on purpose: XRPlayer uses it to grab and to point, and the
        /// tutorial must not give it a second meaning.
        /// </summary>
        private void Reset()
        {
            _profileId = "quest3.xrplayer";
            _bindings = new List<Binding>
            {
                new Binding(ActionIds.PrimarySelect, XRNodeRole.RightHand, XRControllerButton.Trigger),
                new Binding(ActionIds.SecondarySelect, XRNodeRole.LeftHand, XRControllerButton.Trigger),
                new Binding(ActionIds.Confirm, XRNodeRole.RightHand, XRControllerButton.Primary),
                new Binding(ActionIds.Back, XRNodeRole.RightHand, XRControllerButton.Secondary),
                new Binding(ActionIds.Pause, XRNodeRole.LeftHand, XRControllerButton.Menu),
                new Binding(ActionIds.Recenter, XRNodeRole.LeftHand, XRControllerButton.Secondary)
            };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_bindings == null)
                return;

            HashSet<ActionId> seen = new HashSet<ActionId>();
            for (int i = 0; i < _bindings.Count; i++)
            {
                Binding binding = _bindings[i];
                if (binding == null)
                    continue;

                if (binding.Hand == XRNodeRole.Head)
                    Debug.LogWarning("[Tutorial] '" + name + "': " + binding.ActionId +
                                     " is bound to Head, which has no buttons. It will report unmapped.", this);

                if (binding.Hand == XRNodeRole.RightHand && binding.Button == XRControllerButton.Menu)
                    Debug.LogWarning("[Tutorial] '" + name + "': " + binding.ActionId +
                                     " uses the RIGHT menu button, which Quest reserves for the system. " +
                                     "The app will never receive it.", this);

                if (binding.ActionId.IsValid && !seen.Add(binding.ActionId))
                    Debug.LogError("[Tutorial] '" + name + "' maps ActionId '" + binding.ActionId +
                                   "' more than once. Only the first wins.", this);
            }
        }
#endif
    }
}
