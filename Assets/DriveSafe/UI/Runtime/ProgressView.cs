using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace RideSafe.UI
{
    /// <summary>
    /// Shows progress in one of three ways:
    /// <list type="bullet">
    /// <item><b>Bar</b> — continuous pill fill (video, item counter, hold). Anchor-driven so a
    /// 9-sliced pill keeps round caps at any value.</item>
    /// <item><b>Segments</b> — "Step 2 of 4": every segment up to the current one is active.</item>
    /// <item><b>Stages</b> — repair strip: only the current stage is active, its label bold.</item>
    /// </list>
    /// </summary>
    [DisallowMultipleComponent]
    public class ProgressView : MonoBehaviour
    {
        public enum Mode
        {
            Bar,
            Segments,
            Stages
        }

        [SerializeField] private Mode _mode;

        [Header("Bar")]
        [SerializeField] private RectTransform _fill;
        [SerializeField, Range(0f, 1f)] private float _value;

        [Header("Segments / Stages")]
        [SerializeField] private List<UnityEngine.UI.Image> _marks = new List<UnityEngine.UI.Image>();
        [SerializeField] private List<TMP_Text> _labels = new List<TMP_Text>();
        [SerializeField] private Sprite _activeSprite;
        [SerializeField] private Sprite _inactiveSprite;
        [SerializeField] private Color _activeColor = Color.white;
        [SerializeField] private Color _inactiveColor = new Color(0.835f, 0.871f, 0.91f, 1f);
        [SerializeField] private Color _activeLabel = Color.white;
        [SerializeField] private Color _inactiveLabel = new Color(0.72f, 0.78f, 0.84f, 1f);
        [Tooltip("1-based current step or stage.")]
        [SerializeField] private int _current = 1;

        /// <summary>Bar mode: 0..1.</summary>
        public void SetValue(float normalized)
        {
            _value = Mathf.Clamp01(normalized);
            Apply();
        }

        /// <summary>Segments/Stages mode: 1-based current step.</summary>
        public void SetStep(int current)
        {
            _current = Mathf.Clamp(current, 0, _marks.Count);
            Apply();
        }

        private void OnEnable() => Apply();

        private void OnValidate() => Apply();

        private void Apply()
        {
            if (_mode == Mode.Bar)
            {
                if (_fill == null)
                    return;
                Vector2 max = _fill.anchorMax;
                max.x = _value;
                _fill.anchorMax = max;
                _fill.gameObject.SetActive(_value > 0.001f);
                return;
            }

            for (int i = 0; i < _marks.Count; i++)
            {
                bool active = _mode == Mode.Segments ? i < _current : i == _current - 1;
                if (_marks[i] != null)
                {
                    Sprite sprite = active ? _activeSprite : _inactiveSprite;
                    if (sprite != null)
                        _marks[i].sprite = sprite;
                    _marks[i].color = active ? _activeColor : _inactiveColor;
                }
                if (i < _labels.Count && _labels[i] != null)
                {
                    _labels[i].color = active ? _activeLabel : _inactiveLabel;
                    _labels[i].fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
                }
            }
        }
    }
}
