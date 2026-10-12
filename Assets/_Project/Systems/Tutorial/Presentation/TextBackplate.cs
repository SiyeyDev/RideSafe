using TMPro;
using UnityEngine;

namespace RideSafe.Tutorial.Presentation
{
    /// <summary>
    /// Keeps a plate (e.g. RideSafe's Panel_Instruction_Wide) behind a world-space text, sized
    /// to the text, so it reads against any background (the bright garage ceiling as much as the dark cabinets). Refits only
    /// when the text changes. A 9-slice plate is drawn Sliced with a uniform scale, so its
    /// rounded corners never stretch with the text's width.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("RideSafe/Tutorial/Text Backplate")]
    public class TextBackplate : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _plate;
        [Tooltip("Margin around the text, in the text's local units.")]
        [SerializeField] private Vector2 _padding = new Vector2(0.05f, 0.03f);
        [Tooltip("Distance behind the text, in its local units.")]
        [SerializeField] private float _depth = 0.01f;
        [Tooltip("Size of the plate's rounded corner, in the text's local units.")]
        [SerializeField, Min(0.001f)] private float _cornerSize = 0.12f;

        private TMP_Text _text;
        private string _fitted;

        /// <summary>Half the height of the text with its plate, in the text's local units.</summary>
        public float LocalHalfHeight { get; private set; }

        public void Refit()
        {
            if (_text == null)
                _text = GetComponent<TMP_Text>();
            _fitted = _text.text;
            _text.ForceMeshUpdate();
            Bounds bounds = _text.textBounds;
            bool empty = string.IsNullOrEmpty(_fitted) || bounds.size.x <= 0f;
            LocalHalfHeight = empty ? 0f : bounds.extents.y + _padding.y;
            if (_plate == null)
                return;

            _plate.enabled = !empty;
            if (empty || _plate.sprite == null)
                return;
            Sprite sprite = _plate.sprite;
            Vector2 size = new Vector2(bounds.size.x + 2f * _padding.x, bounds.size.y + 2f * _padding.y);
            Transform plate = _plate.transform;
            plate.localPosition = new Vector3(bounds.center.x, bounds.center.y, _depth);

            float borderUnits = Mathf.Max(sprite.border.x, sprite.border.y) / sprite.pixelsPerUnit;
            if (borderUnits > 0f)
            {
                float scale = _cornerSize / borderUnits;
                _plate.drawMode = SpriteDrawMode.Sliced;
                plate.localScale = Vector3.one * scale;
                _plate.size = size / scale;
            }
            else
            {
                Vector2 spriteSize = sprite.bounds.size;
                _plate.drawMode = SpriteDrawMode.Simple;
                plate.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);
            }
        }

        protected virtual void LateUpdate()
        {
            if (_text == null || _text.text != _fitted)
                Refit();
        }
    }
}
