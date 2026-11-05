using System.Collections.Generic;
using NUnit.Framework;
using RideSafe.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// «Evaluación de tus elecciones» (PO, 2026-11-04): la sección de EPP se evalúa en tarjetas con
    /// la píldora del puntaje, y el mismo panel vuelve a las filas para el vehículo y el final.
    /// </summary>
    public class ComparisonCardModeTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private ComparisonView _view;
        private RectTransform _rows, _cards;
        private GameObject _pill, _divider;
        private TMP_Text _noteText;
        private TMP_Text _title, _score;
        private Image _columnBackground;
        private ScrollRect _scroll;

        [SetUp]
        public void SetUp()
        {
            GameObject panel = Track(new GameObject("Comparison", typeof(RectTransform)));
            _title = Child(panel.transform, "Title").gameObject.AddComponent<TextMeshProUGUI>();
            // Sin KeepBelow la nota que se apaga es el propio texto (en el prefab, su caja).
            _noteText = Child(panel.transform, "Body").gameObject.AddComponent<TextMeshProUGUI>();

            RectTransform column = Child(panel.transform, "LeftColumn");
            _columnBackground = Child(column, "Background").gameObject.AddComponent<Image>();
            _pill = Child(column, "ScorePill").gameObject;
            _score = Child(_pill.transform, "Text").gameObject.AddComponent<TextMeshProUGUI>();
            _divider = Child(column, "Divider").gameObject;
            RectTransform viewport = Child(column, "Viewport");
            _scroll = viewport.gameObject.AddComponent<ScrollRect>();
            _rows = Child(viewport, "Rows");
            _cards = Child(viewport, "Cards");
            _scroll.content = _rows;
            RectTransform template = Child(_rows, "RowTemplate");
            template.gameObject.SetActive(false);

            ItemCard card = Track(new GameObject("PF_EppCard", typeof(RectTransform))).AddComponent<ItemCard>();
            card.gameObject.SetActive(false);
            CardGrid grid = panel.AddComponent<CardGrid>();
            grid.Configure(card, _cards);

            _view = panel.AddComponent<ComparisonView>();
            SerializedObject serialized = new SerializedObject(_view);
            serialized.FindProperty("_leftRows").objectReferenceValue = _rows;
            serialized.FindProperty("_rowTemplate").objectReferenceValue = template;
            serialized.FindProperty("_title").objectReferenceValue = _title;
            serialized.FindProperty("_subtitle").objectReferenceValue = _noteText;
            serialized.FindProperty("_column").objectReferenceValue = column;
            serialized.FindProperty("_cards").objectReferenceValue = grid;
            serialized.FindProperty("_score").objectReferenceValue = _score;
            serialized.FindProperty("_columnBackground").objectReferenceValue = _columnBackground;
            SerializedProperty cardsOnly = serialized.FindProperty("_cardsOnly");
            cardsOnly.arraySize = 1;
            cardsOnly.GetArrayElementAtIndex(0).objectReferenceValue = _pill;
            SerializedProperty rowsOnly = serialized.FindProperty("_rowsOnly");
            rowsOnly.arraySize = 1;
            rowsOnly.GetArrayElementAtIndex(0).objectReferenceValue = _divider;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects)
                Object.DestroyImmediate(go);
            _objects.Clear();
        }

        private GameObject Track(GameObject go)
        {
            _objects.Add(go);
            return go;
        }

        private static RectTransform Child(Transform parent, string name)
        {
            RectTransform rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static IList<ComparisonEntry> Gear() => new[]
        {
            new ComparisonEntry(ItemStatus.Selected, "Casco abrochado") { ItemId = "helmet_ok" },
            new ComparisonEntry(ItemStatus.NotSuited, "Paraguas de mano") { ItemId = "umbrella" },
        };

        [Test]
        public void SetCards_ShowsTitleScoreAndOneCardPerItem()
        {
            _view.SetCards("Evaluación de tus elecciones", "Puntaje: 85", Gear());

            Assert.AreEqual("Evaluación de tus elecciones", _title.text);
            Assert.AreEqual("Puntaje: 85", _score.text);
            Assert.IsTrue(_pill.activeSelf, "the score pill is on");
            Assert.IsTrue(_cards.gameObject.activeSelf);
            Assert.AreEqual(2, _cards.childCount);
            Assert.AreEqual(2, _view.Entries.Count, "Entries still tell what is on screen");
        }

        [Test]
        public void SetCards_OnlyTheTitle_NoNoteRowsOrBox()
        {
            // «Tener solo el título y eliminar los subtítulos» (PO, 2026-11-04).
            _view.SetCards("Evaluación de tus elecciones", "Puntaje: 85", Gear());

            Assert.IsFalse(_noteText.gameObject.activeSelf, "no note under the list");
            Assert.IsFalse(_rows.gameObject.activeSelf, "no rows");
            Assert.IsFalse(_divider.activeSelf);
            Assert.IsFalse(_columnBackground.enabled, "the cards sit on the panel, not in a box in a box");
            Assert.AreSame(_cards, _scroll.content, "the window scrolls the cards");
        }

        [Test]
        public void SetSingleList_GoesBackToRows()
        {
            // El vehículo y el reporte final siguen en filas en el mismo panel.
            _view.SetCards("Evaluación de tus elecciones", "Puntaje: 85", Gear());
            _view.SetSingleList("Mi Elección", "62 / 100", new[] { new ComparisonEntry(ItemStatus.Selected, "Frenos") });

            Assert.IsTrue(_rows.gameObject.activeSelf);
            Assert.IsFalse(_cards.gameObject.activeSelf);
            Assert.IsFalse(_pill.activeSelf);
            Assert.IsTrue(_divider.activeSelf);
            Assert.IsTrue(_columnBackground.enabled);
            Assert.AreSame(_rows, _scroll.content);
            Assert.AreEqual(1, _view.Entries.Count);
        }
    }
}
