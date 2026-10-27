using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RideSafe.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// La nota de «Mi Elección» va justo bajo la lista, mida lo que mida (PO, 2026-10-26).
    /// <para>
    /// Con la sección vacía la nota se montaba sobre el aviso «No elegiste ningún elemento»: la
    /// lista se medía antes de encender el aviso. Y el reporte del vehículo, de tres filas, dejaba
    /// un hueco del alto de las nueve del reporte anterior, que seguían contando porque
    /// <c>Destroy</c> no las quita hasta el final del fotograma.
    /// </para>
    /// </summary>
    public class ComparisonNoteFollowsListTests
    {
        private const float Gap = 8f, RowHeight = 30f, EmptyHeight = 44f, ColumnTop = -100f;
        private readonly List<GameObject> _objects = new List<GameObject>();
        private ComparisonView _view;
        private RectTransform _column, _rows, _note;

        [SetUp]
        public void SetUp()
        {
            GameObject panel = new GameObject("Comparison", typeof(RectTransform));
            _objects.Add(panel);

            _column = TopLeft(new GameObject("LeftColumn", typeof(RectTransform)), panel.transform, ColumnTop);
            Stack(_column.gameObject);
            ContentSizeFitter fit = _column.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _rows = (RectTransform)new GameObject("Rows", typeof(RectTransform)).transform;
            _rows.SetParent(_column, false);
            Stack(_rows.gameObject);

            RectTransform template = (RectTransform)new GameObject("RowTemplate", typeof(RectTransform)).transform;
            template.SetParent(_rows, false);
            template.gameObject.AddComponent<LayoutElement>().preferredHeight = RowHeight;
            template.gameObject.SetActive(false);

            GameObject empty = new GameObject("EmptyState", typeof(RectTransform));
            empty.transform.SetParent(_column, false);
            empty.AddComponent<LayoutElement>().preferredHeight = EmptyHeight;
            empty.SetActive(false);

            _note = TopLeft(new GameObject("Body", typeof(RectTransform)), panel.transform, -500f);

            _view = panel.AddComponent<ComparisonView>();
            SerializedObject serialized = new SerializedObject(_view);
            serialized.FindProperty("_leftRows").objectReferenceValue = _rows;
            serialized.FindProperty("_rowTemplate").objectReferenceValue = template;
            serialized.FindProperty("_emptyState").objectReferenceValue = empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _view.KeepBelow(_note, Gap);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects)
                Object.DestroyImmediate(go);
            _objects.Clear();
        }

        [Test]
        public void Con_la_seccion_vacia_la_nota_queda_bajo_el_aviso()
        {
            _view.SetSingleList("Mi Elección", "Nota", new List<ComparisonEntry>());
            Assert.AreEqual(ColumnTop - EmptyHeight - Gap, _note.anchoredPosition.y, .5f);
        }

        [Test]
        public void Con_menos_filas_que_antes_la_nota_sube_hasta_la_lista()
        {
            _view.SetSingleList("Mi Elección", "Nota", Entries(9));
            _view.SetSingleList("Mi Elección", "Nota", Entries(3));
            Assert.AreEqual(ColumnTop - 3 * RowHeight - Gap, _note.anchoredPosition.y, .5f);
        }

        /// <summary>
        /// «Un scrollview para que no quede tan largo sino que mantenga las proporciones» (PO,
        /// 2026-10-26): la ventana de las filas crece con ellas hasta un tope y lo demás se desplaza;
        /// la nota sigue pegada debajo y el panel no se mueve.
        /// </summary>
        [Test]
        public void Una_lista_larga_se_desplaza_dentro_de_una_ventana_de_alto_maximo()
        {
            RectTransform viewport = (RectTransform)new GameObject("Viewport", typeof(RectTransform)).transform;
            viewport.SetParent(_column, false);
            viewport.SetAsFirstSibling();
            LayoutElement window = viewport.gameObject.AddComponent<LayoutElement>();
            viewport.gameObject.AddComponent<RectMask2D>();
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            _rows.SetParent(viewport, false);
            _rows.anchorMin = new Vector2(0f, 1f);
            _rows.anchorMax = Vector2.one;
            _rows.pivot = new Vector2(.5f, 1f);
            _rows.anchoredPosition = Vector2.zero;
            _rows.sizeDelta = Vector2.zero;
            _rows.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = _rows;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            SerializedObject serialized = new SerializedObject(_view);
            serialized.FindProperty("_column").objectReferenceValue = _column;
            serialized.FindProperty("_viewport").objectReferenceValue = window;
            serialized.FindProperty("_maxListHeight").floatValue = 100f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            RectTransform panel = (RectTransform)_view.transform;
            Vector2 built = panel.anchoredPosition;

            _view.SetSingleList("Mi Elección", "Nota", Entries(3));       // 90: fits
            Assert.AreEqual(90f, window.preferredHeight, .5f);
            Assert.AreEqual(ColumnTop - 90f - Gap, _note.anchoredPosition.y, .5f);

            _view.SetSingleList("Mi Elección", "Nota", Entries(20));      // 600: scrolls
            Assert.AreEqual(100f, window.preferredHeight, .5f, "the window stops at the cap");
            Assert.AreEqual(ColumnTop - 100f - Gap, _note.anchoredPosition.y, .5f, "the note right under the window");
            Assert.AreEqual(20 * RowHeight, _rows.rect.height, .5f, "the rows keep their height to scroll through");
            Assert.AreEqual(1f, scroll.verticalNormalizedPosition, .01f, "a new list starts at its first row");
            Assert.AreEqual(built, panel.anchoredPosition, "the panel keeps its place");
        }

        /// <summary>
        /// La nota del reporte final junta el rango y el cierre del módulo, y sin casco pedía tres
        /// líneas en una caja hecha para dos: la última se salía (54,5 de 38). La caja crece con su
        /// texto, nunca por debajo de lo construido.
        /// </summary>
        [TestCase("Nota", false)]
        [TestCase("Faltó buena parte del equipo imprescindible. Vuelve a recorrer el garaje sin prisa " +
                  "antes de circular. · Módulo completado. Revisa el resumen de tu preparación e inspección.", true)]
        public void La_caja_de_la_nota_crece_con_su_texto_y_nunca_encoge(string note, bool grows)
        {
            const float BuiltHeight = 64f, Padding = 13f;
            _note.sizeDelta = new Vector2(400f, BuiltHeight);
            HorizontalLayoutGroup card = _note.gameObject.AddComponent<HorizontalLayoutGroup>();
            card.padding = new RectOffset(13, 13, 13, 13);
            card.childControlWidth = card.childControlHeight = true;
            card.childForceExpandWidth = card.childForceExpandHeight = true;
            TMPro.TextMeshProUGUI text = new GameObject("Text", typeof(RectTransform)).AddComponent<TMPro.TextMeshProUGUI>();
            text.transform.SetParent(_note, false);
            text.fontSize = 15f;
            text.textWrappingMode = TMPro.TextWrappingModes.Normal;
            SerializedObject serialized = new SerializedObject(_view);
            serialized.FindProperty("_subtitle").objectReferenceValue = text;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            _view.SetSingleList("Mi Elección", note, Entries(3));

            float needed = text.GetPreferredValues(note, 400f - 2f * Padding, 0f).y + 2f * Padding;
            Assert.GreaterOrEqual(_note.sizeDelta.y, needed - .5f, "el texto se sale de su caja");
            if (grows)
                Assert.Greater(_note.sizeDelta.y, BuiltHeight, "tres lineas no caben en la caja construida");
            else
                Assert.AreEqual(BuiltHeight, _note.sizeDelta.y, .5f, "una nota corta deja la caja como se construyo");
        }

        private static List<ComparisonEntry> Entries(int count) =>
            Enumerable.Range(0, count).Select(i => new ComparisonEntry(ItemStatus.Selected, "Fila " + i)).ToList();

        private static RectTransform TopLeft(GameObject go, Transform parent, float y)
        {
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(400f, 0f);
            return rect;
        }

        private static void Stack(GameObject go)
        {
            VerticalLayoutGroup stack = go.AddComponent<VerticalLayoutGroup>();
            stack.childControlWidth = stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
        }
    }
}
