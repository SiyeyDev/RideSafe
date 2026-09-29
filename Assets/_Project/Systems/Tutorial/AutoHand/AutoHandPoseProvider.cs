using Autohand;
using Autohand.Demo;
using RideSafe.TaskSequence;
using UnityEngine;
using UnityEngine.XR;

namespace RideSafe.Tutorial.AutoHand
{
    /// <summary>
    /// <see cref="IXRPoseProvider"/> over the XRPlayer rig. Tracks nothing itself.
    /// <para>
    /// Poses are the rig's own transforms ("Camera (head)", "Controller (left/right)"),
    /// which XRPlayer already drives from tracking. Tracking state comes from the devices
    /// AutoHand's <see cref="XRHandControllerLink"/> already resolved for each hand.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-300)]
    [AddComponentMenu("RideSafe/Tutorial/AutoHand Pose Provider")]
    public class AutoHandPoseProvider : MonoBehaviour, IXRPoseProvider
    {
        [Tooltip("XRPlayer's head camera.")]
        [SerializeField] private Transform _head;

        [Tooltip("XRPlayer's tracked left controller (not the physics hand, which can be blocked by colliders).")]
        [SerializeField] private Transform _leftController;

        [Tooltip("XRPlayer's tracked right controller.")]
        [SerializeField] private Transform _rightController;

        private bool _registered;

        protected virtual void Awake()
        {
            _registered = ServiceRegistration.TryRegister<IXRPoseProvider>(this, this);
            if (!_registered)
                enabled = false;
        }

        protected virtual void OnDestroy()
        {
            if (_registered)
                ServiceRegistration.Deregister<IXRPoseProvider>(this);
            _registered = false;
        }

        public bool IsTracked(XRNodeRole role)
        {
            if (role == XRNodeRole.Head)
                return _head != null;

            XRHandControllerLink link = (role == XRNodeRole.LeftHand ? HandControllerLink.handLeft : HandControllerLink.handRight)
                as XRHandControllerLink;
            if (link == null)
                return false;

            foreach (InputDevice device in link.Devices())
            {
                bool tracked;
                if (device.isValid && device.TryGetFeatureValue(CommonUsages.isTracked, out tracked) && tracked)
                    return true;
            }
            return false;
        }

        public bool TryGetPose(XRNodeRole role, out Pose pose)
        {
            Transform source = role == XRNodeRole.Head ? _head
                : role == XRNodeRole.LeftHand ? _leftController
                : _rightController;

            pose = source != null ? new Pose(source.position, source.rotation) : default;
            return source != null;
        }
    }
}
