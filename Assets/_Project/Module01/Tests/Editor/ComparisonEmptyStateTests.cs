using System.Collections.Generic;
using NUnit.Framework;
using RideSafe.UI;
using UnityEditor;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// «Mi Elección» lists only what the learner took. With nothing taken in a section the list box
    /// came out blank and read as a bug (Cindy, 2026-10-21): it says so instead.
    /// </summary>
    public class ComparisonEmptyStateTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private ComparisonView _view;
        private GameObject _empty;

        [SetUp]
        public void SetUp()
        {
            GameObject panel = Track(new GameObject("Comparison", typeof(RectTransform)));
            RectTransform rows = (RectTransform)new GameObject("Rows", typeof(RectTransform)).transform;
            rows.SetParent(panel.transform, false);
            RectTransform template = (RectTransform)new GameObject("RowTemplate", typeof(RectTransform)).transform;
            template.SetParent(rows, false);
            template.gameObject.SetActive(false);
            _empty = new GameObject("EmptyState", typeof(RectTransform));
            _empty.transform.SetParent(panel.transform, false);

            _view = panel.AddComponent<ComparisonView>();
            SerializedObject serialized = new SerializedObject(_view);
            serialized.FindProperty("_leftRows").objectReferenceValue = rows;
            serialized.FindProperty("_rowTemplate").objectReferenceValue = template;
            serialized.FindProperty("_emptyState").objectReferenceValue = _empty;
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

        [Test]
        public void NothingTaken_SaysSo()
        {
            _view.SetSingleList("Mi Elección", "Nota", new List<ComparisonEntry>());
            Assert.IsTrue(_empty.activeSelf);
        }

        [Test]
        public void SomethingTaken_ShowsTheRowsOnly()
        {
            _view.SetSingleList("Mi Elección", "Nota", new List<ComparisonEntry> { new ComparisonEntry(ItemStatus.Selected, "Frenos") });
            Assert.IsFalse(_empty.activeSelf);
        }
    }
}
