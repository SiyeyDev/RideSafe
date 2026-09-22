using System;
using UnityEngine;

namespace RideSafe.Tutorial
{
    /// <summary>Which controllers a tracking validator watches.</summary>
    public enum ControllerSelection
    {
        Either = 0,
        Left = 1,
        Right = 2,
        Both = 3
    }

    /// <summary>
    /// Satisfied while the requested controllers report tracking for <c>_stableSeconds</c>.
    /// <para>
    /// This is step 1 of the initial tutorial: confirm both controllers are held and
    /// tracked before teaching anything. Losing tracking resets the timer, so a brief
    /// glitch does not count as a pass.
    /// </para>
    /// </summary>
    [Serializable]
    public class ControllerTrackedValidator : TutorialValidatorBase
    {
        [SerializeField] private ControllerSelection _controllers = ControllerSelection.Both;

        [Tooltip("Continuous seconds of stable tracking required.")]
        [SerializeField, Min(0f)] private float _stableSeconds = 0.5f;

        private IXRPoseProvider _pose;
        private float _stableFor;

        protected override void OnPrepare()
        {
            _stableFor = 0f;
            _pose = ResolvePose();
        }

        protected override bool OnEvaluate(float deltaTime)
        {
            if (_pose == null)
                return false;

            if (!AreTracked())
            {
                _stableFor = 0f;
                return false;
            }

            _stableFor += deltaTime;
            return _stableFor >= _stableSeconds;
        }

        protected override void OnCleanup()
        {
            _pose = null;
            _stableFor = 0f;
        }

        private bool AreTracked()
        {
            bool left = _pose.IsTracked(XRNodeRole.LeftHand);
            bool right = _pose.IsTracked(XRNodeRole.RightHand);

            switch (_controllers)
            {
                case ControllerSelection.Left: return left;
                case ControllerSelection.Right: return right;
                case ControllerSelection.Either: return left || right;
                default: return left && right;
            }
        }

        public override string Describe() => "track " + _controllers + " controller(s)";
    }

    /// <summary>
    /// Satisfied once the controller has moved far enough from where it was when the step
    /// armed. Teaches "your hands are in the world" without naming a button.
    /// <para>
    /// Measures cumulative path length rather than straight-line displacement, so small
    /// deliberate motions add up and the learner is not forced to reach far.
    /// </para>
    /// </summary>
    [Serializable]
    public class ControllerMovementValidator : TutorialValidatorBase
    {
        [SerializeField] private ControllerSelection _controller = ControllerSelection.Right;

        [Tooltip("Metres of accumulated movement required.")]
        [SerializeField, Min(0.01f)] private float _requiredDistance = 0.25f;

        [Tooltip("Per-frame movement below this is treated as tracking jitter and ignored.")]
        [SerializeField, Min(0f)] private float _noiseThreshold = 0.002f;

        private IXRPoseProvider _pose;
        private Vector3 _lastPosition;
        private bool _hasLast;
        private float _travelled;

        public float Progress => _requiredDistance <= 0f ? 0f : Mathf.Clamp01(_travelled / _requiredDistance);

        protected override void OnPrepare()
        {
            _travelled = 0f;
            _hasLast = false;
            _pose = ResolvePose();

            if (_controller == ControllerSelection.Both || _controller == ControllerSelection.Either)
                Break("ControllerMovementValidator needs a single controller (Left or Right).");
        }

        protected override bool OnEvaluate(float deltaTime)
        {
            if (_pose == null)
                return false;

            XRNodeRole role = _controller == ControllerSelection.Left ? XRNodeRole.LeftHand : XRNodeRole.RightHand;

            Pose current;
            if (!_pose.TryGetPose(role, out current))
            {
                _hasLast = false;
                return false;
            }

            if (!_hasLast)
            {
                _lastPosition = current.position;
                _hasLast = true;
                return false;
            }

            float delta = Vector3.Distance(current.position, _lastPosition);
            _lastPosition = current.position;

            if (delta > _noiseThreshold)
                _travelled += delta;

            return _travelled >= _requiredDistance;
        }

        protected override void OnCleanup()
        {
            _pose = null;
            _hasLast = false;
            _travelled = 0f;
        }

        public override string Describe() =>
            "move " + _controller + " controller " + _requiredDistance.ToString("0.##") + "m";
    }

    /// <summary>
    /// Satisfied when the learner turns their head far enough from the pose recorded at
    /// Prepare. Backs "look left / look right / look behind" without naming a direction
    /// in code: the angle and sign come from the step data.
    /// </summary>
    [Serializable]
    public class HeadRotationValidator : TutorialValidatorBase
    {
        public enum RotationDirection
        {
            AnyDirection = 0,
            Left = 1,
            Right = 2
        }

        [SerializeField] private RotationDirection _direction = RotationDirection.AnyDirection;

        [Tooltip("Degrees of yaw away from the pose captured when the step armed.")]
        [SerializeField, Range(5f, 180f)] private float _requiredYaw = 45f;

        [Tooltip("Continuous seconds the learner must hold that heading.")]
        [SerializeField, Min(0f)] private float _holdSeconds;

        private IXRPoseProvider _pose;
        private float _referenceYaw;
        private bool _hasReference;
        private float _heldFor;

        protected override void OnPrepare()
        {
            _hasReference = false;
            _heldFor = 0f;
            _pose = ResolvePose();
        }

        protected override bool OnEvaluate(float deltaTime)
        {
            if (_pose == null)
                return false;

            Pose head;
            if (!_pose.TryGetPose(XRNodeRole.Head, out head))
                return false;

            float yaw = head.rotation.eulerAngles.y;

            if (!_hasReference)
            {
                _referenceYaw = yaw;
                _hasReference = true;
                return false;
            }

            // Signed shortest angle: positive is a turn to the right.
            float signed = Mathf.DeltaAngle(_referenceYaw, yaw);

            bool satisfiedNow;
            if (_direction == RotationDirection.Left)
                satisfiedNow = signed <= -_requiredYaw;
            else if (_direction == RotationDirection.Right)
                satisfiedNow = signed >= _requiredYaw;
            else
                satisfiedNow = Mathf.Abs(signed) >= _requiredYaw;

            if (!satisfiedNow)
            {
                _heldFor = 0f;
                return false;
            }

            _heldFor += deltaTime;
            return _heldFor >= _holdSeconds;
        }

        protected override void OnCleanup()
        {
            _pose = null;
            _hasReference = false;
            _heldFor = 0f;
        }

        public override string Describe() =>
            "turn head " + _direction + " by " + _requiredYaw.ToString("0") + " degrees";
    }
}
