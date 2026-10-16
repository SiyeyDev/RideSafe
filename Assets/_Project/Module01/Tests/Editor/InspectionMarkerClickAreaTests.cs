using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// El aprendiz apunta al «!», no a la esfera de 8 cm que queda 26 cm por debajo: el signo
    /// tiene que recibir el clic y entregárselo a su punto.
    /// </summary>
    public class InspectionMarkerClickAreaTests
    {
        private GameObject _point;
        private InspectionMarker _marker;
        private GameObject _billboard;

        [SetUp]
        public void SetUp()
        {
            _point = new GameObject("point_brakes");
            _point.AddComponent<EventTrigger>(); // hace de WorldObjectTaskEntity: recibe el clic
            _billboard = new GameObject("Marker");
            _billboard.transform.SetParent(_point.transform, false);
            _billboard.transform.localPosition = new Vector3(0f, .26f, 0f);
            _billboard.transform.localScale = Vector3.one * .002f; // canvas de world space
            _billboard.layer = LayerMask.NameToLayer("UI");
            _marker = _point.AddComponent<InspectionMarker>();
            _marker.ConfigureForTests(null, _billboard.transform);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_point);

        private Collider Area() => _billboard.GetComponentInChildren<Collider>(true);

        [Test]
        public void El_signo_tiene_su_propia_zona_de_clic()
        {
            Collider area = Area();
            Assert.IsNotNull(area, "sin zona de clic, apuntar al «!» no hace nada");
            Assert.IsTrue(area.isTrigger, "no empuja las manos ni el vehículo");
            Assert.AreEqual(0, area.gameObject.layer, "en Default: la capa UI no la ve el PhysicsRaycaster");
            float radius = ((SphereCollider)area).radius * area.transform.lossyScale.x;
            Assert.AreEqual(.12f, radius, .005f, "12 cm alrededor del signo");
        }

        [Test]
        public void El_clic_en_el_signo_llega_al_punto()
        {
            Assert.AreSame(_point, ExecuteEvents.GetEventHandler<IPointerClickHandler>(Area().gameObject));
        }

        [Test]
        public void La_zona_de_clic_se_apaga_con_el_signo()
        {
            _marker.SetHighlighted(true);
            Assert.IsTrue(Area().gameObject.activeInHierarchy);
            _marker.SetHighlighted(false);
            Assert.IsFalse(Area().gameObject.activeInHierarchy, "un «!» oculto no debe recibir clics");
        }
    }
}
