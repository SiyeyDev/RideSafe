using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Lo que el aprendiz elige en el panel Vehicle del módulo 0 tiene que decidir qué
    /// perfil usa el módulo 1. El director tenía un solo <see cref="VehicleProfileSO"/>
    /// serializado, fijo en el de la e-bike, así que elegir patinete recorría las zonas
    /// de la bici. Hoy los dos perfiles comparten las 3 zonas, pero eso es contenido:
    /// estos tests guardan que el director honre la elección, no la coincidencia.
    /// </summary>
    public class VehicleSelectionTests : CatalogFixture
    {
        private GameObject _go;
        private GameObject _rig;
        private Module01Director _director;
        private ItemInspector _inspector;
        private ZoneRunner _runner;
        private GuidedTour _tour;

        private SafetyItemSO _helmet;
        private SafetyItemSO _shoes;
        private VehicleProfileSO _ebike;
        private VehicleProfileSO _escooter;

        [SetUp]
        public void SetUp()
        {
            _rig = new GameObject("rig");
            _go = new GameObject("module01");
            _inspector = _go.AddComponent<ItemInspector>();
            _runner = _go.AddComponent<ZoneRunner>();
            _runner.ConfigureForTests(_inspector, null);
            _tour = _go.AddComponent<GuidedTour>();
            _tour.ConfigureForTests(_rig.transform);
            _director = _go.AddComponent<Module01Director>();

            _helmet = Item("helmet_ok", SafetyItemCategory.Core);
            _shoes = Item("shoes_ok", SafetyItemCategory.Core);

            _ebike = Profile("ebike", Zone("cockpit_ebike", Item("point_brakes", SafetyItemCategory.Core)));
            _escooter = Profile("escooter", Zone("cockpit_escooter", Item("point_brakes_scooter", SafetyItemCategory.Core)));
        }

        [TearDown]
        public void TearDownSelection()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_rig);
            Object.DestroyImmediate(_ebike);
            Object.DestroyImmediate(_escooter);
        }

        private VehicleProfileSO Profile(string contextValue, ZoneSO zone)
        {
            VehicleProfileSO profile = ScriptableObject.CreateInstance<VehicleProfileSO>();
            profile.Configure(contextValue, null, new List<ZoneSO> { zone });
            return profile;
        }

        /// <summary>Resuelve las dos zonas personales para llegar a la sección del vehículo.</summary>
        private void ReachVehicleSection()
        {
            _director.Begin();
            _inspector.Present(_helmet);
            _inspector.Accept();
            _inspector.Present(_shoes);
            _inspector.Accept();
        }

        private void Wire()
        {
            _director.ConfigureForTests(
                Catalog(Zone("head", _helmet), Zone("clothing", _shoes)),
                new List<VehicleProfileSO> { _ebike, _escooter },
                _tour, _runner, null);
        }

        [Test]
        public void Sin_elegir_vehiculo_recorre_el_primero_de_la_lista()
        {
            Wire();

            ReachVehicleSection();

            Assert.AreEqual("cockpit_ebike", _director.CurrentZone.ZoneId,
                "Sin elección, el primer perfil de la lista es el que vale.");
        }

        [Test]
        public void Elegir_el_escooter_recorre_sus_zonas_y_no_las_de_la_ebike()
        {
            Wire();
            _director.SelectVehicle("escooter");

            ReachVehicleSection();

            Assert.AreEqual("cockpit_escooter", _director.CurrentZone.ZoneId,
                "Elegir patinete no puede llevar al aprendiz por las zonas de la bici.");
        }

        [Test]
        public void Elegir_la_ebike_recorre_sus_zonas_y_no_las_del_escooter()
        {
            Wire();
            _director.SelectVehicle("escooter");
            _director.SelectVehicle("ebike");

            ReachVehicleSection();

            Assert.AreEqual("cockpit_ebike", _director.CurrentZone.ZoneId);
        }

        [Test]
        public void El_valor_del_contexto_se_compara_normalizado()
        {
            Wire();

            _director.SelectVehicle("  EScooter  ");

            Assert.AreSame(_escooter, _director.SelectedVehicle,
                "El perfil normaliza su clave; la elección tiene que compararse igual.");
        }

        [Test]
        public void Un_vehiculo_desconocido_avisa_y_conserva_la_seleccion()
        {
            Wire();
            _director.SelectVehicle("escooter");

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("monociclo"));
            _director.SelectVehicle("monociclo");

            Assert.AreSame(_escooter, _director.SelectedVehicle,
                "Un vehículo sin perfil no puede dejar al módulo sin sección de vehículo.");
        }

        [Test]
        public void Elegir_vehiculo_enciende_el_suyo_y_apaga_el_otro()
        {
            Wire();
            GameObject bike = new GameObject("ebike");
            GameObject scooter = new GameObject("scooter");
            VehicleStage stage = _go.AddComponent<VehicleStage>();
            stage.ConfigureForTests(("ebike", bike), ("escooter", scooter));
            _director.ConfigureStageForTests(stage);

            _director.SelectVehicle("escooter");

            Assert.IsTrue(scooter.activeSelf, "El vehículo elegido tiene que estar encendido.");
            Assert.IsFalse(bike.activeSelf, "El otro vehículo tiene que apagarse.");

            Object.DestroyImmediate(bike);
            Object.DestroyImmediate(scooter);
        }
    }


    /// <summary>
    /// El aprendiz se queda quieto y es el vehículo el que gira para enseñar cada zona:
    /// de costado para las ruedas, de frente para las luces. La pose es dato por zona y
    /// por vehículo, no código, porque hay que ajustarla a ojo contra cada malla.
    /// </summary>
    public class VehiclePoseTests : CatalogFixture
    {
        private VehicleProfileSO _profile;
        private ZoneSO _wheels;
        private ZoneSO _lights;

        [SetUp]
        public void SetUp()
        {
            _wheels = Zone("wheels", Item("point_wheels", SafetyItemCategory.Core));
            _lights = Zone("visibility", Item("point_lights", SafetyItemCategory.Core));
            _profile = ScriptableObject.CreateInstance<VehicleProfileSO>();
            _profile.Configure("ebike", null, new List<ZoneSO> { _wheels, _lights });
        }

        [TearDown]
        public void TearDownPose() => Object.DestroyImmediate(_profile);

        [Test]
        public void Cada_zona_puede_declarar_su_propio_giro()
        {
            _profile.ConfigureZoneViews(new[] { (_wheels, 90f), (_lights, 0f) });

            Assert.AreEqual(90f, _profile.YawFor(_wheels), .01f);
            Assert.AreEqual(0f, _profile.YawFor(_lights), .01f);
        }

        [Test]
        public void Una_zona_sin_pose_declarada_no_gira_en_vez_de_reventar()
        {
            Assert.AreEqual(0f, _profile.YawFor(_wheels), .01f);
            Assert.AreEqual(0f, _profile.YawFor(null), .01f);
        }

        [Test]
        public void Los_dos_vehiculos_pueden_tener_giros_distintos_para_la_misma_zona()
        {
            var scooter = ScriptableObject.CreateInstance<VehicleProfileSO>();
            scooter.Configure("escooter", null, new List<ZoneSO> { _wheels });
            _profile.ConfigureZoneViews(new[] { (_wheels, 90f) });
            scooter.ConfigureZoneViews(new[] { (_wheels, -90f) });

            Assert.AreEqual(90f, _profile.YawFor(_wheels), .01f);
            Assert.AreEqual(-90f, scooter.YawFor(_wheels), .01f);

            Object.DestroyImmediate(scooter);
        }
    }

    public class VehicleStageRotationTests
    {
        private GameObject _go;
        private GameObject _bike;
        private VehicleStage _stage;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("stage");
            _bike = new GameObject("ebike");
            _stage = _go.AddComponent<VehicleStage>();
            _stage.ConfigureForTests(("ebike", _bike));
            _stage.Show("ebike");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_bike);
        }

        [Test]
        public void Encarar_una_zona_gira_el_vehiculo_sobre_su_eje()
        {
            _stage.FaceYaw(90f, immediate: true);

            Assert.AreEqual(90f, _bike.transform.eulerAngles.y, .5f);
        }

        [Test]
        public void El_giro_es_el_pedido_aunque_se_encadenen_zonas()
        {
            _stage.FaceYaw(90f, immediate: true);
            _stage.FaceYaw(-35f, immediate: true);

            Assert.AreEqual(325f, _bike.transform.eulerAngles.y, .5f);
        }

        /// <summary>
        /// Los marcadores cuelgan de la malla, así que al girar el vehículo viajan con él:
        /// eso es justo lo que hace que señalen la pieza y no un punto del aire.
        /// </summary>
        [Test]
        public void Los_marcadores_giran_con_el_vehiculo()
        {
            var mount = new GameObject("mount_point_wheels").transform;
            mount.SetParent(_bike.transform, false);
            mount.localPosition = new Vector3(0f, 0f, 1f);
            var marker = new GameObject("point_wheels").transform;
            marker.SetParent(mount, false);

            _stage.FaceYaw(180f, immediate: true);

            Assert.AreEqual(-1f, marker.position.z, .01f);
        }
    }

    /// <summary>
    /// El escenario es lo único que sabe qué mallas del garaje son vehículos. Separado
    /// del director para que elegir vehículo no obligue a buscar objetos por nombre.
    /// </summary>
    public class VehicleStageTests
    {
        private GameObject _go;
        private GameObject _bike;
        private GameObject _scooter;
        private VehicleStage _stage;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("stage");
            _bike = new GameObject("ebike");
            _scooter = new GameObject("scooter");
            _stage = _go.AddComponent<VehicleStage>();
            _stage.ConfigureForTests(("ebike", _bike), ("escooter", _scooter));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_bike);
            Object.DestroyImmediate(_scooter);
        }

        [Test]
        public void Mostrar_un_vehiculo_apaga_los_demas()
        {
            _stage.Show("ebike");

            Assert.IsTrue(_bike.activeSelf);
            Assert.IsFalse(_scooter.activeSelf);
        }

        [Test]
        public void Mostrar_el_otro_invierte_el_encendido()
        {
            _stage.Show("ebike");
            _stage.Show("escooter");

            Assert.IsFalse(_bike.activeSelf);
            Assert.IsTrue(_scooter.activeSelf);
        }

        [Test]
        public void Un_vehiculo_sin_malla_avisa_y_no_deja_el_garaje_vacio()
        {
            _stage.Show("ebike");

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("monociclo"));
            _stage.Show("monociclo");

            Assert.IsTrue(_bike.activeSelf,
                "Sin malla para lo pedido, mejor dejar lo que había que apagar todo.");
        }
    }
}
