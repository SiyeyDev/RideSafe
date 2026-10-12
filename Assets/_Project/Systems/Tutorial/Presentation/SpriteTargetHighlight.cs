using UnityEngine;

namespace RideSafe.Tutorial.Presentation
{
    /// <summary>
    /// Target highlight drawn with a 9-slice sprite (RideSafe's Focus_Ring_Mint): Sliced and
    /// sized to the target with a uniform scale, so cards, buttons and the gate target all get
    /// the same rounded corners. Pulses gently while waiting; steady and fully opaque while the
    /// learner points at the target.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("RideSafe/Tutorial/Sprite Target Highlight")]
    public class SpriteTargetHighlight : MonoBehaviour, ITargetHighlightView
    {
        [Tooltip("World size of the sprite's border (its rounded corner), whatever the target's size.")]
        [SerializeField, Min(0.001f)] private float _cornerSize = 0.035f;
        [Tooltip("Extra world margin around the framed area.")]
        [SerializeField, Min(0f)] private float _margin = 0.015f;
        [Tooltip("Pulse while waiting, as a fraction of the size.")]
        [SerializeField, Range(0f, 0.2f)] private float _pulse = 0.04f;
        [SerializeField, Min(0.1f)] private float _pulseSpeed = 4f;
        [SerializeField, Range(0f, 1f)] private float _waitingAlpha = 0.8f;
        [SerializeField, Range(0f, 1f)] private float _focusedAlpha = 1f;

        private SpriteRenderer _renderer;

        public void Show() => gameObject.SetActive(true);

        public void Hide() => gameObject.SetActive(false);

        public void Frame(Vector3 center, Vector2 halfSize, Quaternion facing, bool focused)
        {
            if (_renderer == null)
                _renderer = GetComponent<SpriteRenderer>();
            Sprite sprite = _renderer.sprite;
            if (sprite == null)
                return;

            _renderer.drawMode = SpriteDrawMode.Sliced;
            float borderUnits = Mathf.Max(sprite.border.x, sprite.border.y) / sprite.pixelsPerUnit;
            float scale = borderUnits > 0f ? _cornerSize / borderUnits : 1f;
            float grow = focused ? 1f : 1f + _pulse * (0.5f + 0.5f * Mathf.Sin(Time.time * _pulseSpeed));

            transform.SetPositionAndRotation(center, facing);
            transform.localScale = Vector3.one * scale;
            Vector2 world = (halfSize * 2f + Vector2.one * (_margin * 2f)) * grow;
            _renderer.size = world / scale;

            Color color = _renderer.color;
            color.a = focused ? _focusedAlpha : _waitingAlpha;
            _renderer.color = color;
        }
    }
}
