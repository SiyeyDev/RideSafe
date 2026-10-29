using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RideSafe.Module01.Editor;
using RideSafe.UI;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Dos reglas de la hoja del cliente que el módulo no aplicaba (PO, 2026-10-28): la gorra es
    /// «Peligrosa condicional» —solo resta si se elige en lugar del casco— y cuatro recomendados
    /// dependen del vehículo —la alforja y el kit solo con la e-bike, la mochila solo con el
    /// scooter—.
    /// </summary>
    public class SheetRulesTests : CatalogFixture
    {
        private Module01CatalogSO HelmetAndCap(out SafetyItemSO cap)
        {
            SafetyItemSO helmet = Item("helmet_ok", SafetyItemCategory.Core, 40f);
            cap = Item("cap", SafetyItemCategory.Inappropriate, 20f, "helmet_substitution");
            cap.ConfigureRulesForTests(substitutesFor: "helmet_ok");
            return Catalog(Zone("z", helmet, cap));
        }

        [Test]
        public void La_gorra_junto_al_casco_no_es_peligro()
        {
            Module01CatalogSO catalog = HelmetAndCap(out _);
            var ledger = new SelectionLedger();
            ledger.Add("helmet_ok");
            ledger.Add("cap");

            SelectionReport report = SelectionGrader.Grade(catalog, ledger);
            SelectionScore score = SelectionScore.Of(catalog, report);

            CollectionAssert.DoesNotContain(report.InappropriateSelected, "cap");
            Assert.AreEqual(0, score.HazardsTaken);
            Assert.AreEqual(0f, score.HazardDeduction);
            Assert.AreEqual(ItemStatus.NoEffect,
                ReportPresenter.BuildEntries(catalog, report, k => k).First(e => e.Detail == "cap").Status);
        }

        [Test]
        public void La_gorra_en_lugar_del_casco_si_es_peligro()
        {
            Module01CatalogSO catalog = HelmetAndCap(out _);
            var ledger = new SelectionLedger();
            ledger.Add("cap");

            SelectionReport report = SelectionGrader.Grade(catalog, ledger);
            SelectionScore score = SelectionScore.Of(catalog, report);

            CollectionAssert.Contains(report.InappropriateSelected, "cap");
            Assert.AreEqual(1, score.HazardsTaken);
            Assert.AreEqual(20f, score.HazardDeduction);
            Assert.AreEqual(ItemStatus.NotSuited,
                ReportPresenter.BuildEntries(catalog, report, k => k).First(e => e.Detail == "cap").Status);
        }

        [Test]
        public void Un_elemento_de_un_vehiculo_solo_aparece_con_ese_vehiculo()
        {
            SafetyItemSO pannier = Item("pannier", SafetyItemCategory.Optional, 3f);
            pannier.ConfigureRulesForTests(onlyForVehicle: "ebike");

            Assert.IsTrue(pannier.AppliesTo("ebike"));
            Assert.IsFalse(pannier.AppliesTo("escooter"));
            Assert.IsTrue(pannier.AppliesTo(null), "sin vehículo elegido aparece todo");
        }

        [Test]
        public void La_zona_de_un_vehiculo_no_trae_lo_que_es_de_otro()
        {
            SafetyItemSO pannier = Item("pannier", SafetyItemCategory.Optional, 3f);
            pannier.ConfigureRulesForTests(onlyForVehicle: "ebike");
            SafetyItemSO backpack = Item("backpack", SafetyItemCategory.Optional, 2f);
            backpack.ConfigureRulesForTests(onlyForVehicle: "escooter");
            SafetyItemSO helmet = Item("helmet_ok", SafetyItemCategory.Core, 40f);
            ZoneSO zone = Zone("z", helmet, pannier, backpack);

            ZoneSO scooter = Track(zone.ForVehicle("escooter"));

            CollectionAssert.AreEqual(new[] { "helmet_ok", "backpack" }, scooter.Items.Select(i => i.ItemId).ToArray());
            Assert.AreEqual(zone.ZoneId, scooter.ZoneId);
            Assert.AreEqual(zone.TitleKey, scooter.TitleKey);
        }

        [Test]
        public void Si_todo_aplica_la_zona_es_la_misma()
        {
            ZoneSO zone = Zone("z", Item("helmet_ok", SafetyItemCategory.Core, 40f));

            Assert.AreSame(zone, zone.ForVehicle("escooter"));
        }

        [TestCase("pannier", "ebike")]
        [TestCase("flat_kit", "ebike")]
        [TestCase("backpack", "escooter")]
        [TestCase("bell", null)]
        public void La_biblioteca_declara_con_que_vehiculo_aparece_cada_uno(string itemId, string vehicle)
        {
            Assert.AreEqual(vehicle, Module01Library.All.First(e => e.ItemId == itemId).OnlyForVehicle);
        }

        [Test]
        public void Solo_esos_cuatro_dependen_del_vehiculo()
        {
            // La PO: «solo los elementos que están en la lista»; el resto, con todos los vehículos.
            CollectionAssert.AreEquivalent(new[] { "pannier", "flat_kit", "backpack" },
                Module01Library.All.Where(e => e.OnlyForVehicle != null).Select(e => e.ItemId));
        }

        [Test]
        public void La_gorra_de_la_biblioteca_sustituye_al_casco()
        {
            Assert.AreEqual("helmet_ok", Module01Library.All.First(e => e.ItemId == "cap").SubstitutesFor);
        }
    }
}
