using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace RideSafe.Help
{
    /// <summary>
    /// Canvas controls pulse softly (white, slightly darker, white) on their CanvasRenderer,
    /// so the module's own colour and sprite states are untouched and restored exactly. Objects
    /// get a mint inverted-hull halo whose opacity pulses in step. All targets pulse together.
    /// </summary>
    [AddComponentMenu("RideSafe/Tutorial Help/Target Highlighter")]
    public sealed class TargetHighlighter : MonoBehaviour, ITargetHighlighter
    {
        public const string HaloName = "TutorialHalo";
        private static readonly int ColorId = Shader.PropertyToID("_OutlineColor");

        [SerializeField] private Material _haloMaterial;
        [SerializeField] private Color _haloColor = new Color32(0xA9, 0xE2, 0xD4, 0xFF);
        [Tooltip("Darkest tone of the pulse on canvas controls (1 = no change).")]
        [SerializeField, Range(0.5f, 1f)] private float _dimTone = 0.85f;
        [SerializeField, Min(0.2f)] private float _periodSeconds = 1.6f;

        private readonly Dictionary<Transform, Graphic> _pulsing = new Dictionary<Transform, Graphic>();
        private readonly Dictionary<Graphic, Color> _original = new Dictionary<Graphic, Color>();
        private readonly Dictionary<Graphic, Color> _written = new Dictionary<Graphic, Color>();
        private readonly Dictionary<Transform, List<MeshRenderer>> _halos = new Dictionary<Transform, List<MeshRenderer>>();
        private readonly HashSet<Transform> _wanted = new HashSet<Transform>();
        private readonly List<Transform> _drop = new List<Transform>();
        private MaterialPropertyBlock _block;
        private float _time;

        public int HighlightedCount => _pulsing.Count + _halos.Count;

        internal void Configure(Material halo) => _haloMaterial = halo;

        public void Highlight(IReadOnlyList<HelpTarget> targets)
        {
            if (HighlightedCount == 0)
                _time = 0f; // a new highlight starts from the control's own colour

            _wanted.Clear();
            foreach (HelpTarget target in targets)
            {
                if (target.Transform == null)
                    continue;
                _wanted.Add(target.Transform);
                if (target.Kind == InteractionKind.Interface)
                {
                    if (target.Graphic != null && !_pulsing.ContainsKey(target.Transform))
                    {
                        _pulsing[target.Transform] = target.Graphic;
                        Color own = target.Graphic.canvasRenderer.GetColor();
                        _original[target.Graphic] = own;
                        _written[target.Graphic] = own;
                    }
                }
                else if (!_halos.ContainsKey(target.Transform))
                {
                    _halos[target.Transform] = CreateHalos(target.Transform);
                }
            }

            _drop.Clear();
            foreach (Transform t in _pulsing.Keys)
                if (!_wanted.Contains(t))
                    _drop.Add(t);
            foreach (Transform t in _halos.Keys)
                if (!_wanted.Contains(t))
                    _drop.Add(t);
            foreach (Transform t in _drop)
                Release(t);
        }

        public void Clear()
        {
            _drop.Clear();
            _drop.AddRange(_pulsing.Keys);
            _drop.AddRange(_halos.Keys);
            foreach (Transform t in _drop)
                Release(t);
        }

        public void Tick(float deltaTime)
        {
            if (HighlightedCount == 0)
                return;
            _time += deltaTime;
            float k = 0.5f - 0.5f * Mathf.Cos(_time / _periodSeconds * Mathf.PI * 2f);
            float tone = Mathf.Lerp(1f, _dimTone, k);
            Color dim = new Color(tone, tone, tone, 1f);
            foreach (KeyValuePair<Transform, Graphic> pair in _pulsing)
            {
                Graphic graphic = pair.Value;
                if (graphic == null)
                    continue;
                // A ColorTint control tweens its own colour (hover, press): follow it, so the
                // restore puts back what the module set, not a colour captured before.
                Color current = graphic.canvasRenderer.GetColor();
                if (current != _written[graphic])
                    _original[graphic] = current;
                Color pulsed = _original[graphic] * dim;
                graphic.canvasRenderer.SetColor(pulsed);
                _written[graphic] = pulsed;
            }

            if (_block == null)
                _block = new MaterialPropertyBlock(); // not allowed in a field initializer
            Color halo = _haloColor;
            halo.a = Mathf.Lerp(0.9f, 0.45f, k);
            foreach (List<MeshRenderer> renderers in _halos.Values)
            {
                foreach (MeshRenderer renderer in renderers)
                {
                    if (renderer == null)
                        continue;
                    _block.Clear();
                    _block.SetColor(ColorId, halo);
                    renderer.SetPropertyBlock(_block);
                }
            }
        }

        private void Update() => Tick(Time.deltaTime);
        private void OnDisable() => Clear();

        private void Release(Transform target)
        {
            if (_pulsing.TryGetValue(target, out Graphic graphic))
            {
                if (graphic != null && _original.TryGetValue(graphic, out Color original))
                    graphic.canvasRenderer.SetColor(original);
                if (graphic != null)
                {
                    _original.Remove(graphic);
                    _written.Remove(graphic);
                }
                _pulsing.Remove(target);
            }
            if (_halos.TryGetValue(target, out List<MeshRenderer> renderers))
            {
                foreach (MeshRenderer renderer in renderers)
                {
                    if (renderer != null)
                        Discard(renderer.gameObject);
                }
                _halos.Remove(target);
            }
        }

        private List<MeshRenderer> CreateHalos(Transform target)
        {
            List<MeshRenderer> halos = new List<MeshRenderer>();
            if (_haloMaterial == null)
                return halos;
            foreach (MeshFilter filter in target.GetComponentsInChildren<MeshFilter>(false))
            {
                MeshRenderer source = filter.GetComponent<MeshRenderer>();
                if (filter.name == HaloName || filter.sharedMesh == null || source == null || !source.enabled)
                    continue;
                GameObject go = new GameObject(HaloName, typeof(MeshFilter), typeof(MeshRenderer))
                {
                    hideFlags = HideFlags.DontSave
                };
                go.transform.SetParent(filter.transform, false);
                go.layer = filter.gameObject.layer;
                go.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                MeshRenderer renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = _haloMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                halos.Add(renderer);
            }
            return halos;
        }

        private static void Discard(Object o)
        {
            if (Application.isPlaying)
                Destroy(o);
            else
                DestroyImmediate(o);
        }
    }
}
