using System.Collections.Generic;
using NUnit.Framework;
using RideSafe.UI;

namespace RideSafe.Module01.Tests
{
    public class ReportPresenterTests : CatalogFixture
    {
        private static string Fake(string key) => "[" + key + "]";

        [Test]
        public void Solo_aparece_lo_que_el_aprendiz_se_llevo()
        {
            // Lo pidio Charlie el 2026-10-16: la lista es de lo ELEGIDO. Listar lo omitido
            // convierte el resumen en un boletin de fallos; el porque se explica al final.
            Module01CatalogSO catalog = Catalog(Zone("head",
                Item("helmet_ok", SafetyItemCategory.Core),
                Item("glasses", SafetyItemCategory.Optional),
                Item("shoes_ok", SafetyItemCategory.Core)));
            SelectionLedger ledger = new SelectionLedger();
            ledger.Add("helmet_ok");

            List<ComparisonEntry> entries = ReportPresenter.BuildEntries(
                catalog, SelectionGrader.Grade(catalog, ledger), Fake);

            Assert.AreEqual(1, entries.Count, "Ni el opcional ni el nucleo omitido pintan nada.");
            Assert.AreEqual("helmet_ok", entries[0].Detail);
        }

        [Test]
        public void Lo_de_nucleo_que_se_llevo_sale_como_correcto()
        {
            Module01CatalogSO catalog = Catalog(Zone("head", Item("helmet_ok", SafetyItemCategory.Core)));
            SelectionLedger ledger = new SelectionLedger();
            ledger.Add("helmet_ok");

            List<ComparisonEntry> entries = ReportPresenter.BuildEntries(
                catalog, SelectionGrader.Grade(catalog, ledger), Fake);

            Assert.AreEqual(ItemStatus.Selected, Find(entries, "helmet_ok").Status);
        }

        [Test]
        public void Lo_inapropiado_que_se_llevo_sale_como_incorrecto()
        {
            // Deteriorado o no: los dos son "esto no". El cliente pidio tres estados y ya.
            SafetyItemSO roto = Item("helmet_cracked", SafetyItemCategory.Inappropriate);
            roto.MarkDeterioratedForTests();
            Module01CatalogSO catalog = Catalog(Zone("head",
                roto, Item("phone", SafetyItemCategory.Inappropriate)));
            SelectionLedger ledger = new SelectionLedger();
            ledger.Add("helmet_cracked");
            ledger.Add("phone");

            List<ComparisonEntry> entries = ReportPresenter.BuildEntries(
                catalog, SelectionGrader.Grade(catalog, ledger), Fake);

            Assert.AreEqual(ItemStatus.NotSuited, Find(entries, "helmet_cracked").Status);
            Assert.AreEqual(ItemStatus.NotSuited, Find(entries, "phone").Status);
        }

        [Test]
        public void Lo_opcional_y_lo_de_condicion_salen_como_que_no_afecta()
        {
            Module01CatalogSO catalog = Catalog(Zone("head",
                Item("glasses", SafetyItemCategory.Optional),
                Item("reflective", SafetyItemCategory.ConditionDependent)));
            SelectionLedger ledger = new SelectionLedger();
            ledger.Add("glasses");
            ledger.Add("reflective");

            List<ComparisonEntry> entries = ReportPresenter.BuildEntries(
                catalog, SelectionGrader.Grade(catalog, ledger), Fake);

            Assert.AreEqual(ItemStatus.NoEffect, Find(entries, "glasses").Status);
            Assert.AreEqual(ItemStatus.NoEffect, Find(entries, "reflective").Status);
        }

        [Test]
        public void Los_titulos_salen_traducidos_no_con_la_clave_cruda()
        {
            Module01CatalogSO catalog = Catalog(Zone("head", Item("helmet_ok", SafetyItemCategory.Core)));
            SelectionLedger ledger = new SelectionLedger();
            ledger.Add("helmet_ok");

            SelectionReport report = SelectionGrader.Grade(catalog, ledger);
            List<ComparisonEntry> entries = ReportPresenter.BuildEntries(catalog, report, Fake);

            Assert.AreEqual("[M1/helmet_ok]", entries[0].Title);
        }

        [Test]
        public void Cambiar_de_idioma_repinta_con_la_nueva_traduccion()
        {
            Module01CatalogSO catalog = Catalog(Zone("head", Item("helmet_ok", SafetyItemCategory.Core)));
            SelectionLedger ledger = new SelectionLedger();
            ledger.Add("helmet_ok");
            SelectionReport report = SelectionGrader.Grade(catalog, ledger);

            List<ComparisonEntry> english = ReportPresenter.BuildEntries(catalog, report, k => "EN:" + k);
            List<ComparisonEntry> spanish = ReportPresenter.BuildEntries(catalog, report, k => "ES:" + k);

            Assert.AreEqual("EN:M1/helmet_ok", english[0].Title);
            Assert.AreEqual("ES:M1/helmet_ok", spanish[0].Title);
        }

        private static ComparisonEntry Find(List<ComparisonEntry> entries, string itemId)
        {
            foreach (ComparisonEntry entry in entries)
            {
                if (entry.Detail == itemId)
                    return entry;
            }
            Assert.Fail("No se encontro la entrada de " + itemId);
            return default;
        }
    }
}
