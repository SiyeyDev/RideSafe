using NUnit.Framework;

namespace RideSafe.Module01.Tests
{
    public class ReadinessDecisionTests : CatalogFixture
    {
        private SelectionReport ReportWith(bool critical)
        {
            Module01CatalogSO catalog = Catalog(Zone("cockpit",
                Item("point_brakes", SafetyItemCategory.Core),
                Item("point_wheels", SafetyItemCategory.Core)));

            SelectionLedger ledger = new SelectionLedger();
            ledger.Add("point_brakes");
            if (!critical)
                ledger.Add("point_wheels");

            return SelectionGrader.Grade(catalog, ledger);
        }

        [Test]
        public void Sin_problema_critico_lo_correcto_es_declararlo_listo()
        {
            SelectionReport report = ReportWith(critical: false);

            Assert.IsTrue(ReadinessDecision.IsCorrect(ReadinessChoice.RoadReady, report));
            Assert.IsFalse(ReadinessDecision.IsCorrect(ReadinessChoice.DoNotRide, report));
        }

        [Test]
        public void Con_problema_critico_declararlo_listo_es_incorrecto()
        {
            SelectionReport report = ReportWith(critical: true);

            Assert.IsFalse(ReadinessDecision.IsCorrect(ReadinessChoice.RoadReady, report));
        }

        [Test]
        public void Con_problema_critico_no_arrancar_y_mandar_a_servicio_son_aceptables()
        {
            SelectionReport report = ReportWith(critical: true);

            Assert.IsTrue(ReadinessDecision.IsCorrect(ReadinessChoice.DoNotRide, report));
            Assert.IsTrue(ReadinessDecision.IsCorrect(ReadinessChoice.NeedsService, report));
        }

        [Test]
        public void Sin_reporte_ninguna_decision_se_da_por_correcta()
        {
            Assert.IsFalse(ReadinessDecision.IsCorrect(ReadinessChoice.RoadReady, null));
            Assert.IsFalse(ReadinessDecision.IsCorrect(ReadinessChoice.DoNotRide, null));
        }
    }
}
