using UnityEngine;

namespace RideSafe.Tutorial.Presentation
{
    /// <summary>
    /// 3D Meta Quest Touch Plus hint: a large didactic controller held in a comfortable spot
    /// in front of the learner, turned so the explained control faces them, with that control
    /// glowing and demonstrating its press.
    /// <para>
    /// Demonstration cycle: highlight, press, release, short pause; at most
    /// <c>_maxDemonstrations</c> times, then the controller stays with the control lit but no
    /// longer pressing, until the learner acts. A temporary illustration only: never the
    /// learner's hand, never a pointer origin, and it has no way to complete a step.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/Tutorial/Controller Hint 3D View")]
    public class ControllerHint3DView : MonoBehaviour, IControllerHintView, IHeadRelativeHintView
    {
        [SerializeField] private ControllerModelVisual _left;
        [SerializeField] private ControllerModelVisual _right;

        [Header("Placement (head-relative)")]
        [Tooltip("Horizontal metres in front of the head.")]
        [SerializeField, Min(0.2f)] private float _distance = 0.6f;
        [Tooltip("Metres to the side (x, toward the centre of the view when the target is off to one side) " +
                 "and up (y, - = below eye level).")]
        [SerializeField] private Vector2 _offset = new Vector2(0.2f, -0.22f);
        [Tooltip("The hint leans toward the target but never further than this from the gaze, in degrees.")]
        [SerializeField, Range(0f, 45f)] private float _maxYawFromGaze = 15f;
        [Tooltip("Didactic size: legibility matters more than real size.")]
        [SerializeField, Min(0.1f)] private float _scale = 2.2f;
        [Tooltip("Extra rotation of the whole hint after facing the viewer.")]
        [SerializeField] private Vector3 _baseEuler = Vector3.zero;
        [Header("Following (world-locked with a dead zone)")]
        [Tooltip("Head turn, in degrees, the hint ignores. Beyond it the hint moves to the new view.")]
        [SerializeField, Range(0f, 90f)] private float _refollowAngle = 25f;
        [Tooltip("Head movement, in metres, the hint ignores. Beyond it the hint moves to the new view.")]
        [SerializeField, Min(0f)] private float _refollowDistance = 0.3f;
        [Tooltip("Seconds to glide to a new spot after a large head movement.")]
        [SerializeField, Min(0.01f)] private float _followSeconds = 0.5f;

        [Tooltip("Seconds to turn the model to a new control's pose.")]
        [SerializeField, Min(0.01f)] private float _poseSeconds = 0.35f;

        [Header("Highlight")]
        [SerializeField] private Color _highlightColor = new Color(0.45f, 0.82f, 1f);
        [Tooltip("Seconds per glow pulse.")]
        [SerializeField, Min(0.1f)] private float _pulseSeconds = 1.2f;
        [Tooltip("Glow never drops below this while the hint is shown, so the part stays marked.")]
        [SerializeField, Range(0f, 1f)] private float _minGlow = 0.45f;

        [Header("Press demonstration")]
        [Tooltip("Highlight alone before each press.")]
        [SerializeField, Min(0f)] private float _leadSeconds = 0.4f;
        [Tooltip("Rest after each press, before the next one.")]
        [SerializeField, Min(0f)] private float _pauseSeconds = 0.6f;
        [Tooltip("Automatic presses before the controller just stays, lit, waiting.")]
        [SerializeField, Min(0)] private int _maxDemonstrations = 3;
        [Tooltip("Pressed pose shown after the learner acts, before hiding.")]
        [SerializeField, Min(0.05f)] private float _releaseSeconds = 0.45f;

        private MaterialPropertyBlock _block;
        private ControllerModelVisual _model;
        private ControllerButtonVisual _active;
        private float _time;
        private float _releaseTime = -1f;
        private bool _placed;
        private Pose _placedViewer;
        private Pose _followFrom;
        private Pose _followTo;
        private float _followTime = float.MaxValue;
        private Pose _poseFrom;
        private Pose _poseTo;
        private float _poseTime;

