using UnityEngine;

namespace RideSafe.UI
{
    /// <summary>
    /// A dark bubble around the learner's head: everything farther than its radius is dimmed, what
    /// is closer stays in full light. Module 1 switches it on while a vehicle point is open, so only
    /// the popup, the pedestal and its part stand out (PO, 2026-10-23).
    /// <para>
    /// <b>Why a bubble and not <see cref="TheaterMode"/>.</b> The theater is a black panel drawn by
    /// the overlay UI camera over every 3D object, the pedestal included, and that camera is stacked
    /// on the PC camera only. The bubble is plain geometry seen from inside: each eye sees it right,
    /// it needs no overlay camera and no depth texture, and it costs one transparent layer. Blur was
    /// ruled out: on Quest it needs the opaque texture and a pass per eye, and a blurred surround
    /// strains the eye in VR.
    /// </para>
    /// <para>
    /// The material is URP Unlit, transparent, back faces only, at queue 2950: after the opaque
    /// world, before the UI canvases, so the popup inside the bubble is never darkened. The pedestal
    /// and its part are opaque and closer than the wall, so the wall fails the depth test there.
    /// </para>
    /// <para>
    /// It never switches at once: the alpha fades in and out (PO), on a smoothstep curve, from
    /// wherever it is when the direction changes.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/UI/Focus Dome")]
    [DisallowMultipleComponent]
    public class FocusDome : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("The inside-out sphere. Unity's sphere mesh, 1 m across.")]
        [SerializeField] private MeshRenderer _renderer;

        [Tooltip("What it follows. Empty: the main camera.")]
        [SerializeField] private Transform _head;

        [Tooltip("Metres from the head to the wall. Between the pedestal and the vehicle: measured in the v5 scene.")]
        [SerializeField, Min(.1f)] private float _radius = 1.6f;

        [Tooltip("Alpha when fully on. The same as the video's theater.")]
        [SerializeField, Range(0f, 1f)] private float _dimAmount = .88f;

        [Tooltip("Seconds of the fade. Switching light at once is uncomfortable in VR.")]
        [SerializeField, Min(0f)] private float _fadeSeconds = .45f;

        private MaterialPropertyBlock _block;
        private float _alpha;
        private float _from;
        private float _target;
        private float _elapsed;

        /// <summary>Is it on, or fading on?</summary>
        public bool IsOn { get; private set; }

        /// <summary>The alpha drawn now. For tests to assert on.</summary>
        public float CurrentAlpha => _alpha;

        public float Radius => _radius;

        private void Awake() => Apply(0f);

        /// <summary>On or off, with a fade from the current alpha.</summary>
        public void SetOn(bool on)
        {
            IsOn = on;
            _from = _alpha;
            _target = on ? _dimAmount : 0f;
            _elapsed = 0f;
            if (_fadeSeconds <= 0f)
                Apply(_target);
            else
                Apply(_alpha); // visible from the first frame of a fade-in
        }

        /// <summary>No fade. For building the scene and for tests.</summary>
        public void SetOnImmediate(bool on)
        {
            IsOn = on;
            _target = on ? _dimAmount : 0f;
            _from = _target;
            _elapsed = _fadeSeconds;
            Apply(_target);
        }

        private void LateUpdate() => Tick(Time.unscaledDeltaTime);

        /// <summary>Follows the head and advances the fade. Driven by LateUpdate; public for tests.</summary>
        public void Tick(float deltaTime)
        {
            Transform head = _head != null ? _head : Camera.main != null ? Camera.main.transform : null;
            if (head != null)
                transform.position = head.position;
            transform.localScale = Vector3.one * (2f * _radius);

            if (Mathf.Approximately(_alpha, _target) && _elapsed >= _fadeSeconds)
                return;
            _elapsed += deltaTime;
            float t = _fadeSeconds <= 0f ? 1f : Mathf.Clamp01(_elapsed / _fadeSeconds);
            Apply(Mathf.Lerp(_from, _target, Mathf.SmoothStep(0f, 1f, t)));
        }

        private void Apply(float alpha)
        {
            _alpha = alpha;
            if (_renderer == null)
                return;
            _block ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, new Color(0f, 0f, 0f, alpha));
            _renderer.SetPropertyBlock(_block);
            // Hidden only once transparent: a fade-out stays visible until it reaches zero.
            bool visible = alpha > .001f || IsOn;
            if (_renderer.enabled != visible)
                _renderer.enabled = visible;
        }

        /// <summary>For tests and the scene builder.</summary>
        public void ConfigureForTests(MeshRenderer renderer, Transform head, float radius, float dimAmount = .88f, float fadeSeconds = .45f)
        {
            _renderer = renderer;
            _head = head;
            _radius = radius;
            _dimAmount = dimAmount;
            _fadeSeconds = fadeSeconds;
            IsOn = false;
            _from = _target = 0f;
            _elapsed = fadeSeconds;
            Apply(0f);
        }
    }
}
