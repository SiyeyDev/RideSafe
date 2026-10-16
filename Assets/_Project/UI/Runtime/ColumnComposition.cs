using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.UI
{
    /// <summary>
    /// Lays a report screen out as one column: title bar, note and list share the list's
    /// visible width and left edge, the title sits in the true centre of the bar, and each
    /// block goes below the previous one with a small gap; the button below the list is placed
    /// under it (at runtime ComparisonView.KeepBelow keeps it there as the list grows). Pure RectTransform maths on top-left anchored
    /// UIBuilderKit layouts, shared by the prefab builder and the in-place prefab fix.
    /// </summary>
    public static class ColumnComposition
    {
        public const float Gap = 8f;
        public const float NoteHeight = 60f;

        /// <param name="barPad">9-slice padding of the bar sprite (UIBuilderKit.PadFor of its multiplier).</param>
        /// <param name="listPad">9-slice padding of the list card.</param>
        public static void Arrange(RectTransform panel, RectTransform bar, float barPad, RectTransform title,
                                   RectTransform note, RectTransform list, float listPad, RectTransform below)
        {
            foreach (string decoration in new[] { "Heading", "Divider" })
            {
                Transform t = list.Find(decoration);
                if (t != null)
                    t.gameObject.SetActive(false);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(list);

            float left = Left(list) + listPad;
            float width = list.sizeDelta.x - 2f * listPad;

            bar.anchoredPosition = new Vector2(left - barPad, bar.anchoredPosition.y);
            bar.sizeDelta = new Vector2(width + 2f * barPad, bar.sizeDelta.y);
            // Centred both ways in the bar: the title used to sit against its top edge.
            title.anchoredPosition = new Vector2(bar.sizeDelta.x / 2f - title.sizeDelta.x / 2f,
                                                 -bar.sizeDelta.y / 2f + title.sizeDelta.y / 2f);
            TMPro.TMP_Text titleText = title.GetComponent<TMPro.TMP_Text>();
            if (titleText != null)
                titleText.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;

            // The note keeps the list's width and wraps, instead of stretching with its text.
            ContentSizeFitter fitter = note.GetComponent<ContentSizeFitter>();
            if (fitter != null)
            {
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
            HorizontalOrVerticalLayoutGroup layout = note.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (layout != null)
            {
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            }
            foreach (TMPro.TMP_Text text in note.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                text.textWrappingMode = TMPro.TextWrappingModes.Normal;
                text.alignment = TMPro.TextAlignmentOptions.Center;
            }
            note.sizeDelta = new Vector2(list.sizeDelta.x, NoteHeight);
            SetTopLeft(note, Left(list), Bottom(bar) + Gap);

            SetTopLeft(list, Left(list), Bottom(note) + Gap);
            // ComparisonView.KeepBelow keeps the button under the list at runtime; this only
            // places it for the built layout.
            if (below != null)
                SetTopLeft(below, Left(below), Bottom(list) + 2f * Gap);
            GrowWithContent stale = panel.GetComponent<GrowWithContent>();
            if (stale != null)
                Object.DestroyImmediate(stale, true);
        }

        private static float Left(RectTransform r) => r.anchoredPosition.x - r.pivot.x * r.sizeDelta.x;
        private static float Top(RectTransform r) => -(r.anchoredPosition.y + (1f - r.pivot.y) * r.sizeDelta.y);
        private static float Bottom(RectTransform r) => Top(r) + r.sizeDelta.y;

        /// <summary>Places the rect's top-left corner at (x, y) frame px (y down), whatever its pivot.</summary>
        private static void SetTopLeft(RectTransform r, float x, float y)
        {
            r.anchoredPosition = new Vector2(x + r.pivot.x * r.sizeDelta.x, -y - (1f - r.pivot.y) * r.sizeDelta.y);
        }
    }
}
