using UnityEngine;

namespace RideSafe.Tutorial
{
    /// <summary>Which tracked node a validator is asking about.</summary>
    public enum XRNodeRole
    {
        Head = 0,
        LeftHand = 1,
        RightHand = 2
    }

    /// <summary>
    /// Tracking seam. Validators that care about controllers or head movement go through
    /// this instead of touching XR directly, so they stay testable and the tracking source
    /// (AutoHand's XRPlayer rig) stays swappable.
    /// </summary>
    public interface IXRPoseProvider
    {
        bool IsTracked(XRNodeRole role);
        bool TryGetPose(XRNodeRole role, out Pose pose);
    }
}
