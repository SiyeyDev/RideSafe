using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.UI
{
    /// <summary>
    /// A panel whose list grows with its rows. When the rows need more height than the list box
    /// was built with, the box and its panels grow by the difference, what sits below the list
    /// moves down by it, and the panel is re-centred vertically. A short list leaves the panel
    /// exactly as built. Recomputed when the rows change. Assumes top-left anchors and pivots
    /// (UIBuilderKit layout). With the rows in a scrolled viewport the growth stops at a cap and
    /// the rest scrolls, so a long list keeps the panel's proportions.
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
        [Tooltip("Optional: the rows when they scroll inside Content (the viewport). Counted and measured instead of it.")]
        [SerializeField] private RectTransform _scrolledRows;
        [Tooltip("Most the box grows; past it the rows scroll. Negative = no cap.")]
        [SerializeField] private float _maxExtra = -1f;

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

        /// <summary>
        /// The same with the rows scrolling inside <paramref name="viewport"/>: the box grows with them
        /// up to <paramref name="maxExtra"/>, and past it the rows scroll (PO, 2026-10-26).
        /// </summary>
        public void Configure(RectTransform viewport, RectTransform box, RectTransform[] grow, RectTransform[] pushDown,
                              RectTransform recentre, RectTransform scrolledRows, float maxExtra)
        {
            Configure(viewport, box, grow, pushDown, recentre);
            _scrolledRows = scrolledRows;
            _maxExtra = maxExtra;
        }

        private RectTransform Rows => _scrolledRows != null ? _scrolledRows : _content;

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
            LayoutRebuilder.ForceRebuildLayoutImmediate(Rows);
            float needed = LayoutUtility.GetPreferredHeight(Rows);
            float extra = Mathf.Max(0f, needed - _contentHeight);
            if (_maxExtra >= 0f)
                extra = Mathf.Min(extra, _maxExtra);
            bool rowsChanged = ActiveRows() != _lastCount;
            _lastCount = ActiveRows();
            // A new list starts at its first row, wherever the last one was left.
            if (rowsChanged && _scrolledRows != null)
            {
                ScrollRect scroll = _content.GetComponentInParent<ScrollRect>(true);
                if (scroll != null)
                    scroll.verticalNormalizedPosition = 1f;
            }
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
            RectTransform rows = Rows;
            int count = 0;
            for (int i = 0; i < rows.childCount; i++)
                if (rows.GetChild(i).gameObject.activeSelf)
                    count++;
            return count;
        }

        private static void SetHeight(RectTransform rect, float height) =>
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
    }
}
