using Cachacos;
using RideSafe.TaskSequence;
using RideSafe.Tutorial;
using UnityEngine;

namespace RideSafe.Demo
{
    /// <summary>
    /// The one piece the existing PC mode lacked for the tutorial: tracked controllers.
    /// <para>
    /// Lives on DesktopMode, so it only runs in the PC mode the project already had (switched
    /// with RideSafe/Demo/Modo PC / Modo VR, which turns XRPlayer and DesktopMode on and off).
    /// It decides nothing about modes, moves no camera and touches no rig. It stands in as the
    /// <see cref="IXRPoseProvider"/>: head = the existing desktop camera, both controllers
    /// "tracked" at fixed offsets from it, so the hidden tracking wait passes and everything
    /// placed relative to the head lands in front of the desktop camera. The learner's
    /// actions stay real: hover and click through the EventSystem.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [AddComponentMenu("RideSafe/Demo/Desktop Tutorial Input Simulator")]
    public class DesktopTutorialInputSimulator : MonoBehaviour, IXRPoseProvider
    {
        [Tooltip("DesktopMode's camera; it is the learner's head while simulating.")]
        [SerializeField] private Camera _desktopCamera;

        [Header("Virtual controllers, relative to the camera")]
        [SerializeField] private Vector3 _leftOffset = new Vector3(-0.2f, -0.3f, 0.35f);
        [SerializeField] private Vector3 _rightOffset = new Vector3(0.2f, -0.3f, 0.35f);

        private bool _registered;

        /// <summary>
        /// Runs after every Awake: the scene's own provider has registered by then, and taking
        /// its slot here (instead of registering first) means nobody logs a duplicate-service
        /// error, which with the Console's Error Pause on would freeze play. Before the
        /// tutorial's Start through the execution order, so the first tracking check sees this.
        /// </summary>
        protected virtual void Start() => TakeOverPoseService();

        protected virtual void OnDestroy()
        {
            if (_registered)
                ServiceRegistration.Deregister<IXRPoseProvider>(this);
            _registered = false;
        }

        internal void TakeOverPoseService()
        {
            IXRPoseProvider existing = ServiceLocator.Instance.RequestService<IXRPoseProvider>();
            if (existing != null && !ReferenceEquals(existing, this))
                ServiceRegistration.Deregister(existing);
            _registered = ServiceRegistration.TryRegister<IXRPoseProvider>(this, this);
        }

        public bool IsTracked(XRNodeRole role) => _desktopCamera != null;

        public bool TryGetPose(XRNodeRole role, out Pose pose)
        {
            if (_desktopCamera == null)
            {
                pose = default(Pose);
                return false;
            }

            Transform head = _desktopCamera.transform;
            Vector3 position = role == XRNodeRole.LeftHand ? head.TransformPoint(_leftOffset)
                : role == XRNodeRole.RightHand ? head.TransformPoint(_rightOffset)
                : head.position;
            pose = new Pose(position, head.rotation);
            return true;
        }
    }
}
