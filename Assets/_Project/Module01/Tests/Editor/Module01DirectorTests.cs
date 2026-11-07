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

        private void ManualProgress()
        {
            var serialized = new UnityEditor.SerializedObject(_director);
            serialized.FindProperty("_manualProgress").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// «Estación anterior» (Cindy, 2026-10-21): en la primera estación de los EPP no hay a
        /// dónde volver —no se regresa al módulo 0— y el botón no hacía nada. Desde las demás sí
        /// vuelve.
        /// </summary>
        [Test]
        public void La_primera_estacion_no_tiene_anterior_y_las_demas_vuelven()
        {
            Wire(out _, out VehicleProfileSO v);
            ManualProgress();
            _director.Begin();
            Assert.IsTrue(_director.IsFirstZone, "head es la primera: sin estacion anterior");

            _director.AdvanceZone();
            Assert.AreEqual("clothing", _director.CurrentZone.ZoneId);
            Assert.IsFalse(_director.IsFirstZone);

            _director.PreviousZone();
            Assert.AreEqual("head", _director.CurrentZone.ZoneId, "desde la segunda se vuelve a la primera");
            Assert.IsTrue(_director.IsFirstZone);

            Object.DestroyImmediate(v);
        }

        /// <summary>
        /// Y la primera del vehículo, después del video: los EPP ya se enviaron y su sección está
        /// cerrada, así que tampoco tiene estación anterior.
        /// </summary>
        [Test]
        public void La_primera_estacion_del_vehiculo_tampoco_tiene_anterior()
        {
            Wire(out _, out VehicleProfileSO v);
            ManualProgress();
            _director.Begin();
            // Se cogen los dos esenciales a propósito: desde la regla del examen (2026-11-04)
            // cerrar la sección sin nada la reprueba y devuelve a la mesa, y entonces este test
            // no estaría midiendo lo suyo —la primera estación del vehículo— sino el reinicio.
            Resolve(_head, accept: true);
            _director.AdvanceZone();
            Resolve(_clothing, accept: true);
            _director.AdvanceZone();
            _director.ContinueAfterReview();

            Assert.AreEqual("cockpit", _director.CurrentZone.ZoneId);
            Assert.IsTrue(_director.IsFirstZone);
            _director.PreviousZone();
            Assert.AreEqual("cockpit", _director.CurrentZone.ZoneId, "no vuelve a la seccion ya enviada");

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

        /// <summary>
        /// Cierra la sección en curso sin coger nada: los dos esenciales quedan omitidos, que es
        /// lo que la regla del examen llama reprobar.
        /// </summary>
        private void CerrarSeccionReprobando()
        {
            _director.AdvanceZone();
            _director.AdvanceZone();
        }

        /// <summary>
        /// La regla del examen (documento del equipo, 2026-11-04): reprobada la sección, el video
        /// es obligatorio y después se vuelve al checkpoint inicial a repetir la selección entera.
        /// <para>
        /// Va en <see cref="Module01Director.ContinueAfterReview"/> y no en un botón aparte porque
        /// ese método es justo lo que se llama <b>al terminar el video</b>: así no hay forma de
        /// saltárselo, que es lo que pide la hoja.
        /// </para>
        /// </summary>
        [Test]
        public void Reprobar_la_seccion_personal_devuelve_a_la_primera_estacion()
        {
            Wire(out _, out VehicleProfileSO v);
            ManualProgress();
            _director.Begin();
            CerrarSeccionReprobando();

            Assert.IsTrue(_director.PersonalScore.FailsExam, "premisa: la sección está reprobada");

            _director.ContinueAfterReview();

            Assert.AreEqual("head", _director.CurrentZone.ZoneId,
                            "tras el video se vuelve a la primera estación, no se pasa al vehículo");

            Object.DestroyImmediate(v);
        }

        /// <summary>
        /// La vuelta nueva se califica sola: repetir bien levanta la nota, sin arrastrar lo de
        /// la vuelta anterior.
        /// <para>
        /// Esto no se puede afirmar mirando solo que el libro quede vacío: el libro ya se vacía
        /// también al pasar a la sección del vehículo, así que esa aserción pasaba sin medir
        /// nada. Lo que distingue el reinicio es que la segunda vuelta se puede jugar y cambia
        /// el resultado.
        /// </para>
        /// </summary>
        [Test]
        public void La_vuelta_despues_de_reprobar_se_califica_sola()
        {
            Wire(out _, out VehicleProfileSO v);
            ManualProgress();
            _director.Begin();
            CerrarSeccionReprobando();
            Assert.IsTrue(_director.PersonalScore.FailsExam, "premisa: la primera vuelta reprueba");

            _director.ContinueAfterReview();

            // Segunda vuelta, esta vez cogiendo los dos esenciales.
            Resolve(_head, accept: true);
            _director.AdvanceZone();
            Resolve(_clothing, accept: true);
            _director.AdvanceZone();

            Assert.IsFalse(_director.PersonalScore.FailsExam,
                           "repetida bien, la sección ya no reprueba");

            Object.DestroyImmediate(v);
        }

        /// <summary>
        /// Con el vehículo sin revisar se repite su sección y no el módulo (PO, 2026-11-06): los EPP
        /// ya aprobaron y su nota se conserva.
        /// </summary>
        [Test]
        public void Reprobar_el_vehiculo_repite_su_seccion_sin_tocar_los_epp()
        {
            Wire(out _, out VehicleProfileSO v);
            ManualProgress();
            _director.Begin();
            Resolve(_head, accept: true);
            _director.AdvanceZone();
            Resolve(_clothing, accept: true);
            _director.AdvanceZone();
            _director.ContinueAfterReview();
            int personal = _director.PersonalScore.Percent;

            for (int i = 0; i < 20 && _director.VehicleReport == null; i++)
                _director.AdvanceZone();
            Assert.IsTrue(_director.VehicleScore.FailsExam, "premisa: no revisó nada");

            _director.ContinueAfterReview();

            Assert.IsTrue(_director.InVehicleSection, "sigue en la sección del vehículo");
            Assert.AreEqual("cockpit", _director.CurrentZone.ZoneId, "desde su primera zona");
            Assert.IsNotNull(_director.PersonalReport, "los EPP no se repiten");
            Assert.AreEqual(personal, _director.PersonalScore.Percent);

            Object.DestroyImmediate(v);
        }

        /// <summary>Y aprobando no cambia nada: se sigue al vehículo como siempre.</summary>
        [Test]
        public void Aprobar_la_seccion_personal_sigue_pasando_al_vehiculo()
        {
            Wire(out _, out VehicleProfileSO v);
            ManualProgress();
            _director.Begin();
            Resolve(_head, accept: true);
            _director.AdvanceZone();
            Resolve(_clothing, accept: true);
            _director.AdvanceZone();

            Assert.IsFalse(_director.PersonalScore.FailsExam, "premisa: con todo cogido aprueba");

            _director.ContinueAfterReview();

            Assert.AreEqual("cockpit", _director.CurrentZone.ZoneId);

            Object.DestroyImmediate(v);
        }
    }
}
