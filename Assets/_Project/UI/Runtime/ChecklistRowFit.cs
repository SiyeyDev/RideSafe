using TMPro;
using UnityEngine;

namespace RideSafe.UI
{
    /// <summary>
    /// A checklist row as tall as its name (PO, 2026-10-28). The highlight is the row's own
    /// Checklist_Row_Selected, sliced at 3: it leaves UIBuilderKit.PadFor(3) transparent above and
    /// below, so at the template's fixed 40 the visible band was ~29 while a two-line name filled
    /// all 40 and brushed the next row. The label now sits inside the band and the row grows to
    /// hold it at full size; one-line names keep the template's height.
    /// <para>
    /// Refits itself when its width or name changes: the width only exists once the list's layout
    /// has run, which is after the row is created.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.UI.LayoutElement))]
    public class ChecklistRowFit : MonoBehaviour
    {
        private const float HighlightPad = 16f / 3f;
        private const float TextMargin = 4f;

        private RectTransform _label;
        private TMP_Text _text;
        private UnityEngine.UI.LayoutElement _layout;
        private float _baseHeight = -1f;
        private float _fittedWidth = -1f;
        private string _fittedText;

        private void LateUpdate()
        {
            if (Resolve() && _label.rect.width > 0f
                && (!Mathf.Approximately(_label.rect.width, _fittedWidth) || _text.text != _fittedText))
                Fit();
        }

        public void Fit()
        {
            if (!Resolve() || _label.rect.width <= 0f)
                return;

            _label.offsetMin = new Vector2(_label.offsetMin.x, HighlightPad);
            _label.offsetMax = new Vector2(_label.offsetMax.x, -HighlightPad);
            // The template shrank long names to fit its fixed height; with the row growing instead,
            // every name stays at the same, readable size.
            if (_text.enableAutoSizing)
            {
                _text.fontSize = _text.fontSizeMax;
                _text.enableAutoSizing = false;
            }
            float needed = _text.GetPreferredValues(_text.text, _label.rect.width, 0f).y;
            _layout.minHeight = _layout.preferredHeight = Mathf.Max(_baseHeight, needed + 2f * (HighlightPad + TextMargin));
            _fittedWidth = _label.rect.width;
            _fittedText = _text.text;
        }

        private bool Resolve()
        {
            if (_text != null)
                return true;
            _label = transform.Find("Label") as RectTransform;
            _text = _label != null ? _label.GetComponent<TMP_Text>() : null;
            _layout = GetComponent<UnityEngine.UI.LayoutElement>();
            if (_baseHeight < 0f && _layout != null)
                _baseHeight = _layout.minHeight;
            return _text != null;
        }
    }
}
