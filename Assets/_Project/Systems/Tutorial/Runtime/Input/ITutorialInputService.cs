using System;

namespace RideSafe.Tutorial
{
    /// <summary>
    /// Translates <see cref="ActionId"/> into input state. This is the only type in the
    /// tutorial that is allowed to know about the Input System; validators talk to this
    /// interface and stay hardware-agnostic.
    /// </summary>
    public interface ITutorialInputService
    {
        /// <summary>Id of the profile currently loaded, e.g. "quest3".</summary>
        string ProfileId { get; }

        /// <summary>
        /// False when the active profile has no binding for the action. Callers should
        /// report this loudly rather than silently never satisfying a step (CASE 04).
        /// </summary>
        bool IsMapped(ActionId action);

        /// <summary>True on the frame the action went from released to pressed.</summary>
        bool WasPerformedThisFrame(ActionId action);

        /// <summary>True while the action is held.</summary>
        bool IsHeld(ActionId action);

        /// <summary>Analog magnitude, 0..1 for triggers, for future speed/brake work.</summary>
        float ReadValue(ActionId action);

        /// <summary>Raised once per performed action. Useful for hint suppression.</summary>
        event Action<ActionId> ActionPerformed;
    }
}
