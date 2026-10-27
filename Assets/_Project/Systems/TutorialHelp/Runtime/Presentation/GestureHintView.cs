using RideSafe.UI;
using System.Collections.Generic;
using RideSafe.Tutorial;
using RideSafe.Tutorial.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.Help
{
    /// <summary>
    /// The Touch Plus of the hand to use, turned to show the gesture's control, which glows and
    /// is pressed a few times, then stays lit. Sits below and beside the gaze, world-locked
    /// with a dead zone so it does not ride on small head movements. Optional short text.
    /// </summary>
    [AddComponentMenu("RideSafe/Tutorial Help/Gesture Hint View")]
    public sealed class GestureHintView : MonoBehaviour, IGestureHintView
    {
        [SerializeField] private ControllerModelVisual _left;
        [SerializeField] private ControllerModelVisual _right;
        [SerializeField] private TMP_Text _caption;

        [Header("Placement (head-relative)")]
        [SerializeField, Min(0.2f)] private float _distance = 0.6f;
        [SerializeField] private Vector2 _offset = new Vector2(0f, -0.3f); // centred below the gaze; Respot moves it aside
        [SerializeField, Range(0f, 45f)] private float _maxYawFromGaze = 15f;
        [SerializeField, Min(0.1f)] private float _scale = 2.2f;
        [SerializeField, Range(0f, 90f)] private float _refollowAngle = 25f;
        [SerializeField, Min(0f)] private float _refollowDistance = 0.3f;
        [SerializeField, Min(0.01f)] private float _followSeconds = 0.5f;
        [Tooltip("Metres to the side when the centre spot would cover a module panel.")]
        [SerializeField, Min(0f)] private float _sideOffset = 0.32f;
        [Tooltip("Seconds between checks for a freer spot.")]
        [SerializeField, Min(0.1f)] private float _respotSeconds = 0.5f;
        [Tooltip("Size of the hint card (its backdrop) before the view's scale, in metres.")]
        [SerializeField] private Vector2 _cardSize = new Vector2(0.2f, 0.21f);
        [Tooltip("How far the controller moves down in the card when there is a caption above it, before the view's scale.")]
        [SerializeField, Min(0f)] private float _captionRoom = 0.03f;

        [Header("Animation")]
        [SerializeField] private Color _glowColor = new Color32(0xA9, 0xE2, 0xD4, 0xFF);
        [SerializeField, Min(0f)] private float _leadSeconds = 0.4f;
        [SerializeField, Min(0f)] private float _pauseSeconds = 0.6f;
        [SerializeField, Min(0.1f)] private float _pulseSeconds = 1.2f;
        [SerializeField, Range(0f, 1f)] private float _minGlow = 0.45f;
        [SerializeField, Min(0)] private int _maxDemonstrations = 3;

        private ControllerButtonVisual _button;
        private TutorialGestureSO _failedGesture;
        private XRNodeRole _failedHand;
        private bool _failed;
        private TutorialGestureSO _gesture;
        private string _text;
        private Vector3? _focus;
        private bool _placed;
        private Pose _placedViewer, _followFrom, _followTo;
        private float _followTime = float.MaxValue;
        private float _time;
        private MaterialPropertyBlock _block;
        private float _lateral;
        private float _sinceRespot;
        private readonly List<Canvas> _canvases = new List<Canvas>();
        private readonly Vector3[] _corners = new Vector3[4];
        private readonly List<Rect> _panelRects = new List<Rect>();
        private readonly Rect[] _spots = new Rect[3];
        private readonly float[] _laterals = new float[3];
        private float _panelsListedAt = float.NegativeInfinity;

        public bool IsVisible => gameObject.activeSelf && _button != null;
        public XRNodeRole ShownHand { get; private set; } = XRNodeRole.RightHand;
        public string CaptionText => _caption != null && _caption.gameObject.activeSelf ? _caption.text : null;

        internal void Configure(ControllerModelVisual left, ControllerModelVisual right, TMP_Text caption)
        {
            _left = left;
            _right = right;
            _caption = caption;
        }

        public void Show(TutorialGestureSO gesture, XRNodeRole hand, string text, Vector3? focus)
        {
            _focus = focus;
            if (_button != null && gesture == _gesture && hand == ShownHand && text == _text)
                return;

            ControllerModelVisual model = hand == XRNodeRole.LeftHand ? _left : _right;
            if (gesture == null || model == null || !model.TryGetButton(gesture.Control, out ControllerButtonVisual button))
            {
                if (!_failed || _failedGesture != gesture || _failedHand != hand) // once, not every frame
                    Debug.LogWarning("[TutorialHelp] The " + hand + " controller has no " +
                                     (gesture != null ? gesture.Control.ToString() : "gesture") + " to show.", this);
                _failed = true;
                _failedGesture = gesture;
                _failedHand = hand;
                Hide();
                return;
            }
            _failed = false;

            EndButton();
            if (_left != null) _left.gameObject.SetActive(model == _left);
            if (_right != null) _right.gameObject.SetActive(model == _right);
            Pose pose = model.PoseFor(gesture.Control);
            // The caption sits at the top of the card; the controller makes room under it.
            Vector3 room = string.IsNullOrEmpty(text) ? Vector3.zero : Vector3.down * _captionRoom;
            model.transform.SetLocalPositionAndRotation(pose.position + room, pose.rotation);

            _button = button;
            _button.Begin();
            _gesture = gesture;
            ShownHand = hand;
            _text = text;
            if (_caption != null)
            {
                _caption.text = text ?? string.Empty;
                _caption.gameObject.SetActive(!string.IsNullOrEmpty(text));
            }
            _time = 0f;
            if (!gameObject.activeSelf)
            {
                _placed = false;
                gameObject.SetActive(true);
            }
            Tick(0f);
        }

        public void Hide()
        {
            EndButton();
            _gesture = null;
            _text = null;
            _placed = false;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                Place(new Pose(camera.transform.position, camera.transform.rotation));
                _sinceRespot += Time.deltaTime;
                if (_sinceRespot >= _respotSeconds)
                    Respot(camera);
            }
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// Moves the hint to the spot below the gaze (centre, dominant side, other side) that
        /// covers the least of the module's panels, so the panel and the controller can both be
        /// seen. Glides there if it was already shown.
        /// </summary>
        internal void Respot(Camera camera)
        {
            _sinceRespot = 0f;
            if (!IsVisible || camera == null)
                return;
            Pose viewer = new Pose(camera.transform.position, camera.transform.rotation);
            float dominant = ShownHand == XRNodeRole.LeftHand ? -_sideOffset : _sideOffset;
            _laterals[0] = 0f;
            _laterals[1] = dominant;
            _laterals[2] = -dominant;
            for (int i = 0; i < 3; i++)
                _spots[i] = CardRect(camera, Compute(viewer, _laterals[i]));
            float chosen = _laterals[HintSpot.Choose(_spots, PanelRects(camera))];
            if (Mathf.Approximately(chosen, _lateral) && _placed)
                return;
            _lateral = chosen;
            if (!_placed)
            {
                Place(viewer);
                return;
            }
            _followFrom = new Pose(transform.position, transform.rotation);
            _followTo = Compute(viewer, _lateral);
            _followTime = 0f;
            _placedViewer = viewer;
        }

        private Rect CardRect(Camera camera, Pose pose)
        {
            Vector2 half = _cardSize * (_scale * 0.5f);
            Vector3 right = pose.rotation * Vector3.right * half.x;
            Vector3 up = pose.rotation * Vector3.up * half.y;
            _corners[0] = pose.position - right - up;
            _corners[1] = pose.position - right + up;
            _corners[2] = pose.position + right + up;
            _corners[3] = pose.position + right - up;
            return Viewport(camera, out _);
        }

        /// <summary>Viewport rects of the module's visible panels (images on world-space canvases), not the tutorial's own.</summary>
        private List<Rect> PanelRects(Camera camera)
        {
            // A handful of canvases, listed every 2 s; their graphics come from uGUI's own
            // registry, so there is no scene-wide search for images and no array per call.
            if (Time.realtimeSinceStartup - _panelsListedAt > 2f || _canvases.Count == 0)
            {
                _canvases.Clear();
                _canvases.AddRange(FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
                _panelsListedAt = Time.realtimeSinceStartup;
            }
            _panelRects.Clear();
            // Lo propio es el objeto de primer nivel de la pista, no la raíz: en una carpeta del
            // Hierarchy, la raíz es la carpeta y se llevaría los paneles de sus vecinos.
            Transform own = SceneFolder.TopLevel(transform);
            for (int c = 0; c < _canvases.Count; c++)
            {
                Canvas canvas = _canvases[c];
                if (canvas == null || !canvas.isActiveAndEnabled || canvas.rootCanvas.renderMode != RenderMode.WorldSpace ||
                    canvas.transform.IsChildOf(own))
                    continue;
                IList<Graphic> graphics = GraphicRegistry.GetGraphicsForCanvas(canvas);
                for (int g = 0; g < graphics.Count; g++)
                {
                    Graphic graphic = graphics[g];
                    if (!(graphic is Image || graphic is RawImage) || graphic == null || !graphic.isActiveAndEnabled ||
                        graphic.color.a < 0.05f)
                        continue;
                    graphic.rectTransform.GetWorldCorners(_corners);
                    Rect rect = Viewport(camera, out bool behind);
                    if (!behind && rect.width * rect.height > 0.0005f)
                        _panelRects.Add(rect);
                }
            }
            return _panelRects;
        }

        /// <summary>Viewport rect of the four points in _corners; behind = any of them is behind the camera.</summary>
        private Rect Viewport(Camera camera, out bool behind)
        {
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            behind = false;
            for (int i = 0; i < 4; i++)
            {
                Vector3 v = camera.WorldToViewportPoint(_corners[i]);
                behind |= v.z <= 0f;
                min = Vector2.Min(min, v);
                max = Vector2.Max(max, v);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private void OnDisable() => EndButton();

        private void EndButton()
        {
            if (_button != null)
                _button.End();
            _button = null;
        }

        public void Place(Pose viewer)
        {
            if (_placed)
            {
                float turned = Vector3.Angle(Flat(_placedViewer.rotation * Vector3.forward), Flat(viewer.rotation * Vector3.forward));
                float moved = Vector3.Distance(_placedViewer.position, viewer.position);
                if (turned < _refollowAngle && moved < _refollowDistance)
                    return;
                _followFrom = new Pose(transform.position, transform.rotation);
                _followTo = Compute(viewer, _lateral);
                _followTime = 0f;
                _placedViewer = viewer;
                return;
            }
            Pose pose = Compute(viewer, _lateral);
            transform.SetPositionAndRotation(pose.position, pose.rotation);
            transform.localScale = Vector3.one * _scale;
            _placedViewer = viewer;
            _followTime = float.MaxValue;
            _placed = true;
        }

        private Pose Compute(Pose viewer, float lateral)
        {
            Vector3 gaze = Flat(viewer.rotation * Vector3.forward);
            Vector3 focus = _focus ?? viewer.position + gaze;
            Vector3 toFocus = Vector3.ProjectOnPlane(focus - viewer.position, Vector3.up);
            float yaw = toFocus.sqrMagnitude > 1e-6f ? Vector3.SignedAngle(gaze, toFocus, Vector3.up) : 0f;
            Vector3 direction = Quaternion.AngleAxis(Mathf.Clamp(yaw, -_maxYawFromGaze, _maxYawFromGaze), Vector3.up) * gaze;
            Vector3 right = Vector3.Cross(Vector3.up, direction);
            float side = Mathf.Abs(yaw) > 2f ? -Mathf.Sign(yaw) : 1f; // toward the view's centre
            Vector3 position = viewer.position + direction * _distance + right * (_offset.x * side + lateral) + Vector3.up * _offset.y;
            Vector3 toViewer = viewer.position - position;
            return new Pose(position, Quaternion.LookRotation(toViewer.sqrMagnitude > 1e-6f ? toViewer : -direction, Vector3.up));
        }

        private static Vector3 Flat(Vector3 direction)
        {
            Vector3 flat = Vector3.ProjectOnPlane(direction, Vector3.up);
            return flat.sqrMagnitude > 1e-6f ? flat.normalized : Vector3.forward;
        }

        public void Tick(float deltaTime)
        {
            if (_button == null)
                return;
            if (_followTime < _followSeconds)
            {
                _followTime += deltaTime;
                float f = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_followTime / _followSeconds));
                transform.SetPositionAndRotation(Vector3.Lerp(_followFrom.position, _followTo.position, f),
                                                 Quaternion.Slerp(_followFrom.rotation, _followTo.rotation, f));
            }
            if (_block == null)
                _block = new MaterialPropertyBlock(); // not allowed in a field initializer

            _time += deltaTime;
            float cycle = _leadSeconds + 2f * _button.PressSeconds + _pauseSeconds;
            int played = cycle > 0f ? Mathf.FloorToInt(_time / cycle) : 0;
            float press = played < _maxDemonstrations ? PressAt(_time, _leadSeconds, _button.PressSeconds, _pauseSeconds) : 0f;
            float glow = Mathf.Lerp(_minGlow, 1f, 0.5f + 0.5f * Mathf.Sin(_time / _pulseSeconds * Mathf.PI * 2f));
            _button.Apply(press, glow, _glowColor, _block);
        }

        /// <summary>One demonstration: glow alone for lead, ease down over press, back up over press, rest for pause.</summary>
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
    }
}
