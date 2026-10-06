using NUnit.Framework;
using UnityEngine;
using RideSafe.TaskSequence;

namespace RideSafe.Module01.Tests
{
    public class ZoneSequenceTests
    {
        private TaskSequenceSO _sequence;

        [SetUp]
        public void SetUp() => _sequence = ScriptableObject.CreateInstance<TaskSequenceSO>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_sequence);

        [Test]
        public void Sin_requisitos_la_secuencia_corre_en_cualquier_contexto()
        {
            Module01Context context = new Module01Context();

            Assert.IsTrue(_sequence.MatchesContext(context.Lookup, out string unmet));
            Assert.IsNull(unmet);
        }

        [Test]
        public void El_contexto_vacio_devuelve_cadena_no_null_para_el_vehiculo()
        {
            // Un requisito vehicle == ebike contra un contexto sin publicar debe
            // simplemente no cumplirse, no reventar.
            Module01Context context = new Module01Context();

            Assert.AreEqual(string.Empty, context.Lookup(Module01Context.VehicleKey));
        }

        [Test]
        public void Publicar_otro_vehiculo_no_satisface_el_requisito_del_ebike()
        {
            Module01Context context = new Module01Context();
            context.Set(Module01Context.VehicleKey, "escooter");

            Assert.AreNotEqual("ebike", context.Lookup(Module01Context.VehicleKey));
        }
    }

    public class ZoneRunnerTests : CatalogFixture
    {
        private GameObject _go;
        private ZoneRunner _runner;
        private ItemInspector _inspector;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("zone");
            _inspector = _go.AddComponent<ItemInspector>();
            _runner = _go.AddComponent<ZoneRunner>();
            _runner.ConfigureForTests(_inspector, null);
        }

        [TearDown]
        public void TearDownRunner() => Object.DestroyImmediate(_go);

        [Test]
        public void Aceptar_un_elemento_lo_registra_en_el_libro()
        {
            SafetyItemSO item = Item("helmet_ok", SafetyItemCategory.Core);
            ZoneSO zone = Zone("head", item);
            _runner.Begin(zone);

            _inspector.Present(item);
            _inspector.Accept();

            Assert.IsTrue(_runner.Ledger.Contains("helmet_ok"));
        }

        [Test]
        public void Rechazar_un_elemento_no_lo_registra()
        {
            SafetyItemSO item = Item("helmet_damaged", SafetyItemCategory.Inappropriate);
            ZoneSO zone = Zone("head", item);
            _runner.Begin(zone);

            _inspector.Present(item);
            _inspector.Decline();

            Assert.IsFalse(_runner.Ledger.Contains("helmet_damaged"));
        }

        [Test]
        public void La_zona_termina_cuando_todos_sus_elementos_quedan_resueltos()
        {
            SafetyItemSO good = Item("helmet_ok", SafetyItemCategory.Core);
            SafetyItemSO bad = Item("helmet_damaged", SafetyItemCategory.Inappropriate);
            ZoneSO zone = Zone("head", good, bad);
            int completed = 0;
            _runner.ZoneCompleted += z => completed++;
            _runner.Begin(zone);

            _inspector.Present(good);
            _inspector.Accept();
            Assert.AreEqual(0, completed, "Todavia falta resolver un elemento.");

            _inspector.Present(bad);
            _inspector.Decline();
            Assert.AreEqual(1, completed);
        }

        [Test]
        public void Resolver_el_mismo_elemento_dos_veces_no_cierra_la_zona_antes_de_tiempo()
        {
            SafetyItemSO good = Item("helmet_ok", SafetyItemCategory.Core);
            SafetyItemSO other = Item("glasses", SafetyItemCategory.Optional);
            ZoneSO zone = Zone("head", good, other);
            int completed = 0;
            _runner.ZoneCompleted += z => completed++;
            _runner.Begin(zone);

            _inspector.Present(good);
            _inspector.Accept();
            _inspector.Present(good);
            _inspector.Decline();

            Assert.AreEqual(0, completed, "Solo se resolvio un elemento distinto.");
        }

        [Test]
        public void Begin_en_otra_zona_limpia_lo_resuelto_pero_conserva_el_libro()
        {
            SafetyItemSO head = Item("helmet_ok", SafetyItemCategory.Core);
            SafetyItemSO load = Item("cargo_secured", SafetyItemCategory.Core);
            _runner.Begin(Zone("head", head));
            _inspector.Present(head);
            _inspector.Accept();

            _runner.Begin(Zone("load", load));

            Assert.IsTrue(_runner.Ledger.Contains("helmet_ok"), "El libro acumula entre zonas.");
            Assert.IsFalse(_runner.Ledger.Contains("cargo_secured"));
        }
    }
}