        public bool IsVisible => _active != null && gameObject.activeSelf;
        public bool IsReleasing => IsVisible && _releaseTime >= 0f;

        /// <summary>Automatic presses shown since the last <see cref="Show"/>, capped at the maximum.</summary>
        public int DemonstrationsPlayed { get; private set; }

        public bool Show(PhysicalBinding binding)
        {
            ControllerModelVisual model = ModelFor(binding);
            ControllerButtonVisual button;
            if (!binding.IsValid || model == null || !model.TryGetButton(binding.Control, out button))
                return false;

            EndActive();
            SetModelVisible(_left, model == _left);
            SetModelVisible(_right, model == _right);

            _model = model;
            _active = button;
            _active.Begin();
            _poseFrom = new Pose(model.transform.localPosition, model.transform.localRotation);
            _poseTo = model.PoseFor(binding.Control);
            _poseTime = 0f;
            _time = 0f;
            _releaseTime = -1f;
            DemonstrationsPlayed = 0;
            _placed = false;
            gameObject.SetActive(true);
            Tick(0f);
            return true;
        }

        public void Release()
        {
            if (IsVisible && _releaseTime < 0f)
                _releaseTime = 0f;
        }

        public void Hide()
        {
            EndActive();
            _releaseTime = -1f;
            _placed = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// World-locked: the first call after <see cref="Show"/> places the hint; later calls move
        /// it only when the head has turned or moved beyond the dead zone, gliding there, so it
        /// never rides on small head movements.
        /// </summary>
        public void Place(Pose viewer, Vector3 focus)
        {
            if (_placed)
            {
                float turned = Vector3.Angle(Flat(_placedViewer.rotation * Vector3.forward), Flat(viewer.rotation * Vector3.forward));
                float moved = Vector3.Distance(_placedViewer.position, viewer.position);
                if (turned < _refollowAngle && moved < _refollowDistance)
                    return;
                _followFrom = new Pose(transform.position, transform.rotation);
                _followTo = Compute(viewer, focus);
                _followTime = 0f;
                _placedViewer = viewer;
                return;
            }

            Pose pose = Compute(viewer, focus);
            transform.SetPositionAndRotation(pose.position, pose.rotation);
            transform.localScale = Vector3.one * _scale;
            _placedViewer = viewer;
            _followTime = float.MaxValue;
            _placed = true;
        }

        private Pose Compute(Pose viewer, Vector3 focus)
        {
            Vector3 gaze = Vector3.ProjectOnPlane(viewer.rotation * Vector3.forward, Vector3.up);
            if (gaze.sqrMagnitude < 1e-6f)
                gaze = Vector3.forward;
            gaze.Normalize();

            Vector3 toFocus = Vector3.ProjectOnPlane(focus - viewer.position, Vector3.up);
            float yaw = toFocus.sqrMagnitude > 1e-6f ? Vector3.SignedAngle(gaze, toFocus, Vector3.up) : 0f;
            Vector3 direction = Quaternion.AngleAxis(Mathf.Clamp(yaw, -_maxYawFromGaze, _maxYawFromGaze), Vector3.up) * gaze;
            Vector3 right = Vector3.Cross(Vector3.up, direction);
            // Beside the target on the side of the view's centre, so it never drifts off-screen.
            float side = Mathf.Abs(yaw) > 2f ? -Mathf.Sign(yaw) : 1f;

            Vector3 position = viewer.position + direction * _distance + right * (_offset.x * side) + Vector3.up * _offset.y;
            Vector3 toViewer = viewer.position - position;
            return new Pose(position,
                Quaternion.LookRotation(toViewer.sqrMagnitude > 1e-6f ? toViewer : -direction, Vector3.up) *
                Quaternion.Euler(_baseEuler));
        }

        private static Vector3 Flat(Vector3 direction)
        {
            Vector3 flat = Vector3.ProjectOnPlane(direction, Vector3.up);
            return flat.sqrMagnitude > 1e-6f ? flat : Vector3.forward;
        }

        private void TickFollow(float deltaTime)
        {
            if (_followTime >= _followSeconds)
                return;
            _followTime += deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_followTime / _followSeconds));
            transform.SetPositionAndRotation(Vector3.Lerp(_followFrom.position, _followTo.position, t),
                                             Quaternion.Slerp(_followFrom.rotation, _followTo.rotation, t));
        }

