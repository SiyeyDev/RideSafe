using System.Collections.Generic;
using NUnit.Framework;
using RideSafe.UI;

namespace RideSafe.Module01.Tests
{
    public class ReportPresenterTests : CatalogFixture
    {
        private static string Fake(string key) => "[" + key + "]";

        [Test]
        public void Cada_categoria_se_mapea_a_su_estado_visual()
        {
            Module01CatalogSO catalog = Catalog(Zone("head",
                Item("helmet_ok", SafetyItemCategory.Core),
                Item("helmet_cracked", SafetyItemCategory.Inappropriate),
                Item("glasses", SafetyItemCategory.Optional),
                Item("reflective", SafetyItemCategory.ConditionDependent),
                Item("shoes_ok", SafetyItemCategory.Core)));

            SelectionLedger ledger = new SelectionLedger();
            ledger.Add("helmet_ok");
            ledger.Add("helmet_cracked");
            ledger.Add("glasses");
            ledger.Add("reflective");

            SelectionReport report = SelectionGrader.Grade(catalog, ledger);
            List<ComparisonEntry> entries = ReportPresenter.BuildEntries(catalog, report, Fake);

            Assert.AreEqual(ItemStatus.Selected, Find(entries, "helmet_ok").Status);
            Assert.AreEqual(ItemStatus.NotSuited, Find(entries, "helmet_cracked").Status);
            Assert.AreEqual(ItemStatus.Selected, Find(entries, "glasses").Status);
            Assert.AreEqual(ItemStatus.ConditionDependent, Find(entries, "reflective").Status);
            Assert.AreEqual(ItemStatus.CoreOmitted, Find(entries, "shoes_ok").Status);
        }

        [Test]
        public void Un_opcional_no_elegido_no_aparece_como_olvido()
        {
            Module01CatalogSO catalog = Catalog(Zone("head",
                Item("helmet_ok", SafetyItemCategory.Core),
                Item("glasses", SafetyItemCategory.Optional)));

            SelectionLedger ledger = new SelectionLedger();
            ledger.Add("helmet_ok");

            SelectionReport report = SelectionGrader.Grade(catalog, ledger);
            List<ComparisonEntry> entries = ReportPresenter.BuildEntries(catalog, report, Fake);

            Assert.AreEqual(1, entries.Count, "Solo deberia listarse el nucleo elegido.");
            Assert.AreEqual("helmet_ok", entries[0].Detail);
        }

        [Test]
        public void Los_titulos_salen_traducidos_no_con_la_clave_cruda()
        {
            Module01CatalogSO catalog = Catalog(Zone("head", Item("helmet_ok", SafetyItemCategory.Core)));
            SelectionLedger ledger = new SelectionLedger();
            ledger.Add("helmet_ok");

            SelectionReport report = SelectionGrader.Grade(catalog, ledger);
            List<ComparisonEntry> entries = ReportPresenter.BuildEntries(catalog, report, Fake);

            Assert.AreEqual("[Module1/helmet_ok]", entries[0].Title);
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

            Assert.AreEqual("EN:Module1/helmet_ok", english[0].Title);
            Assert.AreEqual("ES:Module1/helmet_ok", spanish[0].Title);
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
