using UnityEngine;

namespace RideSafe.Tutorial.AutoHand
{
    /// <summary>
    /// Places this object in front of where the learner is looking, relative to XRPlayer's
    /// head (HMD) instead of assumed floor or eye heights, so it works seated and standing.
    /// <para>
    /// Uses only the head's yaw (a downward glance does not push the object into the floor)
    /// and places once per <see cref="Place"/>: the object stays world-locked afterwards.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/Tutorial/Head Relative Placement")]
    public class HeadRelativePlacement : MonoBehaviour
    {
        [Tooltip("XRPlayer's head camera.")]
        [SerializeField] private Transform _head;

        [Tooltip("Horizontal distance in front of the head, in metres.")]
        [SerializeField, Min(0.3f)] private float _forwardDistance = 1.2f;

        [Tooltip("Metres relative to eye height. Negative is below the eyes.")]
        [SerializeField] private float _verticalOffset = -0.15f;

        public float ForwardDistance => _forwardDistance;
        public float VerticalOffset => _verticalOffset;

        /// <summary>Moves this object in front of the head as it is right now, facing the learner.</summary>
        public void Place()
        {
            if (_head == null)
            {
                Debug.LogError("[Tutorial] HeadRelativePlacement on '" + name + "' has no head assigned.", this);
                return;
            }
            Place(transform, new Pose(_head.position, _head.rotation), _forwardDistance, _verticalOffset);
        }

        public static void Place(Transform target, Pose head, float forwardDistance, float verticalOffset)
        {
            Vector3 forward = Vector3.ProjectOnPlane(head.rotation * Vector3.forward, Vector3.up);
            if (forward.sqrMagnitude < 1e-4f)
                forward = Vector3.ProjectOnPlane(head.rotation * Vector3.up, Vector3.up); // looking straight down/up
            if (forward.sqrMagnitude < 1e-4f)
                forward = Vector3.forward;
            forward.Normalize();

            target.SetPositionAndRotation(head.position + forward * forwardDistance + Vector3.up * verticalOffset,
                                          Quaternion.LookRotation(forward, Vector3.up));
        }
    }
}
