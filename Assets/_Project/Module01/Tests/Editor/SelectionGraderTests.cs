using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Base de los tests que necesitan catálogo. Los ScriptableObject se crean en
    /// memoria y se destruyen al terminar, así ningún test toca el disco.
    /// </summary>
    public abstract class CatalogFixture
    {
        private readonly List<Object> _created = new List<Object>();

        protected SafetyItemSO Item(string id, SafetyItemCategory category)
        {
            SafetyItemSO item = ScriptableObject.CreateInstance<SafetyItemSO>();
            item.ConfigureForTests(id, "Module1/" + id, "Module1/" + id + "_Risk", category);
            _created.Add(item);
            return item;
        }

        protected ZoneSO Zone(string id, params SafetyItemSO[] items)
        {
            ZoneSO zone = ScriptableObject.CreateInstance<ZoneSO>();
            zone.ConfigureForTests(id, "Module1/Zone_" + id, items);
            _created.Add(zone);
            return zone;
        }

        protected Module01CatalogSO Catalog(params ZoneSO[] zones)
        {
            Module01CatalogSO catalog = ScriptableObject.CreateInstance<Module01CatalogSO>();
            catalog.ConfigureForTests(zones);
            _created.Add(catalog);
            return catalog;
        }

        [TearDown]
        public void DestroyCreatedAssets()
        {
            foreach (Object o in _created)
                Object.DestroyImmediate(o);
            _created.Clear();
        }
    }

    public class CatalogTests : CatalogFixture
    {
        [Test]
        public void AllItems_devuelve_los_elementos_en_orden_de_zona()
        {
            Module01CatalogSO catalog = Catalog(
                Zone("head", Item("helmet_ok", SafetyItemCategory.Core)),
                Zone("load", Item("bag_ok", SafetyItemCategory.Core)));

            CollectionAssert.AreEqual(
                new[] { "helmet_ok", "bag_ok" },
                new List<SafetyItemSO>(catalog.AllItems).ConvertAll(i => i.ItemId));
        }

        [Test]
        public void TryGetItem_encuentra_por_id_y_falla_con_id_desconocido()
        {
            Module01CatalogSO catalog = Catalog(Zone("head", Item("helmet_ok", SafetyItemCategory.Core)));

            Assert.IsTrue(catalog.TryGetItem("helmet_ok", out SafetyItemSO found));
            Assert.AreEqual("helmet_ok", found.ItemId);
            Assert.IsFalse(catalog.TryGetItem("no_existe", out _));
        }

        [Test]
        public void Validate_reporta_ids_duplicados()
        {
            Module01CatalogSO catalog = Catalog(
                Zone("head", Item("helmet_ok", SafetyItemCategory.Core)),
                Zone("load", Item("helmet_ok", SafetyItemCategory.Core)));

            List<string> problems = catalog.Validate();

            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains("helmet_ok", problems[0]);
        }
    }

    public class SelectionGraderTests : CatalogFixture
    {
        private Module01CatalogSO StandardCatalog() => Catalog(
            Zone("head",
                Item("helmet_ok", SafetyItemCategory.Core),
                Item("helmet_cracked", SafetyItemCategory.Inappropriate),
                Item("glasses", SafetyItemCategory.Optional)),
            Zone("clothing",
                Item("shoes_ok", SafetyItemCategory.Core),
                Item("laces_loose", SafetyItemCategory.Inappropriate),
                Item("reflective", SafetyItemCategory.ConditionDependent)));

        [Test]
        public void Elegir_un_opcional_no_penaliza()
        {
            SelectionLedger ledger = new SelectionLedger();
            ledger.Add("helmet_ok");
            ledger.Add("shoes_ok");
            ledger.Add("glasses");

            SelectionReport report = SelectionGrader.Grade(StandardCatalog(), ledger);

            CollectionAssert.AreEquivalent(new[] { "glasses" }, report.OptionalSelected);
            CollectionAssert.IsEmpty(report.InappropriateSelected);
            CollectionAssert.IsEmpty(report.CoreOmitted);
            Assert.IsFalse(report.HasCriticalProblem);
        }

        [Test]
        public void Omitir_un_opcional_tampoco_penaliza()
        {
            SelectionLedger ledger = new SelectionLedger();
            ledger.Add("helmet_ok");
            ledger.Add("shoes_ok");

            SelectionReport report = SelectionGrader.Grade(StandardCatalog(), ledger);

            CollectionAssert.IsEmpty(report.OptionalSelected);
            CollectionAssert.IsEmpty(report.CoreOmitted);
            Assert.IsFalse(report.HasCriticalProblem);
        }

        [Test]
        public void Omitir_un_elemento_nucleo_es_problema_critico()
        {
            SelectionLedger ledger = new SelectionLedger();
            ledger.Add("helmet_ok");

            SelectionReport report = SelectionGrader.Grade(StandardCatalog(), ledger);

            CollectionAssert.AreEquivalent(new[] { "shoes_ok" }, report.CoreOmitted);
            Assert.IsTrue(report.HasCriticalProblem);
        }

        [Test]
        public void Elegir_un_inapropiado_es_problema_critico()
        {
            SelectionLedger ledger = new SelectionLedger();
            ledger.Add("helmet_ok");
            ledger.Add("shoes_ok");
            ledger.Add("laces_loose");

            SelectionReport report = SelectionGrader.Grade(StandardCatalog(), ledger);

            CollectionAssert.AreEquivalent(new[] { "laces_loose" }, report.InappropriateSelected);
            Assert.IsTrue(report.HasCriticalProblem);
        }

        [Test]
        public void El_condicional_se_reporta_aparte_y_no_es_critico()
        {
            SelectionLedger ledger = new SelectionLedger();
            ledger.Add("helmet_ok");
            ledger.Add("shoes_ok");
            ledger.Add("reflective");

            SelectionReport report = SelectionGrader.Grade(StandardCatalog(), ledger);

            CollectionAssert.AreEquivalent(new[] { "reflective" }, report.ConditionDependentSelected);
            CollectionAssert.IsEmpty(report.InappropriateSelected);
            Assert.IsFalse(report.HasCriticalProblem);
        }

        [Test]
        public void Quitar_antes_de_enviar_deja_el_elemento_fuera_del_reporte()
        {
            SelectionLedger ledger = new SelectionLedger();
            ledger.Add("helmet_ok");
            ledger.Add("shoes_ok");
            ledger.Add("laces_loose");
            ledger.Remove("laces_loose");

            SelectionReport report = SelectionGrader.Grade(StandardCatalog(), ledger);

            CollectionAssert.IsEmpty(report.InappropriateSelected);
            Assert.IsFalse(report.HasCriticalProblem);
        }

        [Test]
        public void Un_id_desconocido_en_el_libro_se_ignora_sin_reventar()
        {
            SelectionLedger ledger = new SelectionLedger();
            ledger.Add("helmet_ok");
            ledger.Add("shoes_ok");
            ledger.Add("fantasma");

            SelectionReport report = SelectionGrader.Grade(StandardCatalog(), ledger);

            Assert.IsFalse(report.HasCriticalProblem);
            CollectionAssert.DoesNotContain(report.OptionalSelected, "fantasma");
        }

        [Test]
        public void Agregar_dos_veces_el_mismo_elemento_no_lo_duplica()
        {
            SelectionLedger ledger = new SelectionLedger();
            ledger.Add("helmet_ok");
            ledger.Add("helmet_ok");

            Assert.AreEqual(1, ledger.Selected.Count);
        }
    }
}
