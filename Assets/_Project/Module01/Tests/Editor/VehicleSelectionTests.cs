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
