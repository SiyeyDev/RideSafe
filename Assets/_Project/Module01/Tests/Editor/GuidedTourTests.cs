using NUnit.Framework;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    public class GuidedTourTests
    {
        private GameObject _go;
        private GuidedTour _tour;
        private GameObject _rig;

        [SetUp]
        public void SetUp()
        {
            _rig = new GameObject("rig");
            _go = new GameObject("tour");
            _tour = _go.AddComponent<GuidedTour>();
            _tour.ConfigureForTests(_rig.transform);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_rig);
        }

        [Test]
        public void Ir_a_una_zona_registrada_mueve_el_rig_a_su_ancla()
        {
            GameObject anchor = new GameObject("anchor_head");
            anchor.transform.position = new Vector3(3f, 0f, 5f);
            _tour.RegisterAnchor("head", anchor.transform);

            Assert.IsTrue(_tour.TryGoTo("head"));
            Assert.AreEqual(new Vector3(3f, 0f, 5f), _rig.transform.position);
            Assert.AreEqual("head", _tour.CurrentZoneId);

            Object.DestroyImmediate(anchor);
        }

        [Test]
        public void Ir_a_una_zona_desconocida_no_mueve_nada_y_devuelve_falso()
        {
            Vector3 before = _rig.transform.position;

            // Avisa con LogWarning, que no hace fallar el test; un LogError si lo haria.
            Assert.IsFalse(_tour.TryGoTo("no_existe"));

            Assert.AreEqual(before, _rig.transform.position);
            Assert.IsNull(_tour.CurrentZoneId);
        }

        [Test]
        public void Llegar_a_la_zona_avisa_una_vez()
        {
            GameObject anchor = new GameObject("anchor_load");
            _tour.RegisterAnchor("load", anchor.transform);
            int reached = 0;
            _tour.ZoneReached += id => reached++;

            _tour.TryGoTo("load");

            Assert.AreEqual(1, reached);
            Object.DestroyImmediate(anchor);
        }

        [Test]
        public void El_id_de_zona_no_distingue_mayusculas_ni_espacios()
        {
            GameObject anchor = new GameObject("anchor_cockpit");
            _tour.RegisterAnchor("  Cockpit ", anchor.transform);

            Assert.IsTrue(_tour.TryGoTo("COCKPIT"));
            Assert.AreEqual("cockpit", _tour.CurrentZoneId);

            Object.DestroyImmediate(anchor);
        }
    }
}
