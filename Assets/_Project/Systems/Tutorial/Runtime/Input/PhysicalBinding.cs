using System;

namespace RideSafe.Tutorial
{
    /// <summary>
    /// A physical control on a hand controller, named without any XR backend. The tutorial
    /// asks for <see cref="ActionId"/>s; only presentation needs to know which control that
    /// is, to draw it. Face buttons are positional: Primary is A on the right controller and
    /// X on the left, Secondary is B / Y.
    /// </summary>
    public enum ControllerControl
    {
        None = 0,
        Trigger = 1,
        Grip = 2,
        PrimaryButton = 3,
        SecondaryButton = 4,
        Thumbstick = 5,
        /// <summary>Left controller's menu button.</summary>
        Menu = 6,
        /// <summary>Right controller's Meta/system button. Reserved by the OS; only ever drawn.</summary>
        System = 7
    }

    /// <summary>Where an action lives physically: which hand, which control.</summary>
    public readonly struct PhysicalBinding : IEquatable<PhysicalBinding>
    {
        public PhysicalBinding(XRNodeRole hand, ControllerControl control)
        {
            Hand = hand;
            Control = control;
        }

        public XRNodeRole Hand { get; }
        public ControllerControl Control { get; }

        public bool IsLeftHand => Hand == XRNodeRole.LeftHand;

        public bool IsValid => Hand != XRNodeRole.Head && Control != ControllerControl.None;

        public bool Equals(PhysicalBinding other) => Hand == other.Hand && Control == other.Control;
        public override bool Equals(object obj) => obj is PhysicalBinding other && Equals(other);
        public override int GetHashCode() => ((int)Hand * 397) ^ (int)Control;
        public override string ToString() => Hand + "/" + Control;
    }

    /// <summary>
    /// Resolves an action to its physical control under the active input profile. Lets
    /// presentation draw "the button for PrimarySelect" without knowing the XR backend or the
    /// profile (handheld today, mounted later).
    /// </summary>
    public interface IPhysicalBindingResolver
    {
        /// <summary>False when the active profile has no usable binding for the action.</summary>
        bool TryResolve(ActionId action, out PhysicalBinding binding);
    }
}
