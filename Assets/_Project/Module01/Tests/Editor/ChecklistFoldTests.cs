using NUnit.Framework;
using RideSafe.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// «Mi preparación» como desplegable (PO, 2026-10-28): plegada solo se ve el título con
    /// cuántos elementos lleva, para que no se alargue sobre los muebles; al pulsar el título se
    /// abre la lista.
    /// </summary>
    public class ChecklistFoldTests
    {
        private GameObject _root;
        private ChecklistView _view;
        private TMP_Text _title;
        private GameObject _rows;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("ChecklistPanel", typeof(RectTransform));
            _title = new GameObject("Title", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            _title.transform.SetParent(_root.transform, false);
            _rows = new GameObject("Rows", typeof(RectTransform));
            _rows.transform.SetParent(_root.transform, false);

            GameObject template = new GameObject("Item", typeof(RectTransform), typeof(UnityEngine.UI.Button));
            template.transform.SetParent(_rows.transform, false);
            foreach (string child in new[] { "Label", "Tag" })
                new GameObject(child, typeof(RectTransform)).AddComponent<TextMeshProUGUI>().transform
                    .SetParent(template.transform, false);

            _view = _root.AddComponent<ChecklistView>();
            var so = new SerializedObject(_view);
            so.FindProperty("_rowTemplate").objectReferenceValue = template.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        private string Fold => _title.transform.Find("Fold").GetComponent<TMP_Text>().text;

        [Test]
        public void Plegada_solo_se_ve_el_titulo_con_la_cuenta()
        {
            _view.MakeCollapsible(_title, new GameObject[0]);

            Assert.IsTrue(_view.Collapsed);
            Assert.IsFalse(_rows.activeSelf, "las filas se ven con la lista plegada");
            StringAssert.StartsWith("(0)", Fold);
        }

        [Test]
        public void Elegir_algo_sube_la_cuenta_sin_abrir_la_lista()
        {
            _view.MakeCollapsible(_title, new GameObject[0]);

            _view.AddItem("helmet_ok", "Casco");

            StringAssert.StartsWith("(1)", Fold);
            Assert.IsFalse(_rows.activeSelf);
        }

        [Test]
        public void Pulsar_el_titulo_abre_y_cierra_la_lista()
        {
            _view.MakeCollapsible(_title, new GameObject[0]);

            _title.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.IsFalse(_view.Collapsed);
            Assert.IsTrue(_rows.activeSelf);

            _title.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.IsTrue(_view.Collapsed);
            Assert.IsFalse(_rows.activeSelf);
        }
    }
}
