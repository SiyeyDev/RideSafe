using TMPro;
using UnityEngine;

namespace RideSafe.UI
{
    /// <summary>
    /// A text box as wide as its longest line, up to a limit, in whatever language (PO, 2026-10-29:
    /// «deja mucho espacio en las casillas»). The station sign was a fixed 640 with its mark at the
    /// left edge: short headings left a strip of nothing on the right and all the weight on the left.
    /// <para>
    /// It keeps its centre. The text's own margins (the room for an inline <see cref="TitleMark"/>)
    /// are kept and counted. With <see cref="_firstLineIndent"/> the room for the mark is made on the
    /// first paragraph only, with TMP's <c>&lt;line-indent&gt;</c>, so a heading over an instruction
    /// moves aside for the mark and the instruction under it does not. An optional box (the sign's
    /// image) follows the text with a fixed padding; a box made by <see cref="Enclose"/> follows it
    /// by itself.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/UI/Hug Text")]
    [DefaultExecutionOrder(990)] // before Enclose (1000) and TitleMark (1050)
    public class HugText : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [Tooltip("Optional: resized around the text, keeping its centre.")]
        [SerializeField] private RectTransform _box;
        [Tooltip("The longest a line may be before it wraps, margins not counted.")]
        [SerializeField] private float _maxWidth = 500f;
        [Tooltip("Room for the mark before the first paragraph only (0 = none).")]
        [SerializeField] private float _firstLineIndent;
        [Tooltip("Box only: from the box's edge to the text, each side (atlas padding included).")]
        [SerializeField] private Vector2 _boxPadding;
        [Tooltip("Also as tall as the text (the sign); off for one-line titles whose box sets the height.")]
        [SerializeField] private bool _hugHeight;

        private const string IndentTag = "<line-indent=";
        private string _last;

        public void Configure(TMP_Text text, RectTransform box, float maxWidth, float firstLineIndent, Vector2 boxPadding, bool hugHeight)
        {
            _text = text;
            _box = box;
            _maxWidth = maxWidth;
            _firstLineIndent = firstLineIndent;
            _boxPadding = boxPadding;
            _hugHeight = hugHeight;
            _last = null;
        }

        private void OnEnable() => _last = null;

        private void LateUpdate()
        {
            if (_text != null && _text.text != _last)
                Refresh();
        }

        public void Refresh()
        {
            if (_text == null)
                return;
            if (_firstLineIndent > 0f && !_text.text.StartsWith(IndentTag))
            {
                string text = _text.text;
                int end = text.IndexOf('\n');
                string first = end < 0 ? text : text.Substring(0, end);
                _text.text = IndentTag + _firstLineIndent.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + ">"
                             + first + "</line-indent>" + (end < 0 ? string.Empty : text.Substring(end));
            }

            RectTransform rect = _text.rectTransform;
            Vector4 margin = _text.margin;
            SetWidth(rect, margin.x + _maxWidth + margin.z);
            _text.ForceMeshUpdate();
            TMP_TextInfo info = _text.textInfo;
            float widest = 0f;
            for (int i = 0; i < info.lineCount; i++)
            {
                TMP_LineInfo line = info.lineInfo[i];
                if (line.visibleCharacterCount == 0)
                    continue;
                float width = line.lineExtents.max.x - line.lineExtents.min.x + (i == 0 ? _firstLineIndent : 0f);
                widest = Mathf.Max(widest, width);
            }
            // A couple of units of slack: at exactly its own width a line can break one word early.
            float hugged = margin.x + Mathf.Ceil(Mathf.Min(widest, _maxWidth)) + 2f + margin.z;
            SetWidth(rect, hugged);

            float height = rect.sizeDelta.y;
            if (_hugHeight)
            {
                _text.ForceMeshUpdate();
                height = Mathf.Ceil(_text.preferredHeight) + margin.y + margin.w;
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            }
            if (_box != null)
            {
                SetWidth(_box, hugged + 2f * _boxPadding.x);
                if (_hugHeight)
                    _box.sizeDelta = new Vector2(_box.sizeDelta.x, height + 2f * _boxPadding.y);
            }
            // Laid out again at the final width: the TitleMark reads these lines right after, and the
            // ones measured at the widest width put an inline mark on top of the title.
            _text.ForceMeshUpdate();
            _last = _text.text;
        }

        /// <summary>A new width around the same centre, whatever the pivot.</summary>
        private static void SetWidth(RectTransform rect, float width)
        {
            float before = (0.5f - rect.pivot.x) * rect.sizeDelta.x;
            float after = (0.5f - rect.pivot.x) * width;
            rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
            rect.anchoredPosition += new Vector2(before - after, 0f);
        }
    }
}
