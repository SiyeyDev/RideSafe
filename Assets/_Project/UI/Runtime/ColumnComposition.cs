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

        /// <summary>
        /// Alto de la nota bajo la barra del título.
        /// <para>
        /// <b>64 y no 60.</b> Este método le activa el ajuste de línea al texto, y el español
        /// es más largo que el inglés con el que se diseñó el kit: medido el 2026-10-16, el
        /// subtítulo del reporte pedía 36,3 de alto en los 34,0 que deja una caja de 60, y eso
        /// tumbaba tres de los seis tests de PlayMode.
        /// </para>
        /// <para>
        /// Es un número y no un <c>ContentSizeFitter</c> a propósito. Con el ajuste puesto, en
        /// el editor el grupo de layout mide 0 —el panel no está visible— y esa altura se
        /// guarda en la escena como una sobrescritura de 0×0: el texto sale como una columna de
        /// letras, el prefab parece correcto y el fallo vuelve <b>cada vez que alguien guarda la
        /// escena</b>. Probado; no merece la pena.
        /// </para>
        /// <para>
        /// Si alguien alarga el subtítulo y deja de caber, lo caza el test de PlayMode que mide
        /// cada texto contra su caja. Entonces se sube este número.
        /// </para>
        /// </summary>
        public const float NoteHeight = 64f;

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

            // Ancho fijo —el de la lista— y alto fijo: ver NoteHeight.
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
