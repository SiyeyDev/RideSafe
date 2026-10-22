using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// El «!» del punto en revisión es naranja, y el ya revisado se queda a la vista en verde,
    /// también cuando el recorrido pasa a otra zona: Cindy (2026-10-21) pidió el verde en el signo
    /// como ya lo tiene el contorno, porque ayuda a ver de un vistazo cuáles van.
    /// </summary>
    public class InspectionMarkerColorTests
    {
        private static readonly Color Orange = new Color(1f, .42f, .05f, 1f);
        private static readonly Color Green = new Color(.46f, .83f, .54f, .9f);

        private GameObject _point;
        private InspectionMarker _marker;

        [SetUp]
        public void SetUp()
        {
            _point = new GameObject("point_brakes");
            GameObject billboard = new GameObject("Marker");
            billboard.transform.SetParent(_point.transform, false);
            TMP_Text glyph = new GameObject("Glyph").AddComponent<TextMeshPro>();
            glyph.transform.SetParent(billboard.transform, false);
            glyph.text = "!";
            _marker = _point.AddComponent<InspectionMarker>();
            _marker.ConfigureForTests(glyph, billboard.transform);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_point);

        private static void AssertColor(Color expected, Color actual, string message) =>
            Assert.AreEqual(0f, Vector4.Distance(expected, actual), 1e-3f, message);

        [Test]
        public void El_punto_en_revision_lleva_el_signo_naranja()
        {
            _marker.SetHighlighted(true);

            Assert.IsTrue(_marker.IsVisible);
            AssertColor(Orange, _marker.CurrentColor, "pendiente");
        }

        [Test]
        public void Revisado_el_signo_queda_verde_y_a_la_vista_aunque_cambie_la_zona()
        {
            _marker.SetHighlighted(true);
            _marker.SetResolved(true);
            AssertColor(Green, _marker.CurrentColor, "revisado");

            _marker.SetHighlighted(false);

            Assert.IsTrue(_marker.IsVisible, "lo revisado sigue marcado al pasar a otra zona");
            AssertColor(Green, _marker.CurrentColor, "y sigue en verde");
        }
    }
}
