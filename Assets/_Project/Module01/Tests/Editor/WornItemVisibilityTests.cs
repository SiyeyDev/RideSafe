using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// El libro avisa de lo que entra y lo que sale. Sin esto, el mundo no puede reaccionar
    /// a que el aprendiz se ponga o se quite un elemento.
    /// </summary>
    public class SelectionLedgerEventTests
    {
        [Test]
        public void Add_avisa_una_vez_con_el_id()
        {
            SelectionLedger ledger = new SelectionLedger();
            List<string> added = new List<string>();
            ledger.Added += added.Add;

            ledger.Add("helmet_ok");

            CollectionAssert.AreEqual(new[] { "helmet_ok" }, added);
        }

        [Test]
        public void Add_repetido_no_vuelve_a_avisar()
        {
            SelectionLedger ledger = new SelectionLedger();
            int added = 0;
            ledger.Added += _ => added++;

            ledger.Add("helmet_ok");
            ledger.Add("helmet_ok");

            Assert.AreEqual(1, added, "El id ya estaba: avisar otra vez lo escondería dos veces.");
        }

        [Test]
        public void Remove_avisa_solo_si_estaba()
        {
            SelectionLedger ledger = new SelectionLedger();
            List<string> removed = new List<string>();
            ledger.Removed += removed.Add;

            ledger.Add("helmet_ok");
            ledger.Remove("helmet_ok");
            ledger.Remove("helmet_ok");

            CollectionAssert.AreEqual(new[] { "helmet_ok" }, removed);
        }

        [Test]
        public void Clear_avisa_una_vez_y_deja_el_libro_vacio()
        {
            SelectionLedger ledger = new SelectionLedger();
            int cleared = 0;
            ledger.Cleared += () => cleared++;

            ledger.Add("helmet_ok");
            ledger.Add("shoes_ok");
            ledger.Clear();

            Assert.AreEqual(1, cleared);
            Assert.AreEqual(0, ledger.Selected.Count);
        }

        [Test]
        public void Los_ids_llegan_recortados_igual_que_se_guardan()
        {
            SelectionLedger ledger = new SelectionLedger();
            List<string> added = new List<string>();
            ledger.Added += added.Add;

            ledger.Add("  helmet_ok  ");

            CollectionAssert.AreEqual(new[] { "helmet_ok" }, added);
        }
    }

    /// <summary>
    /// Lo que la reunión pidió en §3: al girar un EPP desaparece su gemelo de la mesa, y
    /// al añadirlo al checklist desaparece del todo —como si el aprendiz ya lo llevara—
    /// y vuelve si lo quita.
    /// </summary>
    public class WornItemVisibilityTests : CatalogFixture
    {
        private GameObject _host;
        private WornItemVisibility _visibility;
        private SelectionLedger _ledger;
        private readonly List<GameObject> _props = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("visibility");
            _visibility = _host.AddComponent<WornItemVisibility>();
            _ledger = new SelectionLedger();
        }

        [TearDown]
        public void TearDownHost()
        {
            Object.DestroyImmediate(_host);
            foreach (GameObject p in _props)
                if (p != null)
                    Object.DestroyImmediate(p);
            _props.Clear();
        }

        private GameObject Prop(string itemId)
        {
            GameObject go = new GameObject(itemId);
            _props.Add(go);
            return go;
        }

        private void Configure(params string[] itemIds)
        {
            Dictionary<string, GameObject> world = new Dictionary<string, GameObject>();
            foreach (string id in itemIds)
                world[id] = Prop(id);
            _visibility.ConfigureForTests(_ledger, world);
        }

        [Test]
        public void Al_presentar_un_elemento_se_esconde_el_de_la_mesa()
        {
            Configure("helmet_ok");

            _visibility.BeginInspecting("helmet_ok");

            Assert.IsFalse(_visibility.WorldObject("helmet_ok").activeSelf,
                "Se ve el que gira sobre el pedestal y el de la mesa a la vez: son dos cascos.");
        }

        [Test]
        public void Al_cerrar_sin_aceptar_el_de_la_mesa_vuelve()
        {
            Configure("helmet_ok");

            _visibility.BeginInspecting("helmet_ok");
            _visibility.EndInspecting();

            Assert.IsTrue(_visibility.WorldObject("helmet_ok").activeSelf);
        }

        [Test]
        public void Aceptar_lo_deja_escondido_aunque_se_cierre_la_presentacion()
        {
            Configure("helmet_ok");

            _visibility.BeginInspecting("helmet_ok");
            _ledger.Add("helmet_ok");
            _visibility.EndInspecting();

            Assert.IsFalse(_visibility.WorldObject("helmet_ok").activeSelf,
                "Ya lo lleva puesto: no puede seguir sobre la mesa.");
        }

        [Test]
        public void Quitarlo_del_checklist_lo_devuelve_a_la_mesa()
        {
            Configure("helmet_ok");

            _ledger.Add("helmet_ok");
            _ledger.Remove("helmet_ok");

            Assert.IsTrue(_visibility.WorldObject("helmet_ok").activeSelf);
        }

        [Test]
        public void El_orden_de_aceptar_y_cerrar_no_cambia_el_resultado()
        {
            Configure("helmet_ok");

            // Al revés que en el caso normal: primero se cierra, luego entra en el libro.
            _visibility.BeginInspecting("helmet_ok");
            _visibility.EndInspecting();
            _ledger.Add("helmet_ok");

            Assert.IsFalse(_visibility.WorldObject("helmet_ok").activeSelf);
        }

        [Test]
        public void Limpiar_el_libro_devuelve_todo_a_la_mesa()
        {
            Configure("helmet_ok", "shoes_ok");

            _ledger.Add("helmet_ok");
            _ledger.Add("shoes_ok");
            _ledger.Clear();

            Assert.IsTrue(_visibility.WorldObject("helmet_ok").activeSelf);
            Assert.IsTrue(_visibility.WorldObject("shoes_ok").activeSelf);
        }

        [Test]
        public void Presentar_un_elemento_no_toca_a_los_demas()
        {
            Configure("helmet_ok", "shoes_ok");

            _visibility.BeginInspecting("helmet_ok");

            Assert.IsTrue(_visibility.WorldObject("shoes_ok").activeSelf);
        }

        [Test]
        public void Un_id_sin_objeto_en_la_escena_no_revienta()
        {
            Configure("helmet_ok");

            Assert.DoesNotThrow(() => _visibility.BeginInspecting("no_existe"));
            Assert.DoesNotThrow(() => _ledger.Add("no_existe"));
            Assert.IsNull(_visibility.WorldObject("no_existe"));
        }
    }
}
