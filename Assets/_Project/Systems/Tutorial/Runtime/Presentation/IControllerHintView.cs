using UnityEngine;

namespace RideSafe.Tutorial
{
    /// <summary>
    /// Illustration of which physical control to use. Purely instructive: a view never
    /// reports anything back, so its animation can never complete a step. Only the learner's
    /// real input, read by validators, does.
    /// </summary>
    public interface IControllerHintView
    {
        /// <summary>Implemented by <see cref="Component.transform"/>; the presenter places the view.</summary>
        Transform transform { get; }

        bool IsVisible { get; }

        /// <summary>True while the view plays its "pressed" confirmation before hiding itself.</summary>
        bool IsReleasing { get; }

        /// <summary>
        /// Shows the controller of <c>binding.Hand</c> with <c>binding.Control</c> highlighted
        /// and demonstrating its press, until <see cref="Release"/> or <see cref="Hide"/>.
        /// False when this view cannot draw that control, so a caller can fall back.
        /// </summary>
        bool Show(PhysicalBinding binding);

        /// <summary>The learner performed the action: play a short pressed pose, then hide.</summary>
        void Release();

        /// <summary>Hides at once and restores every part to its rest state.</summary>
        void Hide();
    }

    /// <summary>
    /// A hint view that places itself relative to the learner's head instead of being placed
    /// next to the target, e.g. a large didactic controller kept in a comfortable view zone.
    /// </summary>
    public interface IHeadRelativeHintView
    {
        /// <param name="viewer">The learner's head.</param>
        /// <param name="focus">What the step is about (target or hand); the view leans toward it.</param>
        void Place(Pose viewer, Vector3 focus);
    }

    /// <summary>Short controller vibration on one hand, as feedback for the learner's own action.</summary>
    public interface IHapticFeedback
    {
        bool TryPulse(XRNodeRole hand, float amplitude, float seconds);
    }
}
