using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.UI
{
    /// <summary>
    /// A panel whose list grows with its rows. When the rows need more height than the list box
    /// was built with, the box and its panels grow by the difference, what sits below the list
    /// moves down by it, and the panel is re-centred vertically. A short list leaves the panel
    /// exactly as built. Recomputed when the rows change. Assumes top-left anchors and pivots
    /// (UIBuilderKit layout).
    /// </summary>
    [AddComponentMenu("RideSafe/UI/Grow With Content")]
    public class GrowWithContent : MonoBehaviour
    {
        [Tooltip("The rows, laid out by a VerticalLayoutGroup.")]
        [SerializeField] private RectTransform _content;
        [Tooltip("The card around the rows.")]
        [SerializeField] private RectTransform _box;
        [Tooltip("Panels that grow by the same height (e.g. the modal).")]
        [SerializeField] private RectTransform[] _grow = new RectTransform[0];
        [Tooltip("What sits below the list and moves down (notice, buttons).")]
        [SerializeField] private RectTransform[] _pushDown = new RectTransform[0];
        [Tooltip("Optional: moved up by half the extra height, so the panel stays centred.")]
        [SerializeField] private RectTransform _recentre;

        private bool _captured;
        private float _contentHeight, _boxHeight, _recentreY;
        private float[] _growHeights, _pushY;
        private float _lastExtra = -1f;
        private int _lastCount = -1;

        public void Configure(RectTransform content, RectTransform box, RectTransform[] grow, RectTransform[] pushDown,
                              RectTransform recentre)
        {
            _content = content;
            _box = box;
            _grow = grow ?? new RectTransform[0];
            _pushDown = pushDown ?? new RectTransform[0];
            _recentre = recentre;
            _captured = false;
        }

        private void LateUpdate()
        {
            if (_content != null && ActiveRows() != _lastCount)
                Refresh();
        }

        public void Refresh()
        {
            if (_content == null || _box == null)
                return;
            Capture();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            float needed = LayoutUtility.GetPreferredHeight(_content);
            float extra = Mathf.Max(0f, needed - _contentHeight);
            _lastCount = ActiveRows();
            if (Mathf.Approximately(extra, _lastExtra))
                return;
            _lastExtra = extra;

            SetHeight(_content, _contentHeight + extra);
            SetHeight(_box, _boxHeight + extra);
            for (int i = 0; i < _grow.Length; i++)
                if (_grow[i] != null)
                    SetHeight(_grow[i], _growHeights[i] + extra);
            for (int i = 0; i < _pushDown.Length; i++)
                if (_pushDown[i] != null)
                    _pushDown[i].anchoredPosition = new Vector2(_pushDown[i].anchoredPosition.x, _pushY[i] - extra);
            if (_recentre != null)
                _recentre.anchoredPosition = new Vector2(_recentre.anchoredPosition.x, _recentreY + extra / 2f);
        }

        private void Capture()
        {
            if (_captured)
                return;
            _contentHeight = _content.sizeDelta.y;
            _boxHeight = _box.sizeDelta.y;
            _growHeights = new float[_grow.Length];
            for (int i = 0; i < _grow.Length; i++)
                _growHeights[i] = _grow[i] != null ? _grow[i].sizeDelta.y : 0f;
            _pushY = new float[_pushDown.Length];
            for (int i = 0; i < _pushDown.Length; i++)
                _pushY[i] = _pushDown[i] != null ? _pushDown[i].anchoredPosition.y : 0f;
            _recentreY = _recentre != null ? _recentre.anchoredPosition.y : 0f;
            _captured = true;
        }

        private int ActiveRows()
        {
            int count = 0;
            foreach (Transform child in _content)
                if (child.gameObject.activeSelf)
                    count++;
            return count;
        }

        private static void SetHeight(RectTransform rect, float height) =>
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
    }
}
