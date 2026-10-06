using UnityEngine;

namespace RideSafe.UI
{
    /// <summary>
    /// Keeps a world-space panel readable without gluing it to the head: it stays put while it
    /// sits inside a dead zone around where the user is looking, and only eases back into place
    /// once they turn far enough away.
    /// <para>
    /// UI that tracks the head one-to-one is the usual cause of discomfort, and UI nailed to the
    /// world is lost the moment the user turns around. This lazy follow keeps the panel findable
    /// while ignoring the constant micro-movements of a head.
    /// </para>
    /// </summary>
    public class GazeFollower : MonoBehaviour
    {
        [Tooltip("Empty uses the main camera, which in VR is the head.")]
        [SerializeField] private Transform _head;
        [SerializeField] private float _distance = 2f;
        [Tooltip("Degrees below the eye line. Reading below the horizon is more comfortable and keeps the view clear.")]
        [SerializeField] private float _pitchOffsetDegrees = 14f;
        [Tooltip("The panel does not move while it stays within this angle of the gaze.")]
        [SerializeField] private float _deadZoneDegrees = 16f;
        [Tooltip("Once moving, it stops again when it gets this close to the resting spot.")]
        [SerializeField] private float _settleDegrees = 2f;
        [Tooltip("Smoothing time. Higher is lazier.")]
        [SerializeField] private float _followSeconds = 0.45f;
        [Tooltip("Snap into place on enable instead of drifting in from wherever it was left.")]
        [SerializeField] private bool _snapOnEnable = true;

        private bool _following;
        private Vector3 _velocity;

        private void OnEnable()
        {
            if (!ResolveHead())
                return;
            _velocity = Vector3.zero;
            _following = false;
            if (_snapOnEnable)
            {
                transform.position = RestingPosition();
                FaceHead();
            }
        }

        private void LateUpdate()
        {
            if (!ResolveHead())
                return;

            float offGaze = Vector3.Angle(_head.forward, transform.position - _head.position);
            if (!_following && offGaze > _deadZoneDegrees)
                _following = true;

            if (_following)
            {
                transform.position = Vector3.SmoothDamp(transform.position, RestingPosition(),
                    ref _velocity, _followSeconds);
                if (Vector3.Angle(_head.forward, transform.position - _head.position) < _settleDegrees)
                    _following = false;
            }
            FaceHead();
        }

        private bool ResolveHead()
        {
            if (_head != null)
                return true;
            Camera camera = Camera.main;
            if (camera != null)
                _head = camera.transform;
            return _head != null;
        }

        private Vector3 RestingPosition()
        {
            Quaternion pitch = Quaternion.AngleAxis(_pitchOffsetDegrees, _head.right);
            return _head.position + pitch * _head.forward * _distance;
        }

        /// <summary>Canvases are read from behind their forward axis, so forward points away from the head.</summary>
        private void FaceHead()
        {
            Vector3 away = transform.position - _head.position;
            if (away.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(away, Vector3.up);
        }
    }
}
