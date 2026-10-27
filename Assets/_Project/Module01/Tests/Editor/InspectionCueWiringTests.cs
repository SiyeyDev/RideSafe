using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using RideSafe.TaskSequence;
using EntityId = RideSafe.TaskSequence.EntityId;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// El punto de inspección puede avisar de dos maneras —el signo flotante de siempre y el
    /// contorno sobre la pieza real— y el recorrido no tiene por qué saber cuál hay.
    /// <see cref="ZoneRunner"/> habla con <see cref="IInspectionCue"/> y las dos obedecen.
    /// </summary>
    public class InspectionCueContractTests
    {
        private GameObject _point;
        private GameObject _part;
        private Material _material;
        private Mesh _mesh;

        [SetUp]
        public void SetUp()
        {
            _mesh = TestMeshes.Tetrahedron();
            _material = new Material(Shader.Find("Unlit/Color"));
            _part = new GameObject("manubrio", typeof(MeshFilter), typeof(MeshRenderer));
            _part.GetComponent<MeshFilter>().sharedMesh = _mesh;
            _point = new GameObject("point_steering");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_point);
            Object.DestroyImmediate(_part);
            Object.DestroyImmediate(_material);
            Object.DestroyImmediate(_mesh);
        }

        [Test]
        public void Los_dos_avisos_del_punto_responden_a_la_misma_interfaz()
        {
            _point.AddComponent<InspectionMarker>();
            InspectionHighlight highlight = _point.AddComponent<InspectionHighlight>();
            highlight.ConfigureForTests(_material, Color.red, Color.green);
            highlight.Bind(_part.transform);

            IInspectionCue[] cues = _point.GetComponents<IInspectionCue>();

            Assert.AreEqual(2, cues.Length,
                "El recorrido recorre los avisos por interfaz; si uno no la implementa, se queda mudo.");
        }

        [Test]
        public void Avisar_por_la_interfaz_enciende_el_contorno()
        {
            InspectionHighlight highlight = _point.AddComponent<InspectionHighlight>();
            highlight.ConfigureForTests(_material, Color.red, Color.green);
            highlight.Bind(_part.transform);

            foreach (IInspectionCue cue in _point.GetComponents<IInspectionCue>())
                cue.SetHighlighted(true);

            Assert.IsTrue(highlight.Outline.enabled);
        }
    }

    /// <summary>
    /// Cuando el punto tiene una pieza real que delinear, el signo de exclamación sobra: los
    /// dos a la vez señalan lo mismo dos veces y el signo vuelve a tapar lo que hay detrás.
    /// </summary>
    public class InspectionMarkerSuppressionTests
    {
        private GameObject _point;
        private InspectionMarker _marker;
        private GameObject _billboard;

        [SetUp]
        public void SetUp()
        {
            _point = new GameObject("point_steering");
            _billboard = new GameObject("Marker");
            _billboard.transform.SetParent(_point.transform, false);
            _marker = _point.AddComponent<InspectionMarker>();
            _marker.ConfigureForTests(null, _billboard.transform);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_point);

        [Test]
        public void Un_punto_en_revision_ensena_su_signo()
        {
            _marker.SetHighlighted(true);

            Assert.IsTrue(_marker.IsVisible);
        }

        [Test]
        public void Silenciar_el_signo_lo_apaga_aunque_el_punto_este_en_revision()
        {
            _marker.SetHighlighted(true);

            _marker.SetSuppressed(true);

            Assert.IsFalse(_marker.IsVisible,
                "Con contorno sobre la pieza real, el signo estorba.");
        }

        /// <summary>
        /// Al cambiar a un vehículo que no despieza ese componente, el mismo punto se queda
        /// sin pieza, y entonces el signo tiene que volver. Pasa de verdad en los dos
        /// sentidos: la pantalla es pieza en la bici y no en la patineta.
        /// </summary>
        [Test]
        public void Dejar_de_silenciarlo_lo_devuelve()
        {
            _marker.SetHighlighted(true);
            _marker.SetSuppressed(true);

            _marker.SetSuppressed(false);

            Assert.IsTrue(_marker.IsVisible);
        }
    }

    /// <summary>
    /// El escenario es quien sabe qué malla está encendida, así que es quien cuelga el
    /// contorno: la pieza se busca por nombre dentro del vehículo vivo, con el nombre que
    /// declara su perfil. Un punto sin pieza se queda exactamente como estaba.
    /// </summary>
    public class VehicleStageOutlineTests
    {
        private GameObject _go;
        private GameObject _bike;
        private GameObject _scooter;
        private GameObject _manubrio;
        private VehicleStage _stage;
        private VehicleProfileSO _ebike;
        private VehicleProfileSO _escooter;
        private ZoneSO _zone;
        private Material _material;
        private Mesh _mesh;

        private WorldObjectTaskEntity _steering;
        private WorldObjectTaskEntity _lights;
        private InspectionHighlight _steeringHighlight;
        private InspectionMarker _steeringMarker;
        private InspectionMarker _lightsMarker;

        [SetUp]
        public void SetUp()
        {
            _mesh = TestMeshes.Tetrahedron();
            _material = new Material(Shader.Find("Unlit/Color"));

            _bike = new GameObject("ebike");
            _manubrio = new GameObject("manubrio", typeof(MeshFilter), typeof(MeshRenderer));
            _manubrio.GetComponent<MeshFilter>().sharedMesh = _mesh;
            _manubrio.transform.SetParent(_bike.transform, false);
            _scooter = new GameObject("E_Scooter");
            // Los dos llevan sus mounts, como en la escena: es el mount el que recoge el punto
            // cuando el vehiculo nuevo no tiene pieza para el.
            foreach (GameObject vehicle in new[] { _bike, _scooter })
                foreach (string id in new[] { "point_steering", "point_lights" })
                    new GameObject("mount_" + id).transform.SetParent(vehicle.transform, false);

            _steering = Point("point_steering", out _steeringMarker, out _steeringHighlight);
            _lights = Point("point_lights", out _lightsMarker, out _);

            // Los dos revisan los dos puntos, como los perfiles reales: un vehiculo apaga los
            // puntos que no revisa (la plataforma con la bici), y estos tests no van de eso.
            _zone = Zone("point_steering", "point_lights");

            _ebike = ScriptableObject.CreateInstance<VehicleProfileSO>();
            _ebike.Configure("ebike", null, new[] { _zone });
            _ebike.ConfigurePartBindings(new[] { ("point_steering", "manubrio") });

            _escooter = ScriptableObject.CreateInstance<VehicleProfileSO>();
            _escooter.Configure("escooter", null, new[] { _zone });

            _go = new GameObject("stage");
            _stage = _go.AddComponent<VehicleStage>();
            _stage.ConfigureForTests(("ebike", _bike), ("escooter", _scooter));
            _stage.ConfigurePointsForTests(_steering, _lights);
        }

        private ZoneSO Zone(params string[] itemIds)
        {
            List<SafetyItemSO> items = new List<SafetyItemSO>();
            foreach (string itemId in itemIds)
            {
                SafetyItemSO item = ScriptableObject.CreateInstance<SafetyItemSO>();
                item.ConfigureForTests(itemId, "k", "k_Risk", SafetyItemCategory.Core);
                items.Add(item);
            }
            ZoneSO zone = ScriptableObject.CreateInstance<ZoneSO>();
            zone.ConfigureForTests("zone", "t", items);
            return zone;
        }

        private WorldObjectTaskEntity Point(
            string itemId, out InspectionMarker marker, out InspectionHighlight highlight)
        {
            GameObject go = new GameObject(itemId, typeof(SphereCollider));
            WorldObjectTaskEntity entity = go.AddComponent<WorldObjectTaskEntity>();
            entity.Configure(new EntityId("module01.zone." + itemId), itemId);

            GameObject billboard = new GameObject("Marker");
            billboard.transform.SetParent(go.transform, false);
            marker = go.AddComponent<InspectionMarker>();
            marker.ConfigureForTests(null, billboard.transform);

            highlight = go.AddComponent<InspectionHighlight>();
            highlight.ConfigureForTests(_material, Color.red, Color.green);
            return entity;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_bike);
            Object.DestroyImmediate(_scooter);
            if (_steering != null) Object.DestroyImmediate(_steering.gameObject);
            if (_lights != null) Object.DestroyImmediate(_lights.gameObject);
            Object.DestroyImmediate(_ebike);
            Object.DestroyImmediate(_escooter);
            foreach (SafetyItemSO item in _zone.Items)
                Object.DestroyImmediate(item);
            Object.DestroyImmediate(_zone);
            Object.DestroyImmediate(_material);
            Object.DestroyImmediate(_mesh);
        }

        [Test]
        public void Encender_el_vehiculo_delinea_la_pieza_que_declara_su_perfil()
        {
            _stage.Show(_ebike);

            Assert.IsNotNull(_steeringHighlight.Outline);
            Assert.AreSame(_manubrio.transform, _steeringHighlight.Outline.transform.parent);
        }

        /// <summary>
        /// Cindy, 2026-10-21, recorriendo la experiencia: encontrar con la vista todas las piezas
        /// delineadas costaba. El signo vuelve junto al contorno, y se puede pulsar cualquiera
        /// de los dos.
        /// </summary>
        [Test]
        public void Un_punto_con_pieza_tambien_lleva_su_signo_para_pulsarlo()
        {
            _stage.Show(_ebike);
            _steeringMarker.SetHighlighted(true);

            Assert.IsNotNull(_steeringHighlight.Outline, "la pieza sigue delineada");
            Assert.IsTrue(_steeringMarker.IsVisible, "y el signo la acompaña");
        }

        /// <summary>
        /// El signo va con la esfera, sobre la pieza delineada. Se probó a dejarlo sobre su mount
        /// (2026-10-21) y en la bici el de ruedas caía junto a la rueda delantera, debajo del
        /// checklist: la tabla de mounts se escribió con el morro al otro lado, y solo la esfera
        /// —que va a la pieza— lo corregía.
        /// </summary>
        [Test]
        public void El_signo_va_con_su_esfera_sobre_la_pieza()
        {
            Vector3 mount = new Vector3(5f, 0f, 0f);
            _bike.transform.Find("mount_point_steering").position = mount;

            _stage.Show(_ebike);

            Assert.Greater(Vector3.Distance(mount, _steering.transform.position), .5f, "la esfera fue a la pieza");
            Assert.Less(Vector3.Distance(_steering.transform.position, _steeringMarker.SignPosition), .001f,
                        "y el signo con ella");
        }

        [Test]
        public void Cambiar_de_vehiculo_devuelve_el_signo_sobre_su_punto()
        {
            _bike.transform.Find("mount_point_steering").position = new Vector3(5f, 0f, 0f);
            _scooter.transform.Find("mount_point_steering").position = new Vector3(0f, 0f, 2f);
            _stage.Show(_ebike);

            _stage.Show(_escooter);

            Assert.Less(Vector3.Distance(new Vector3(0f, 0f, 2f), _steeringMarker.SignPosition), .001f,
                "sin pieza, el signo vuelve a ir con su punto: si no, se quedaba donde lo dejó la bici");
        }

        /// <summary>Lo revisado se queda en verde también en el signo: ayuda a ver cuáles van ya.</summary>
        [Test]
        public void Revisado_el_signo_de_un_punto_con_pieza_queda_en_verde()
        {
            _stage.Show(_ebike);
            _steeringMarker.SetHighlighted(true);
            _steeringMarker.SetResolved(true);
            _steeringHighlight.SetResolved(true);

            // El verde del signo lo fijan los tests de InspectionMarker (aquí no lleva glifo).
            Assert.IsTrue(_steeringMarker.IsVisible, "revisado, el signo se queda a la vista");
            Assert.IsTrue(_steeringMarker.IsResolved);
            Assert.AreEqual(Color.green, _steeringHighlight.CurrentColor, "y el contorno, en el verde de resuelto");
        }

        [Test]
        public void Un_punto_sin_pieza_conserva_su_signo()
        {
            _stage.Show(_ebike);
            _lightsMarker.SetHighlighted(true);

            Assert.IsTrue(_lightsMarker.IsVisible,
                "Freno, batería y faro no existen como pieza: ahí el signo sigue siendo lo único.");
        }

        [Test]
        public void La_esfera_clicable_se_cuelga_de_la_pieza()
        {
            _stage.Show(_ebike);

            Assert.AreSame(_manubrio.transform, _steering.transform.parent,
                "La esfera cuelga de la pieza para girar con ella.");
            Assert.IsTrue(_steering.GetComponent<SphereCollider>().enabled,
                "La esfera sigue siendo el blanco: es el unico collider del punto.");
        }

        /// <summary>
        /// Si el mount cae fuera de la pieza, la esfera se arrima a ella: lo que brilla y lo
        /// que se pulsa tienen que ser lo mismo.
        /// </summary>
        [Test]
        public void Una_esfera_lejos_de_su_pieza_se_arrima_a_ella()
        {
            _bike.transform.Find("mount_point_steering").position = new Vector3(5f, 0f, 0f);

            _stage.Show(_ebike);

            Bounds box = _manubrio.GetComponent<Renderer>().bounds;
            Assert.Less(Vector3.Distance(box.ClosestPoint(_steering.transform.position),
                                         _steering.transform.position), .001f,
                "La esfera acabo fuera de la pieza que delinea.");
        }

        /// <summary>
        /// Y si ya caía dentro, no se mueve. Los mounts se ajustaron a ojo contra capturas
        /// —la rueda subió de .13 a .18 para no salirse del encuadre, y el de llantas se cruzó
        /// al otro lado para no quedar debajo del checklist—; recolocarlos "al centro de la
        /// pieza" tira ese trabajo y vuelve a meter el punto debajo de la UI.
        /// </summary>
        [Test]
        public void Una_esfera_que_ya_cae_en_su_pieza_no_se_mueve()
        {
            Vector3 dentro = _manubrio.GetComponent<Renderer>().bounds.center;
            _bike.transform.Find("mount_point_steering").position = dentro;

            _stage.Show(_ebike);

            Assert.Less(Vector3.Distance(dentro, _steering.transform.position), .001f,
                "Mover un punto que ya estaba bien deshace un ajuste hecho a ojo.");
        }

        /// <summary>
        /// Lo contrario de lo que se probo primero: NO se le pone collider a la pieza. Un
        /// MeshCollider en 'manubrio' —que es todo el tren delantero— entierra las esferas de
        /// los puntos que cuelgan por detras, y el recorrido de PlayMode lo caza con un "clic
        /// bloqueado por manubrio".
        /// </summary>
        [Test]
        public void Delinear_no_le_pone_collider_a_la_pieza()
        {
            _stage.Show(_ebike);

            Assert.IsNull(_manubrio.GetComponent<Collider>(),
                "Un collider sobre la pieza tapa los puntos que quedan detras de ella.");
        }

        /// <summary>
        /// El vehículo al que se cambia no declara pieza para ese punto. El contorno de la
        /// bici tiene que irse —si no, se queda colgando de una malla apagada— y el signo del
        /// punto tiene que volver.
        /// </summary>
        [Test]
        public void Cambiar_al_patinete_se_lleva_el_contorno_y_devuelve_el_signo()
        {
            _stage.Show(_ebike);

            _stage.Show(_escooter);
            _steeringMarker.SetHighlighted(true);

            Assert.IsNull(_steeringHighlight.Outline);
            Assert.AreEqual(0, _manubrio.transform.childCount,
                "Ni el contorno ni la esfera pueden quedarse colgando de la bici apagada.");
            Assert.IsTrue(_steeringMarker.IsVisible);
            Assert.IsTrue(_steering.GetComponent<SphereCollider>().enabled,
                "La esfera nunca se apaga: es el unico collider del punto.");
        }
    }
}