        /// <summary>Advances the animation. Driven by Update; public so it can be stepped deterministically.</summary>
        public void Tick(float deltaTime)
        {
            if (_active == null)
                return;
            TickFollow(deltaTime);
            if (_block == null)
                _block = new MaterialPropertyBlock(); // Not allowed in a field initializer.

            TickPose(deltaTime);

            if (_releaseTime >= 0f)
            {
                _releaseTime += deltaTime;
                float t = Mathf.Clamp01(_releaseTime / _releaseSeconds);
                // Snap to pressed, then ease back to rest while the glow fades out.
                float release = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, t));
                _active.Apply(release, 1f - t, _highlightColor, _block);
                if (t >= 1f)
                    Hide();
                return;
            }

            _time += deltaTime;
            float cycle = _leadSeconds + 2f * _active.PressSeconds + _pauseSeconds;
            int played = cycle > 0f ? Mathf.FloorToInt(_time / cycle) : 0;
            bool demonstrating = played < _maxDemonstrations;
            DemonstrationsPlayed = Mathf.Min(_maxDemonstrations, played + (InPress(_time % cycle, _active.PressSeconds) ? 1 : 0));

            float press = demonstrating ? PressAt(_time, _leadSeconds, _active.PressSeconds, _pauseSeconds) : 0f;
            float glow = Mathf.Lerp(_minGlow, 1f, 0.5f + 0.5f * Mathf.Sin(_time / _pulseSeconds * Mathf.PI * 2f));
            _active.Apply(press, glow, _highlightColor, _block);
        }

        /// <summary>
        /// One demonstration: highlight alone for <paramref name="lead"/>, ease down over
        /// <paramref name="press"/>, ease back up over <paramref name="press"/>, rest for <paramref name="pause"/>.
        /// </summary>
        public static float PressAt(float time, float lead, float press, float pause)
        {
            float cycle = lead + 2f * press + pause;
            float t = cycle > 0f ? time % cycle : 0f;
            if (t < lead)
                return 0f;
            t -= lead;
            if (t < press)
                return Mathf.SmoothStep(0f, 1f, t / press);
            t -= press;
            return t < press ? Mathf.SmoothStep(1f, 0f, t / press) : 0f;
        }

        protected virtual void Update() => Tick(Time.deltaTime);

        protected virtual void OnDisable() => EndActive();

        private bool InPress(float timeInCycle, float press) => timeInCycle >= _leadSeconds && timeInCycle < _leadSeconds + 2f * press;

        private void TickPose(float deltaTime)
        {
            if (_model == null || _poseTime >= _poseSeconds)
                return;
            _poseTime += deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_poseTime / _poseSeconds));
            _model.transform.localPosition = Vector3.Lerp(_poseFrom.position, _poseTo.position, t);
            _model.transform.localRotation = Quaternion.Slerp(_poseFrom.rotation, _poseTo.rotation, t);
        }

        private ControllerModelVisual ModelFor(PhysicalBinding binding)
        {
            if (binding.Hand == XRNodeRole.LeftHand)
                return _left;
            if (binding.Hand == XRNodeRole.RightHand)
                return _right;
            return null;
        }

        private void EndActive()
        {
            if (_active != null)
                _active.End();
            _active = null;
        }

        private static void SetModelVisible(ControllerModelVisual model, bool visible)
        {
            if (model != null)
                model.gameObject.SetActive(visible);
        }
    }
}
