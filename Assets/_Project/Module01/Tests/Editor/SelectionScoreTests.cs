using NUnit.Framework;
using RideSafe.UI;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// La nota y su rango, con los cortes que fijó la reunión: ≥75 aprueba y abre el módulo 2;
    /// por debajo solo se puede repetir.
    /// </summary>
    public class SelectionScoreTests : CatalogFixture
    {
        private SelectionReport Grade(Module01CatalogSO catalog, params string[] chosen)
        {
            SelectionLedger ledger = new SelectionLedger();
            foreach (string id in chosen)
                ledger.Add(id);
            return SelectionGrader.Grade(catalog, ledger);
        }

        [Test]
        public void Todo_el_nucleo_y_ningun_inapropiado_es_el_cien_por_cien()
        {
            Module01CatalogSO catalog = Catalog(Zone("head",
                Item("helmet_ok", SafetyItemCategory.Core),
                Item("shoes_ok", SafetyItemCategory.Core),
                Item("phone", SafetyItemCategory.Inappropriate)));

            SelectionScore score = SelectionScore.Of(catalog, Grade(catalog, "helmet_ok", "shoes_ok"));

            Assert.AreEqual(100, score.Percent);
            Assert.AreEqual(PerformanceRank.Knowledgeable, score.Rank);
            Assert.IsTrue(score.UnlocksNextModule);
        }

        [Test]
        public void Elegir_un_inapropiado_cuenta_como_fallo()
        {
            Module01CatalogSO catalog = Catalog(Zone("head",
                Item("helmet_ok", SafetyItemCategory.Core),
                Item("shoes_ok", SafetyItemCategory.Core),
                Item("phone", SafetyItemCategory.Inappropriate)));

            SelectionScore score = SelectionScore.Of(catalog, Grade(catalog, "helmet_ok", "shoes_ok", "phone"));

            Assert.AreEqual(66, score.Percent, "2 aciertos de 3 cosas juzgables.");
        }

        [Test]
        public void Lo_opcional_y_lo_condicional_nunca_entran_en_la_nota()
        {
            // La narrativa lo prohíbe: penalizar lo opcional es exactamente la penalización
            // falsa que el módulo no debe enseñar.
            Module01CatalogSO catalog = Catalog(Zone("head",
                Item("helmet_ok", SafetyItemCategory.Core),
                Item("glasses", SafetyItemCategory.Optional),
                Item("vest", SafetyItemCategory.ConditionDependent)));

            SelectionScore sinExtras = SelectionScore.Of(catalog, Grade(catalog, "helmet_ok"));
            SelectionScore conExtras = SelectionScore.Of(catalog, Grade(catalog, "helmet_ok", "glasses", "vest"));

            Assert.AreEqual(100, sinExtras.Percent);
            Assert.AreEqual(100, conExtras.Percent, "Tomarlos no sube la nota ni la baja.");
        }

        [Test]
        public void Setenta_y_cinco_justo_aprueba()
        {
            Module01CatalogSO catalog = Catalog(Zone("z",
                Item("a", SafetyItemCategory.Core), Item("b", SafetyItemCategory.Core),
                Item("c", SafetyItemCategory.Core), Item("d", SafetyItemCategory.Core)));

            SelectionScore score = SelectionScore.Of(catalog, Grade(catalog, "a", "b", "c"));

            Assert.AreEqual(75, score.Percent);
            Assert.AreEqual(PerformanceRank.Knowledgeable, score.Rank);
            Assert.IsTrue(score.UnlocksNextModule);
        }

        [Test]
        public void La_nota_se_trunca_hacia_abajo_y_no_regala_el_aprobado()
        {
            // 3 de 4 aciertos más uno a medias no existe, pero 5 de 7 = 71.4 % sí: truncar es
            // lo que impide que un 74.6 % se convierta en aprobado por un redondeo.
            Module01CatalogSO catalog = Catalog(Zone("z",
                Item("a", SafetyItemCategory.Core), Item("b", SafetyItemCategory.Core),
                Item("c", SafetyItemCategory.Core), Item("d", SafetyItemCategory.Core),
                Item("e", SafetyItemCategory.Core), Item("f", SafetyItemCategory.Core),
                Item("g", SafetyItemCategory.Core)));

            SelectionScore score = SelectionScore.Of(catalog, Grade(catalog, "a", "b", "c", "d", "e"));

            Assert.AreEqual(71, score.Percent);
            Assert.IsFalse(score.UnlocksNextModule);
        }

        [TestCase(4, 4, 100, PerformanceRank.Knowledgeable, true)]
        [TestCase(3, 4, 75, PerformanceRank.Knowledgeable, true)]
        [TestCase(2, 4, 50, PerformanceRank.NeedsImprovement, false)]
        [TestCase(1, 4, 25, PerformanceRank.Reconsider, false)]
        [TestCase(0, 4, 0, PerformanceRank.Inexperienced, false)]
        public void Los_cortes_de_la_reunion(int aciertos, int total, int esperado,
                                             PerformanceRank rango, bool abreModulo2)
        {
            SafetyItemSO[] items = new SafetyItemSO[total];
            for (int i = 0; i < total; i++)
                items[i] = Item("i" + i, SafetyItemCategory.Core);
            Module01CatalogSO catalog = Catalog(Zone("z", items));

            string[] chosen = new string[aciertos];
            for (int i = 0; i < aciertos; i++)
                chosen[i] = "i" + i;

            SelectionScore score = SelectionScore.Of(catalog, Grade(catalog, chosen));

            Assert.AreEqual(esperado, score.Percent);
            Assert.AreEqual(rango, score.Rank);
            Assert.AreEqual(abreModulo2, score.UnlocksNextModule);
        }

        [Test]
        public void Solo_el_rango_de_aprobado_abre_el_modulo_2()
        {
            Assert.IsTrue(PerformanceRank.Knowledgeable.UnlocksNextModule());
            Assert.IsFalse(PerformanceRank.NeedsImprovement.UnlocksNextModule());
            Assert.IsFalse(PerformanceRank.Reconsider.UnlocksNextModule());
            Assert.IsFalse(PerformanceRank.Inexperienced.UnlocksNextModule());
        }

        [Test]
        public void Sin_nada_que_juzgar_no_se_suspende_a_nadie()
        {
            // Un catálogo sin núcleo ni inapropiados es un error de datos, pero el aprendiz no
            // ha fallado en nada: suspenderlo sería castigarle por un fallo que no es suyo.
            Module01CatalogSO catalog = Catalog(Zone("z", Item("glasses", SafetyItemCategory.Optional)));

            SelectionScore score = SelectionScore.Of(catalog, Grade(catalog));

            Assert.AreEqual(100, score.Percent);
            Assert.AreEqual(0, score.Judged);
        }

        [Test]
        public void Un_reporte_nulo_no_revienta()
        {
            Module01CatalogSO catalog = Catalog(Zone("z", Item("a", SafetyItemCategory.Core)));

            Assert.DoesNotThrow(() => SelectionScore.Of(catalog, null));
            Assert.DoesNotThrow(() => SelectionScore.Of(null, null));
        }

        [Test]
        public void La_nota_final_suma_aciertos_y_juzgables_en_vez_de_promediar_porcentajes()
        {
            // 7 elementos personales con 7 aciertos, 5 del vehiculo con 1: 8 de 12 = 66 %.
            // Promediando los porcentajes saldria (100 + 20) / 2 = 60, que le daria a cada
            // punto del vehiculo mas peso que a un EPP sin que nadie lo haya decidido.
            Module01CatalogSO personal = Catalog(Zone("p",
                Item("p0", SafetyItemCategory.Core), Item("p1", SafetyItemCategory.Core),
                Item("p2", SafetyItemCategory.Core), Item("p3", SafetyItemCategory.Core),
                Item("p4", SafetyItemCategory.Core), Item("p5", SafetyItemCategory.Core),
                Item("p6", SafetyItemCategory.Core)));
            Module01CatalogSO vehicle = Catalog(Zone("v",
                Item("v0", SafetyItemCategory.Core), Item("v1", SafetyItemCategory.Core),
                Item("v2", SafetyItemCategory.Core), Item("v3", SafetyItemCategory.Core),
                Item("v4", SafetyItemCategory.Core)));

            SelectionScore a = SelectionScore.Of(personal,
                Grade(personal, "p0", "p1", "p2", "p3", "p4", "p5", "p6"));
            SelectionScore b = SelectionScore.Of(vehicle, Grade(vehicle, "v0"));

            SelectionScore total = SelectionScore.Combine(a, b);

            Assert.AreEqual(12, total.Judged);
            Assert.AreEqual(8, total.Correct);
            Assert.AreEqual(66, total.Percent);
            Assert.AreEqual(PerformanceRank.NeedsImprovement, total.Rank);
        }

        [Test]
        public void Cada_rango_trae_su_clave_de_texto_y_no_se_repiten()
        {
            string[] keys =
            {
                PerformanceRank.Knowledgeable.FeedbackKey(),
                PerformanceRank.NeedsImprovement.FeedbackKey(),
                PerformanceRank.Reconsider.FeedbackKey(),
                PerformanceRank.Inexperienced.FeedbackKey()
            };

            CollectionAssert.AllItemsAreNotNull(keys);
            CollectionAssert.AllItemsAreUnique(keys);
            foreach (string k in keys)
                StringAssert.StartsWith("M1/", k);
        }
    }

    /// <summary>
    /// El cuarto estado que pidió la reunión: «elemento en riesgo o deteriorado», distinto de
    /// «selección incorrecta».
    /// </summary>
    public class DeterioratedStatusTests : CatalogFixture
    {
        [Test]
        public void Un_inapropiado_deteriorado_se_pinta_igual_que_cualquier_inapropiado()
        {
            SafetyItemSO damaged = Item("helmet_damaged", SafetyItemCategory.Inappropriate);
            damaged.MarkDeterioratedForTests();
            Module01CatalogSO catalog = Catalog(Zone("head", damaged));

            SelectionLedger ledger = new SelectionLedger();
            ledger.Add("helmet_damaged");
            SelectionReport report = SelectionGrader.Grade(catalog, ledger);

            var entries = ReportPresenter.BuildEntries(catalog, report, k => k);

            Assert.AreEqual(1, entries.Count);
            // Tres estados y ya, por decision de Charlie el 2026-10-16: deteriorado y
            // "no es lo que toca" se dicen igual en el resumen.
            Assert.AreEqual(ItemStatus.NotSuited, entries[0].Status);
        }

        [Test]
        public void Un_inapropiado_que_solo_esta_fuera_de_sitio_sigue_siendo_incorrecto()
        {
            // El teléfono en la mano no está deteriorado: está mal elegido. Son dos mensajes
            // distintos y la leyenda los separa.
            Module01CatalogSO catalog = Catalog(Zone("load",
                Item("cargo_handheld", SafetyItemCategory.Inappropriate)));

            SelectionLedger ledger = new SelectionLedger();
            ledger.Add("cargo_handheld");
            SelectionReport report = SelectionGrader.Grade(catalog, ledger);

            var entries = ReportPresenter.BuildEntries(catalog, report, k => k);

            Assert.AreEqual(ItemStatus.NotSuited, entries[0].Status);
        }

        [Test]
        public void Lo_deteriorado_sigue_contando_como_fallo_en_la_nota()
        {
            SafetyItemSO damaged = Item("helmet_damaged", SafetyItemCategory.Inappropriate);
            damaged.MarkDeterioratedForTests();
            Module01CatalogSO catalog = Catalog(Zone("head",
                Item("helmet_ok", SafetyItemCategory.Core), damaged));

            SelectionLedger ledger = new SelectionLedger();
            ledger.Add("helmet_ok");
            ledger.Add("helmet_damaged");

            SelectionScore score = SelectionScore.Of(catalog, SelectionGrader.Grade(catalog, ledger));

            Assert.AreEqual(50, score.Percent, "Cambiar el icono no puede cambiar la nota.");
        }
    }
}
