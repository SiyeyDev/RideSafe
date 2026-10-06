using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    public class Module01DirectorTests : CatalogFixture
    {
        private GameObject _go;
        private Module01Director _director;
        private ItemInspector _inspector;
        private ZoneRunner _runner;
        private GuidedTour _tour;
        private GameObject _rig;

        private SafetyItemSO _head, _clothing, _cockpit;

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

            _head = Item("helmet_ok", SafetyItemCategory.Core);
            _clothing = Item("shoes_ok", SafetyItemCategory.Core);
            _cockpit = Item("point_brakes", SafetyItemCategory.Core);
        }

        [TearDown]
        public void TearDownDirector()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_rig);
        }

        private void Wire(out Module01CatalogSO catalog, out VehicleProfileSO vehicle)
        {
            catalog = Catalog(Zone("head", _head), Zone("clothing", _clothing));
            vehicle = ScriptableObject.CreateInstance<VehicleProfileSO>();
            vehicle.Configure("ebike", null, new List<ZoneSO> { Zone("cockpit", _cockpit) });
            _director.ConfigureForTests(catalog, vehicle, _tour, _runner, null);
        }

        /// <summary>Resuelve el elemento que la zona en curso espera.</summary>
        private void Resolve(SafetyItemSO item, bool accept)
        {
            _inspector.Present(item);
            if (accept) _inspector.Accept(); else _inspector.Decline();
        }

        [Test]
        public void Begin_arranca_en_la_primera_zona_del_catalogo()
        {
            Wire(out _, out VehicleProfileSO v);
            _director.Begin();

            Assert.AreEqual("head", _director.CurrentZone.ZoneId);
            Assert.IsFalse(_director.IsFinished);

            Object.DestroyImmediate(v);
        }

        [Test]
        public void Resolver_una_zona_avanza_a_la_siguiente()
        {
            Wire(out _, out VehicleProfileSO v);
            _director.Begin();

            Resolve(_head, accept: true);

            Assert.AreEqual("clothing", _director.CurrentZone.ZoneId);

            Object.DestroyImmediate(v);
        }

        [Test]
        public void Al_cerrar_las_zonas_personales_emite_el_reporte_de_la_seccion()
        {
            Wire(out _, out VehicleProfileSO v);
            SelectionReport personal = null;
            _director.PersonalSectionCompleted += r => personal = r;
            _director.Begin();

            Resolve(_head, accept: true);
            Resolve(_clothing, accept: true);

            Assert.IsNotNull(personal, "Debe emitir el reporte al cerrar la seccion personal.");
            CollectionAssert.AreEquivalent(new[] { "helmet_ok", "shoes_ok" }, personal.CoreSelected);
            Assert.IsFalse(personal.HasCriticalProblem);

            Object.DestroyImmediate(v);
        }

        [Test]
        public void Tras_la_seccion_personal_sigue_con_las_zonas_del_vehiculo()
        {
            Wire(out _, out VehicleProfileSO v);
            _director.Begin();

            Resolve(_head, accept: true);
            Resolve(_clothing, accept: true);

            Assert.AreEqual("cockpit", _director.CurrentZone.ZoneId);
            Assert.IsFalse(_director.IsFinished);

            Object.DestroyImmediate(v);
        }

        [Test]
        public void El_libro_se_reinicia_entre_secciones()
        {
            Wire(out _, out VehicleProfileSO v);
            SelectionReport vehicleReport = null;
            _director.VehicleSectionCompleted += r => vehicleReport = r;
            _director.Begin();

            Resolve(_head, accept: true);
            Resolve(_clothing, accept: true);
            Resolve(_cockpit, accept: true);

            Assert.IsNotNull(vehicleReport);
            CollectionAssert.AreEquivalent(new[] { "point_brakes" }, vehicleReport.CoreSelected,
                "El reporte del vehiculo no debe arrastrar los elementos personales.");
            Assert.IsTrue(_director.IsFinished);

            Object.DestroyImmediate(v);
        }

        [Test]
        public void Omitir_un_elemento_nucleo_llega_al_reporte_como_critico()
        {
            Wire(out _, out VehicleProfileSO v);
            SelectionReport personal = null;
            _director.PersonalSectionCompleted += r => personal = r;
            _director.Begin();

            Resolve(_head, accept: false);
            Resolve(_clothing, accept: true);

            Assert.IsNotNull(personal);
            CollectionAssert.AreEquivalent(new[] { "helmet_ok" }, personal.CoreOmitted);
            Assert.IsTrue(personal.HasCriticalProblem);

            Object.DestroyImmediate(v);
        }
    }
}
