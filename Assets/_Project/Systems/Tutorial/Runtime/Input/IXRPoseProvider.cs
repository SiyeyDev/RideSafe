using UnityEngine;
using UnityEngine.XR;

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
    /// this instead of touching <c>UnityEngine.XR</c> directly, so they stay testable and
    /// the hardware stays swappable.
    /// </summary>
    public interface IXRPoseProvider
    {
        bool IsTracked(XRNodeRole role);
        bool TryGetPose(XRNodeRole role, out Pose pose);
    }

    /// <summary>
    /// Default provider backed by <see cref="InputDevices"/>. Works with OpenXR without
    /// requiring XR Interaction Toolkit, which this project does not have installed.
    /// </summary>
    public class UnityXRPoseProvider : IXRPoseProvider
    {
        private readonly Transform _headFallback;

        /// <param name="headFallback">
        /// Camera transform used for head pose when no HMD is present, so the tutorial is
        /// still testable in the editor without a headset.
        /// </param>
        public UnityXRPoseProvider(Transform headFallback = null) => _headFallback = headFallback;

        public bool IsTracked(XRNodeRole role)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(ToNode(role));
            if (!device.isValid)
                return role == XRNodeRole.Head && _headFallback != null;

            bool tracked;
            if (device.TryGetFeatureValue(CommonUsages.isTracked, out tracked))
                return tracked;

            return false;
        }

        public bool TryGetPose(XRNodeRole role, out Pose pose)
        {
            pose = default;
            InputDevice device = InputDevices.GetDeviceAtXRNode(ToNode(role));

            if (device.isValid)
            {
                Vector3 position;
                Quaternion rotation;
                bool hasPosition = device.TryGetFeatureValue(CommonUsages.devicePosition, out position);
                bool hasRotation = device.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation);

                if (hasPosition || hasRotation)
                {
                    pose = new Pose(hasPosition ? position : Vector3.zero,
                                    hasRotation ? rotation : Quaternion.identity);
                    return true;
                }
            }

            if (role == XRNodeRole.Head && _headFallback != null)
            {
                pose = new Pose(_headFallback.position, _headFallback.rotation);
                return true;
            }
            return false;
        }

        private static XRNode ToNode(XRNodeRole role)
        {
            if (role == XRNodeRole.LeftHand)
                return XRNode.LeftHand;
            if (role == XRNodeRole.RightHand)
                return XRNode.RightHand;
            return XRNode.CenterEye;
        }
    }
}
