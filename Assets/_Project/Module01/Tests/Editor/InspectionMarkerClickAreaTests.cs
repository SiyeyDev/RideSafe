using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// El aprendiz apunta al «!», no a la esfera de 8 cm que queda 26 cm por debajo: el signo
    /// tiene que recibir el clic y entregárselo a su punto, y solo donde se dibuja.
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
            BoxCollider box = (BoxCollider)area;
            Vector3 size = Vector3.Scale(box.size, area.transform.lossyScale);
            Assert.AreEqual(InspectionMarker.ClickSize.x, size.x, .005f, "8 cm de ancho: el trazo y un margen");
            Assert.AreEqual(InspectionMarker.ClickSize.y, size.y, .005f, "20 cm de alto: el recuadro del signo");
        }

        /// <summary>
        /// La cabina de la patineta (medida en Play, 2026-11-19): el «!» de la dirección queda a unos
        /// 11-14 cm del rayo que apunta al del display, y 16 cm más cerca; su esfera de 12 cm (15 con
        /// el latido) se lo tragaba y apuntar al del display abría la dirección.
        /// </summary>
        [Test]
        public void Apuntar_a_un_signo_no_abre_el_vecino_que_queda_delante()
        {
            GameObject near = new GameObject("point_steering");
            near.AddComponent<EventTrigger>();
            GameObject board = new GameObject("Marker");
            board.transform.SetParent(near.transform, false);
            board.transform.localScale = Vector3.one * .002f;
            try
            {
                near.AddComponent<InspectionMarker>().ConfigureForTests(null, board.transform);
                _marker.SetHighlighted(true);
                near.GetComponent<InspectionMarker>().SetHighlighted(true);
                // El aprendiz en el origen mirando a +Z; el del display a 2,4 m, el de la dirección a
                // 2,24 m y 11 cm a la derecha.
                _billboard.transform.position = new Vector3(0f, 0f, 2.4f);
                board.transform.position = new Vector3(.11f, 0f, 2.24f);
                Physics.SyncTransforms();

                Assert.IsTrue(Physics.Raycast(Vector3.zero, Vector3.forward, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Collide));
                Assert.AreSame(_point, ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.collider.gameObject),
                    "apuntando al «!» del display responde " + hit.collider.transform.parent.parent.name);
            }
            finally
            {
                Object.DestroyImmediate(near);
            }
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
