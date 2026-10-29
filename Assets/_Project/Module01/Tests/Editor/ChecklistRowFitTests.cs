using NUnit.Framework;
using RideSafe.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// La franja que resalta una fila de «Mi preparación» tiene que envolver su texto (PO,
    /// 2026-10-28, con captura): las filas medían 40 fijos y el sprite de la franja, troceado a
    /// 3, deja 16/3 transparentes arriba y abajo. Un nombre en dos líneas —«Gafas con lentes
    /// rayados, agrietados…»— llenaba los 40 de borde a borde, más alto que la franja, y rozaba la
    /// fila de al lado.
    /// </summary>
    public class ChecklistRowFitTests
    {
        private const float HighlightPad = 16f / 3f;

        private GameObject _root;
        private ChecklistView _view;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("ChecklistPanel", typeof(RectTransform));
            GameObject rows = new GameObject("Rows", typeof(RectTransform));
            rows.transform.SetParent(_root.transform, false);

            // Como ChecklistRowTemplate: 40 de alto mínimo, etiqueta estirada con 44 a la izquierda
            // y 90 a la derecha, letra de 17 con autoajuste hasta 12.
            GameObject template = new GameObject("RowTemplate", typeof(RectTransform), typeof(Button));
            template.transform.SetParent(rows.transform, false);
            ((RectTransform)template.transform).sizeDelta = new Vector2(400f, 40f);
            LayoutElement layout = template.AddComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = 40f;
            foreach (string child in new[] { "Label", "Tag" })
            {
                RectTransform rect = new GameObject(child, typeof(RectTransform)).GetComponent<RectTransform>();
                rect.SetParent(template.transform, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(44f, 0f);
                rect.offsetMax = new Vector2(-90f, 0f);
                TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
                text.enableAutoSizing = true;
                text.fontSizeMin = 12f;
                text.fontSizeMax = 17f;
                text.textWrappingMode = TextWrappingModes.Normal;
                text.alignment = TextAlignmentOptions.Left;
            }

            _view = _root.AddComponent<ChecklistView>();
            var so = new SerializedObject(_view);
            so.FindProperty("_rowTemplate").objectReferenceValue = template.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        private GameObject Row(string label) => _root.transform.Find("Rows/Row · " + label).gameObject;

        [Test]
        public void Un_nombre_en_dos_lineas_cabe_dentro_de_la_franja()
        {
            const string label = "Glasses with severely scratched, cracked, deformed, or fogged lenses";
            _view.AddItem("glasses_damaged", label);
            _view.FitRows();

            GameObject row = Row(label);
            TMP_Text text = row.transform.Find("Label").GetComponent<TMP_Text>();
            float band = row.GetComponent<LayoutElement>().preferredHeight - 2f * HighlightPad;
            float needed = text.GetPreferredValues(label, ((RectTransform)text.transform).rect.width, 0f).y;

            Assert.Greater(row.GetComponent<LayoutElement>().preferredHeight, 40f, "la fila no crece con su texto");
            Assert.GreaterOrEqual(band, needed, "el texto es más alto que la franja visible");
            Assert.GreaterOrEqual(((RectTransform)text.transform).offsetMin.y, HighlightPad, "el texto pisa el borde transparente");
            Assert.AreEqual(17f, text.fontSize, "la letra encoge en vez de crecer la fila");
        }

        [Test]
        public void Un_nombre_corto_deja_la_fila_en_40()
        {
            _view.AddItem("bell", "Bell");
            _view.FitRows();

            Assert.AreEqual(40f, Row("Bell").GetComponent<LayoutElement>().preferredHeight);
        }
    }
}
