using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.UI
{
    /// <summary>
    /// A choice card sized to what it holds, in whatever language: unselected it hugs its name
    /// and description; selected it grows just enough for the check beside the name (and, with
    /// <see cref="HoldCaptionInside"/>, downward for its SELECTED caption). It stays
    /// centred where it was built (over its vehicle). Recomputed when the text or the selection
    /// changes. Assumes the card and its children are anchored top-left (UIBuilderKit cards).
    /// </summary>
    [AddComponentMenu("RideSafe/UI/Fit Card To Content")]
    public class FitCardToContent : MonoBehaviour
    {
        public const float SideMargin = 16f;
        public const float CheckGap = 8f;
        /// <summary>What the card grows downward to hold its caption inside when selected.</summary>
        public const float CaptionRow = 16f;
        private const float Pad = 16f / 2.8f; // UIBuilderKit.PadFor(2.8): slice padding around the visible card

        [SerializeField] private Toggle _toggle;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _description;
        [SerializeField] private RectTransform _check;
        [Tooltip("Optional caption (e.g. SELECTED), kept as wide as the card.")]
        [SerializeField] private RectTransform _caption;
        [Tooltip("Selected, the card grows downward and holds the caption inside instead of under it.")]
        [SerializeField] private bool _captionInside;

        private bool _hasCentre;
        private float _centre;
        private bool _hasHeight;
        private float _height;
        private string _lastTitle, _lastDescription;
        private bool _lastOn;

        public float ContentWidth { get; private set; }

        public void Configure(Toggle toggle, TMP_Text title, TMP_Text description, RectTransform check, RectTransform caption)
        {
            _toggle = toggle;
            _title = title;
            _description = description;
            _check = check;
            _caption = caption;
            _hasCentre = false;
        }

        public void HoldCaptionInside(bool inside)
        {
            _captionInside = inside;
            _hasHeight = false;
        }

        private void OnEnable() => Refresh();

        private void LateUpdate()
        {
            if (_title == null)
                return;
            bool on = _toggle != null && _toggle.isOn;
            if (_title.text != _lastTitle || (_description != null && _description.text != _lastDescription) || on != _lastOn)
                Refresh();
        }

        public void Refresh()
        {
            RectTransform card = (RectTransform)transform;
            if (_title == null)
                return;
            if (!_hasCentre)
            {
                _centre = card.anchoredPosition.x + card.sizeDelta.x / 2f;
                _hasCentre = true;
            }

            bool on = _toggle != null && _toggle.isOn;
            ContentWidth = Mathf.Ceil(Mathf.Max(Preferred(_title), Preferred(_description)));
            float visible = ContentWidth + 2f * SideMargin + (on && _check != null ? CheckGap + _check.sizeDelta.x : 0f);

            card.sizeDelta = new Vector2(visible + 2f * Pad, card.sizeDelta.y);
            card.anchoredPosition = new Vector2(_centre - card.sizeDelta.x / 2f, card.anchoredPosition.y);
            Place(_title.rectTransform, Pad + SideMargin, ContentWidth);
            if (_description != null)
                Place(_description.rectTransform, Pad + SideMargin, ContentWidth);
            if (_check != null)
                _check.anchoredPosition = new Vector2(Pad + SideMargin + ContentWidth + CheckGap, _check.anchoredPosition.y);
            if (_caption != null)
                Place(_caption, Pad + 8f, visible - 16f);
            if (_captionInside && _caption != null)
            {
                if (!_hasHeight)
                {
                    _height = card.sizeDelta.y;
                    _hasHeight = true;
                }
                card.sizeDelta = new Vector2(card.sizeDelta.x, _height + (on ? CaptionRow : 0f));
                // Where the closed card's bottom edge was, a little up: the description is one short line.
                _caption.anchoredPosition = new Vector2(_caption.anchoredPosition.x, -(_height - Pad - 8f));
            }

            _lastTitle = _title.text;
            _lastDescription = _description != null ? _description.text : null;
            _lastOn = on;
        }

        private static float Preferred(TMP_Text text) =>
            text == null || string.IsNullOrEmpty(text.text) ? 0f : text.GetPreferredValues(text.text).x;

        private static void Place(RectTransform rect, float x, float width)
        {
            rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
            rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
        }
    }
}
