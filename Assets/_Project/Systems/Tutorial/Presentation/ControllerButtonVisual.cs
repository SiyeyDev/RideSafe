using UnityEngine;

namespace RideSafe.Tutorial.Presentation
{
    /// <summary>
    /// One pressable part of a controller model (a separate mesh in the Touch Plus FBX):
    /// which control it is, how it moves when pressed and which renderers glow.
    /// <para>
    /// Holds no timing of its own. <see cref="ControllerHint3DView"/> drives the active
    /// button through <see cref="Apply"/>, so a whole controller costs one Update.
    /// Highlight goes through a <see cref="MaterialPropertyBlock"/>: no material instance is
    /// ever created, and the shared material asset is never written. The part's base colour
    /// is tinted and, when it has one, its glow shell lights up.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("RideSafe/Tutorial/Controller Button Visual")]
    public class ControllerButtonVisual : MonoBehaviour
    {
        public enum PressMotion
        {
            Translate = 0,
            Rotate = 1
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColorId = Shader.PropertyToID("_Color");

        [SerializeField] private ControllerControl _control = ControllerControl.Trigger;

        [Tooltip("Part that moves. Empty uses this transform.")]
        [SerializeField] private Transform _target;

        [Tooltip("Renderers that glow. Empty uses the renderers on this object.")]
        [SerializeField] private Renderer[] _renderers = new Renderer[0];

        [Header("Press")]
        [SerializeField] private PressMotion _motion = PressMotion.Translate;

        [Tooltip("Local direction of travel (Translate) or local rotation axis (Rotate).")]
        [SerializeField] private Vector3 _pressDirection = Vector3.down;

        [Tooltip("Metres of travel (Translate).")]
        [SerializeField, Min(0f)] private float _pressDistance = 0.003f;

        [Tooltip("Degrees around the axis (Rotate).")]
        [SerializeField] private float _pressAngle = 12f;

        [Tooltip("Seconds to go down, and again to come back up.")]
        [SerializeField, Min(0.05f)] private float _pressSeconds = 0.25f;

        [Header("Highlight")]
        [Tooltip("Optional glow shell: a slightly larger copy of the part with an unlit material, shown only " +
                 "while highlighted. Reads as lit on dark and light plastic alike and does not depend on lighting.")]
        [SerializeField] private Renderer _glow;

        [Tooltip("0 = no highlight, 1 = full highlight at the pulse peak.")]
        [SerializeField, Range(0f, 1f)] private float _highlightIntensity = 0.85f;

        [Tooltip("Size of the part at full highlight, so a small or recessed button stands out from the body.")]
        [SerializeField, Min(1f)] private float _highlightScale = 1.12f;

        private bool _captured;
        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private Vector3 _restScale;
        private Color[] _restColors;
        private int[] _colorIds;

        public ControllerControl Control => _control;
        public float PressSeconds => _pressSeconds;
        private Transform Target => _target != null ? _target : transform;

        /// <summary>Remembers the rest pose and colours. Call before the first <see cref="Apply"/>.</summary>
        public void Begin()
        {
            if (_renderers == null || _renderers.Length == 0)
                _renderers = GetComponents<Renderer>();

            Transform target = Target;
            _restPosition = target.localPosition;
            _restRotation = target.localRotation;
            _restScale = target.localScale;

            _restColors = new Color[_renderers.Length];
            _colorIds = new int[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                Material material = _renderers[i] != null ? _renderers[i].sharedMaterial : null;
                int id = material != null && !material.HasProperty(BaseColorId) && material.HasProperty(LegacyColorId)
                    ? LegacyColorId
                    : BaseColorId;
                _colorIds[i] = id;
                _restColors[i] = material != null && material.HasProperty(id) ? material.GetColor(id) : Color.white;
            }
            if (_glow != null)
                _glow.enabled = false;
            _captured = true;
        }

        /// <summary>Poses the part: <paramref name="press"/> and <paramref name="glow"/> are 0..1.</summary>
        public void Apply(float press, float glow, Color highlightColor, MaterialPropertyBlock block)
        {
            if (!_captured)
                Begin();

            press = Mathf.Clamp01(press);
            Transform target = Target;
            if (_motion == PressMotion.Translate)
            {
                Vector3 direction = _pressDirection.sqrMagnitude > 0f ? _pressDirection.normalized : Vector3.zero;
                target.localPosition = _restPosition + direction * (_pressDistance * press);
            }
            else
            {
                target.localRotation = _restRotation * Quaternion.AngleAxis(_pressAngle * press, _pressDirection);
            }

            float tint = Mathf.Clamp01(glow) * _highlightIntensity;
            if (_glow != null)
            {
                _glow.enabled = glow > 0.01f;
                block.Clear();
                block.SetColor(BaseColorId, Color.Lerp(highlightColor * 0.75f, Color.Lerp(highlightColor, Color.white, 0.35f), Mathf.Clamp01(glow)));
                _glow.SetPropertyBlock(block);
            }
            target.localScale = _restScale * Mathf.Lerp(1f, _highlightScale, Mathf.Clamp01(glow));
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer renderer = _renderers[i];
                if (renderer == null)
                    continue;
                block.Clear();
                block.SetColor(_colorIds[i], Color.Lerp(_restColors[i], highlightColor, tint));
                renderer.SetPropertyBlock(block);
            }
        }

        /// <summary>Back to the rest pose with no highlight.</summary>
        public void End()
        {
            if (!_captured)
                return;

            Transform target = Target;
            target.localPosition = _restPosition;
            target.localRotation = _restRotation;
            target.localScale = _restScale;
            if (_glow != null)
            {
                _glow.enabled = false;
                _glow.SetPropertyBlock(null);
            }
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                    _renderers[i].SetPropertyBlock(null);
            }
            _captured = false;
        }

        private void Reset()
        {
            _renderers = GetComponents<Renderer>();
        }
    }
}
