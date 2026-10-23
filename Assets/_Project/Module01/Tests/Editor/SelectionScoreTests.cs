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
        private const float Tol = 0.01f;

        private SelectionReport Grade(Module01CatalogSO catalog, params string[] chosen)
        {
            SelectionLedger ledger = new SelectionLedger();
            foreach (string id in chosen)
                ledger.Add(id);
            return SelectionGrader.Grade(catalog, ledger);
        }

        private SelectionScore Score(Module01CatalogSO catalog, params string[] chosen) =>
            SelectionScore.Of(catalog, Grade(catalog, chosen));

        /// <summary>
        /// La mesa del cliente en día claro, con los pesos y los grupos tal cual la hoja: los 3
        /// esenciales —casco 40, calzado 20, ropa ceñida 5, pool de 65— y las 11 peligrosas
        /// visibles. Las dos luces son «esencial condicional» y en día claro no aplican.
        /// </summary>
        private Module01CatalogSO Biblioteca() => Catalog(
            Zone("proteccion",
                Item(Casco, SafetyItemCategory.Core, 40f),
                Item(Calzado, SafetyItemCategory.Core, 20f),
                Item(Ropa, SafetyItemCategory.Core, 5f),
                Item("H01_correa", SafetyItemCategory.Inappropriate, 30f, "helmet_integrity"),
                Item("H02_agrietado", SafetyItemCategory.Inappropriate, 30f, "helmet_integrity"),
                Item("H03_audifonos", SafetyItemCategory.Inappropriate, 20f, "auditory_occlusion"),
                Item("H13_airpods", SafetyItemCategory.Inappropriate, 20f, "auditory_occlusion"),
                Item("H04_chanclas", SafetyItemCategory.Inappropriate, 15f, "footwear_control"),
                Item("H06_poncho", SafetyItemCategory.Inappropriate, 20f, "loose_rainwear"),
                Item("H07_gafas", SafetyItemCategory.Inappropriate, 20f, "visual_obstruction"),
                Item("H08_mitones", SafetyItemCategory.Inappropriate, 15f, "control_access"),
                Item("H09_tote", SafetyItemCategory.Inappropriate, 15f, "unstable_carry"),
                Item("H10_paraguas", SafetyItemCategory.Inappropriate, 30f, "one_hand_wind"),
                Item("H11_vaso", SafetyItemCategory.Inappropriate, 20f, "one_hand_drink")));

        private const string Casco = "E01_casco";
        private const string Calzado = "E04_calzado";
        private const string Ropa = "E05_ropa";

        private static readonly string[] TodosLosPeligros =
        {
            "H01_correa", "H02_agrietado", "H03_audifonos", "H13_airpods", "H04_chanclas",
            "H06_poncho", "H07_gafas", "H08_mitones", "H09_tote", "H10_paraguas", "H11_vaso"
        };

        private static string[] Mas(string[] baseIds, params string[] extra)
        {
            System.Collections.Generic.List<string> all =
                new System.Collections.Generic.List<string>(baseIds);
            all.AddRange(extra);
            return all.ToArray();
        }

        // ------------------------------------------------ los dos bloques: 70 esencial + 30 riesgo

        [Test]
        public void Todo_lo_esencial_y_ningun_peligro_es_el_cien_por_cien()
        {
            SelectionScore score = Score(Biblioteca(), Casco, Calzado, Ropa);

            Assert.AreEqual(70f, score.EssentialScore, Tol);
            Assert.AreEqual(30f, score.HazardScore, Tol);
            Assert.AreEqual(100f, score.SafetyScore, Tol);
            Assert.AreEqual(PerformanceRank.Knowledgeable, score.Rank);
            Assert.IsTrue(score.CanAdvance);
        }

        [Test]
        public void No_tocar_nada_deja_los_30_del_riesgo_pero_no_deja_avanzar()
        {
            // La hoja no da 0 aquí: no caer en ninguna trampa vale sus 30 puntos. Lo que para al
            // que se queda quieto es el contador de esenciales, no la nota.
            SelectionScore score = Score(Biblioteca());

            Assert.AreEqual(0f, score.EssentialScore, Tol);
            Assert.AreEqual(30f, score.HazardScore, Tol);
            Assert.AreEqual(30f, score.SafetyScore, Tol);
            Assert.AreEqual(3, score.EssentialsMissing);
            Assert.IsFalse(score.CanAdvance, "3 esenciales olvidados: repetir la selección.");
            Assert.IsFalse(score.UnlocksNextModule);
        }

        [TestCase("H04_chanclas", 15f, 85f, TestName = "Chanclas (15) dejan el riesgo en 15")]
        [TestCase("H03_audifonos", 10f, 80f, TestName = "Audífonos (20) dejan el riesgo en 10")]
        [TestCase("H02_agrietado", 0f, 70f, TestName = "Casco agrietado (30) se lleva el bloque entero")]
        public void Cada_peligro_descuenta_sus_puntos_de_los_30(string peligro, float riesgo, float total)
        {
            SelectionScore score = Score(Biblioteca(), Casco, Calzado, Ropa, peligro);

            Assert.AreEqual(riesgo, score.HazardScore, Tol);
            Assert.AreEqual(total, score.SafetyScore, Tol);
        }

        [Test]
        public void El_riesgo_nunca_baja_de_cero_por_mucho_que_se_acumule()
        {
            // «MAX(0; 30 − deducción)». Con los 11 peligros la deducción por grupos es 185.
            SelectionScore score = Score(Biblioteca(), Mas(TodosLosPeligros, Casco, Calzado, Ropa));

            Assert.AreEqual(185f, score.HazardDeduction, Tol);
            Assert.AreEqual(0f, score.HazardScore, Tol);
            Assert.AreEqual(70f, score.SafetyScore, Tol, "Lo esencial sigue valiendo sus 70.");
            Assert.AreEqual(11, score.HazardsTaken);
        }

        // ------------------------------------------------------------- la regla de grupo

        [Test]
        public void Dos_peligros_del_mismo_grupo_descuentan_una_sola_vez()
        {
            // «Usa la penalización máxima por grupo». Los dos cascos rotos son
            // helmet_integrity: 30, no 60. Es el mismo error cometido dos veces.
            SelectionScore uno = Score(Biblioteca(), Casco, Calzado, Ropa, "H01_correa");
            SelectionScore dos = Score(Biblioteca(), Casco, Calzado, Ropa, "H01_correa", "H02_agrietado");

            Assert.AreEqual(30f, uno.HazardDeduction, Tol);
            Assert.AreEqual(30f, dos.HazardDeduction, Tol, "El grupo descuenta su máximo, no la suma.");
            Assert.AreEqual(2, dos.HazardsTaken, "Pero para el contador siguen siendo dos.");
            Assert.IsFalse(dos.CanAdvance);
        }

        [Test]
        public void Dentro_de_un_grupo_manda_el_peligro_mas_caro()
        {
            Module01CatalogSO catalog = Catalog(Zone("z",
                Item("nucleo", SafetyItemCategory.Core, 10f),
                Item("barato", SafetyItemCategory.Inappropriate, 5f, "mismo_grupo"),
                Item("caro", SafetyItemCategory.Inappropriate, 25f, "mismo_grupo")));

            Assert.AreEqual(25f, Score(catalog, "nucleo", "barato", "caro").HazardDeduction, Tol);
            Assert.AreEqual(25f, Score(catalog, "nucleo", "caro").HazardDeduction, Tol);
            Assert.AreEqual(5f, Score(catalog, "nucleo", "barato").HazardDeduction, Tol);
        }

        [Test]
        public void Dos_peligros_de_grupos_distintos_si_suman()
        {
            // Audífonos (auditory_occlusion, 20) y chanclas (footwear_control, 15) son dos
            // errores distintos: 35 de deducción, el riesgo se va a 0.
            SelectionScore score = Score(Biblioteca(), Casco, Calzado, Ropa, "H03_audifonos", "H04_chanclas");

            Assert.AreEqual(35f, score.HazardDeduction, Tol);
            Assert.AreEqual(0f, score.HazardScore, Tol);
        }

        [Test]
        public void Sin_grupo_declarado_cada_elemento_es_su_propio_grupo()
        {
            Module01CatalogSO catalog = Catalog(Zone("z",
                Item("nucleo", SafetyItemCategory.Core, 10f),
                Item("t1", SafetyItemCategory.Inappropriate, 5f),
                Item("t2", SafetyItemCategory.Inappropriate, 5f)));

            Assert.AreEqual(10f, Score(catalog, "nucleo", "t1", "t2").HazardDeduction, Tol);
        }

        // ---------------------------------------------------- el pool esencial y sus proporciones

        [TestCase(Casco, 26.9f, 56.9f, TestName = "Sin el casco (25 de 65) la nota cae a 56,9")]
        [TestCase(Calzado, 48.5f, 78.5f, TestName = "Sin el calzado (45 de 65), 78,5")]
        [TestCase(Ropa, 64.6f, 94.6f, TestName = "Sin la ropa ceñida (60 de 65), 94,6")]
        public void Omitir_un_esencial_cuesta_en_proporcion_a_su_peso(string omitido, float esencial, float total)
        {
            System.Collections.Generic.List<string> elegidos =
                new System.Collections.Generic.List<string> { Casco, Calzado, Ropa };
            elegidos.Remove(omitido);

            SelectionScore score = Score(Biblioteca(), elegidos.ToArray());

            Assert.AreEqual(esencial, score.EssentialScore, Tol);
            Assert.AreEqual(total, score.SafetyScore, Tol);
            Assert.AreEqual(1, score.EssentialsMissing);
            Assert.IsTrue(score.CanAdvance, "Un solo olvido avisa, no obliga a repetir.");
        }

        [Test]
        public void El_pool_esencial_solo_cuenta_lo_que_pide_el_escenario()
        {
            // «Solo incluye esenciales requeridos por el escenario». De noche entran las dos
            // luces: el pool pasa de 65 a 95 y el casco deja de valer el 61,5 % del bloque.
            Module01CatalogSO noche = Catalog(
                Zone("proteccion",
                    Item(Casco, SafetyItemCategory.Core, 40f),
                    Item(Calzado, SafetyItemCategory.Core, 20f),
                    Item(Ropa, SafetyItemCategory.Core, 5f),
                    Item("E02_luz_del", SafetyItemCategory.Core, 15f),
                    Item("E03_luz_tras", SafetyItemCategory.Core, 15f)));

            SelectionScore sinCasco = SelectionScore.Of(noche,
                Grade(noche, Calzado, Ropa, "E02_luz_del", "E03_luz_tras"));

            Assert.AreEqual(95f, sinCasco.EssentialApplicable, Tol);
            Assert.AreEqual(40.5f, sinCasco.EssentialScore, Tol, "70 × 55/95 = 40,526 → 40,5.");
            Assert.AreEqual(70.5f, sinCasco.SafetyScore, Tol);
        }

        [Test]
        public void El_puntaje_esencial_se_redondea_a_un_decimal()
        {
            // REDONDEAR(70 × seleccionado / aplicable; 1).
            SelectionScore score = Score(Biblioteca(), Calzado, Ropa);

            Assert.AreEqual(26.9f, score.EssentialScore, 0.001f, "26,923 redondeado a un decimal.");
        }

        // -------------------------------------------------------------------- lo opcional

        [Test]
        public void Lo_opcional_y_lo_condicional_nunca_entran_en_la_nota()
        {
            // «No elegirlos no resta puntos ni impide avanzar»; su bono va por separado.
            Module01CatalogSO catalog = Catalog(Zone("head",
                Item("helmet_ok", SafetyItemCategory.Core),
                Item("glasses", SafetyItemCategory.Optional),
                Item("vest", SafetyItemCategory.ConditionDependent)));

            SelectionScore sinExtras = SelectionScore.Of(catalog, Grade(catalog, "helmet_ok"));
            SelectionScore conExtras = SelectionScore.Of(catalog,
                Grade(catalog, "helmet_ok", "glasses", "vest"));

            Assert.AreEqual(100f, sinExtras.SafetyScore, Tol);
            Assert.AreEqual(100f, conExtras.SafetyScore, Tol, "Tomarlos no sube la nota ni la baja.");
        }

        [Test]
        public void Un_peso_de_cero_deja_el_elemento_fuera_de_la_nota()
        {
            // El teléfono: la hoja lo declara neutral, «no cambia el resultado». Peso 0 lo deja
            // en la mesa y en el reporte sin mover la nota.
            Module01CatalogSO catalog = Catalog(Zone("load",
                Item("cargo_secured", SafetyItemCategory.Core, 10f),
                Item("phone", SafetyItemCategory.Inappropriate, 0f, "phone_storage")));

            SelectionScore score = Score(catalog, "cargo_secured", "phone");

            Assert.AreEqual(100f, score.SafetyScore, Tol);
            Assert.AreEqual(0f, score.HazardDeduction, Tol);
            Assert.AreEqual(1, score.HazardsTaken, "Se cuenta, pero no resta.");
        }

        // ------------------------------------------- avanzar: contadores, no porcentaje

        [Test]
        public void Un_solo_fallo_de_cada_clase_todavia_deja_avanzar()
        {
            SelectionScore score = Score(Biblioteca(), Casco, Calzado, "H04_chanclas");

            Assert.AreEqual(1, score.EssentialsMissing);
            Assert.AreEqual(1, score.HazardsTaken);
            Assert.IsTrue(score.CanAdvance, "Uno de cada clase es feedback, no repetición.");
            Assert.IsFalse(score.MustRepeatSelection);
        }

        [Test]
        public void Dos_peligros_obligan_a_repetir_la_seleccion()
        {
            SelectionScore score = Score(Biblioteca(), Casco, Calzado, Ropa, "H04_chanclas", "H08_mitones");

            Assert.AreEqual(2, score.HazardsTaken);
            Assert.IsTrue(score.MustRepeatSelection);
        }

        [Test]
        public void Dos_esenciales_olvidados_obligan_a_repetir_aunque_la_nota_apruebe()
        {
            Module01CatalogSO catalog = Catalog(Zone("z",
                Item("a", SafetyItemCategory.Core, 90f),
                Item("b", SafetyItemCategory.Core, 5f),
                Item("c", SafetyItemCategory.Core, 5f)));

            SelectionScore score = SelectionScore.Of(catalog, Grade(catalog, "a"));

            Assert.AreEqual(93f, score.SafetyScore, Tol, "63 de esencial más los 30 de riesgo.");
            Assert.AreEqual(PerformanceRank.Knowledgeable, score.Rank, "La nota sola aprobaría.");
            Assert.AreEqual(2, score.EssentialsMissing);
            Assert.IsTrue(score.MustRepeatSelection, "Pero el contador manda sobre la nota.");
        }

        [Test]
        public void La_nota_y_la_decision_de_avanzar_pueden_discrepar_y_es_a_proposito()
        {
            // Un solo casco roto: la nota se cae a 70 y suspende el corte de 75, pero es UN
            // peligro, así que la hoja manda avanzar con feedback. Son dos medidas distintas.
            SelectionScore score = Score(Biblioteca(), Casco, Calzado, Ropa, "H02_agrietado");

            Assert.AreEqual(70f, score.SafetyScore, Tol);
            Assert.IsFalse(score.UnlocksNextModule, "No llega al 75 %.");
            Assert.IsTrue(score.CanAdvance, "Pero no tiene que repetir la selección.");
        }

        // ------------------------------------------------------------------ cortes y rangos

        [Test]
        public void Setenta_y_cinco_justo_aprueba()
        {
            Module01CatalogSO catalog = Catalog(Zone("z",
                Item("nucleo", SafetyItemCategory.Core, 10f),
                Item("trampa", SafetyItemCategory.Inappropriate, 25f)));

            SelectionScore score = SelectionScore.Of(catalog, Grade(catalog, "nucleo", "trampa"));

            Assert.AreEqual(75f, score.SafetyScore, Tol, "70 de esencial más 5 de riesgo.");
            Assert.AreEqual(PerformanceRank.Knowledgeable, score.Rank);
            Assert.IsTrue(score.UnlocksNextModule);
        }

        [Test]
        public void Un_pelo_por_debajo_del_corte_no_regala_el_aprobado()
        {
            // 74,6 no puede convertirse en 75 por redondear a entero antes de mirar el rango.
            Module01CatalogSO catalog = Catalog(Zone("z",
                Item("nucleo", SafetyItemCategory.Core, 10f),
                Item("trampa", SafetyItemCategory.Inappropriate, 25.4f)));

            SelectionScore score = SelectionScore.Of(catalog, Grade(catalog, "nucleo", "trampa"));

            Assert.AreEqual(74.6f, score.SafetyScore, Tol);
            Assert.AreEqual(75, score.Percent, "En pantalla se ve 75...");
            Assert.AreEqual(PerformanceRank.NeedsImprovement, score.Rank, "...pero el rango sale del valor real.");
            Assert.IsFalse(score.UnlocksNextModule);
        }

        [TestCase(100f, PerformanceRank.Knowledgeable)]
        [TestCase(75f, PerformanceRank.Knowledgeable)]
        [TestCase(74.9f, PerformanceRank.NeedsImprovement)]
        [TestCase(50f, PerformanceRank.NeedsImprovement)]
        [TestCase(25f, PerformanceRank.Reconsider)]
        [TestCase(0f, PerformanceRank.Inexperienced)]
        public void Los_cortes_de_la_reunion(float score, PerformanceRank esperado)
        {
            Assert.AreEqual(esperado, SelectionScore.RankOf(score));
        }

        [Test]
        public void Solo_el_rango_de_aprobado_abre_el_modulo_2()
        {
            Assert.IsTrue(PerformanceRank.Knowledgeable.UnlocksNextModule());
            Assert.IsFalse(PerformanceRank.NeedsImprovement.UnlocksNextModule());
            Assert.IsFalse(PerformanceRank.Reconsider.UnlocksNextModule());
            Assert.IsFalse(PerformanceRank.Inexperienced.UnlocksNextModule());
        }

        // -------------------------------------------------------------------- bordes

        [Test]
        public void Sin_nada_que_juzgar_no_se_suspende_a_nadie()
        {
            Module01CatalogSO catalog = Catalog(Zone("z", Item("glasses", SafetyItemCategory.Optional)));

            SelectionScore score = Score(catalog);

            Assert.AreEqual(100f, score.SafetyScore, Tol);
            Assert.AreEqual(0, score.Judged);
            Assert.IsTrue(score.CanAdvance);
        }

        [Test]
        public void Una_seccion_que_solo_tiene_trampas_da_el_bloque_esencial_entero()
        {
            Module01CatalogSO catalog = Catalog(Zone("z",
                Item("t1", SafetyItemCategory.Inappropriate, 12f),
                Item("t2", SafetyItemCategory.Inappropriate, 12f)));

            Assert.AreEqual(100f, Score(catalog).SafetyScore, Tol, "No caer en ninguna es un 100.");
            Assert.AreEqual(88f, Score(catalog, "t1").SafetyScore, Tol);
            Assert.AreEqual(76f, Score(catalog, "t1", "t2").SafetyScore, Tol);
        }

        [Test]
        public void Un_reporte_nulo_no_revienta()
        {
            Module01CatalogSO catalog = Catalog(Zone("z", Item("a", SafetyItemCategory.Core)));

            Assert.DoesNotThrow(() => SelectionScore.Of(catalog, null));
            Assert.DoesNotThrow(() => SelectionScore.Of(null, null));
        }

        // --------------------------------------------------------------- las dos secciones

        [Test]
        public void La_nota_final_suma_los_pesos_de_las_dos_secciones_en_vez_de_promediar()
        {
            // Protección perfecta (65 de 65) y vehículo a medias (1 punto de 5): 66 de 70 del
            // pool, que son 66 puntos de esencial. Promediando porcentajes saldría (100+20)/2.
            Module01CatalogSO personal = Catalog(Zone("p",
                Item(Casco, SafetyItemCategory.Core, 40f),
                Item(Calzado, SafetyItemCategory.Core, 20f),
                Item(Ropa, SafetyItemCategory.Core, 5f)));
            Module01CatalogSO vehiculo = Catalog(Zone("v",
                Item("v0", SafetyItemCategory.Core), Item("v1", SafetyItemCategory.Core),
                Item("v2", SafetyItemCategory.Core), Item("v3", SafetyItemCategory.Core),
                Item("v4", SafetyItemCategory.Core)));

            SelectionScore a = SelectionScore.Of(personal, Grade(personal, Casco, Calzado, Ropa));
            SelectionScore b = SelectionScore.Of(vehiculo, Grade(vehiculo, "v0"));

            SelectionScore total = SelectionScore.Combine(a, b);

            Assert.AreEqual(70f, total.EssentialApplicable, Tol);
            Assert.AreEqual(66f, total.EssentialEarned, Tol);
            Assert.AreEqual(96f, total.SafetyScore, Tol);
            Assert.AreEqual(4, total.EssentialsMissing);
            Assert.IsTrue(total.MustRepeatSelection, "4 puntos del vehículo sin revisar.");
        }

        [Test]
        public void Al_combinar_se_suman_contadores_y_deducciones()
        {
            Module01CatalogSO personal = Catalog(Zone("p",
                Item("p0", SafetyItemCategory.Core, 10f),
                Item("ph", SafetyItemCategory.Inappropriate, 20f)));
            Module01CatalogSO vehiculo = Catalog(Zone("v",
                Item("v0", SafetyItemCategory.Core, 10f)));

            SelectionScore a = SelectionScore.Of(personal, Grade(personal, "p0", "ph"));
            SelectionScore b = SelectionScore.Of(vehiculo, Grade(vehiculo));

            SelectionScore total = SelectionScore.Combine(a, b);

            Assert.AreEqual(1, total.EssentialsMissing);
            Assert.AreEqual(1, total.HazardsTaken);
            Assert.AreEqual(20f, total.HazardDeduction, Tol);
            Assert.AreEqual(35f, total.EssentialScore, Tol, "70 × 10/20.");
            Assert.AreEqual(10f, total.HazardScore, Tol, "30 − 20.");
            Assert.AreEqual(45f, total.SafetyScore, Tol);
        }

        // ------------------------------------------------------- el bono de preparación

        /// <summary>
        /// Los recomendados de la hoja con su bono: suman 25 y el tope es 15. Los dos primeros
        /// son el par excluyente —prenda fluorescente de día y chaleco reflectivo de noche—.
        /// </summary>
        private Module01CatalogSO Recomendados()
        {
            SafetyItemSO dia = Item("O01_fluorescente", SafetyItemCategory.Optional, 2f);
            SafetyItemSO noche = Item("O02_reflectivo", SafetyItemCategory.Optional, 3f);
            dia.ConfigureBonusForTests("O02_reflectivo");
            noche.ConfigureBonusForTests("O01_fluorescente");

            SafetyItemSO lluvia = Item("O16_impermeable", SafetyItemCategory.Optional, 2f);
            lluvia.ConfigureBonusForTests(null, "rain");

            return Catalog(Zone("preparacion",
                Item("nucleo", SafetyItemCategory.Core, 10f),
                dia, noche, lluvia,
                Item("O04_guantes", SafetyItemCategory.Optional, 2f),
                Item("O07_gafas", SafetyItemCategory.Optional, 1f),
                Item("O08_gafas_sol", SafetyItemCategory.Optional, 1f),
                Item("O13_multiherramienta", SafetyItemCategory.Optional, 1f),
                Item("O19_alforja", SafetyItemCategory.Optional, 3f)));
        }

        [Test]
        public void El_bono_suma_lo_que_vale_cada_recomendado_y_no_toca_la_nota()
        {
            Module01CatalogSO catalog = Recomendados();

            SelectionScore sinExtras = Score(catalog, "nucleo");
            SelectionScore conExtras = Score(catalog, "nucleo", "O04_guantes", "O13_multiherramienta");

            Assert.AreEqual(0f, sinExtras.PreparednessBonus, Tol);
            Assert.AreEqual(3f, conExtras.PreparednessBonus, Tol, "Guantes 2 más multiherramienta 1.");
            Assert.AreEqual(sinExtras.SafetyScore, conExtras.SafetyScore, Tol,
                "El bono va aparte: la nota no se mueve.");
        }

        [Test]
        public void No_llevarse_ningun_recomendado_no_resta_ni_impide_avanzar()
        {
            SelectionScore score = Score(Recomendados(), "nucleo");

            Assert.AreEqual(100f, score.SafetyScore, Tol);
            Assert.AreEqual(0f, score.PreparednessBonus, Tol);
            Assert.IsTrue(score.CanAdvance);
        }

        [Test]
        public void El_bono_se_topa_en_quince()
        {
            // 2+3 del par (cuenta 3) + 2 + 1 + 1 + 1 + 3 = 11... hacen falta más para pasarse,
            // así que se mide con el tope directo.
            Module01CatalogSO catalog = Catalog(Zone("z",
                Item("nucleo", SafetyItemCategory.Core, 10f),
                Item("a", SafetyItemCategory.Optional, 9f),
                Item("b", SafetyItemCategory.Optional, 9f)));

            SelectionScore score = Score(catalog, "nucleo", "a", "b");

            Assert.AreEqual(18f, score.BonusEarned, Tol, "Lo que sumaban de verdad.");
            Assert.AreEqual(15f, score.PreparednessBonus, Tol, "MIN(15; suma).");
        }

        [Test]
        public void El_par_excluyente_solo_cobra_el_mejor_de_los_dos()
        {
            // «exclusive_bonus_with»: la prenda fluorescente de día y el chaleco reflectivo de
            // noche son la misma precaución para dos escenarios. Llevarse las dos no se cobra
            // dos veces.
            Module01CatalogSO catalog = Recomendados();

            Assert.AreEqual(2f, Score(catalog, "nucleo", "O01_fluorescente").PreparednessBonus, Tol);
            Assert.AreEqual(3f, Score(catalog, "nucleo", "O02_reflectivo").PreparednessBonus, Tol);
            Assert.AreEqual(3f, Score(catalog, "nucleo", "O01_fluorescente", "O02_reflectivo").PreparednessBonus,
                Tol, "Las dos juntas valen lo que la mejor, no 5.");
        }

        [Test]
        public void Dos_recomendados_que_comparten_grupo_pero_no_son_excluyentes_si_suman()
        {
            // Las gafas transparentes y las de sol son las dos «eye_protection» en la hoja, pero
            // no están declaradas excluyentes, así que sus bonos se acumulan. Por eso el par
            // excluyente es un campo aparte del grupo de peligro.
            SelectionScore score = Score(Recomendados(), "nucleo", "O07_gafas", "O08_gafas_sol");

            Assert.AreEqual(2f, score.PreparednessBonus, Tol);
        }

        [Test]
        public void Un_bono_con_condicion_no_cuenta_si_el_escenario_no_la_trae()
        {
            Module01CatalogSO catalog = Recomendados();
            SelectionReport report = Grade(catalog, "nucleo", "O16_impermeable");

            SelectionScore diaClaro = SelectionScore.Of(catalog, report);
            SelectionScore conLluvia = SelectionScore.Of(catalog, report, new[] { "rain" });

            Assert.AreEqual(0f, diaClaro.PreparednessBonus, Tol,
                "La chaqueta impermeable no se premia en día claro.");
            Assert.AreEqual(2f, conLluvia.PreparednessBonus, Tol);
        }

        [Test]
        public void Una_condicion_activa_que_no_es_la_suya_no_abre_el_bono()
        {
            Module01CatalogSO catalog = Recomendados();
            SelectionReport report = Grade(catalog, "nucleo", "O16_impermeable");

            SelectionScore score = SelectionScore.Of(catalog, report, new[] { "night", "cold" });

            Assert.AreEqual(0f, score.PreparednessBonus, Tol);
        }

        [Test]
        public void Lo_condicional_tambien_puede_dar_bono()
        {
            // El chaleco vive hoy como ConditionDependent y la hoja lo clasifica «Recomendada
            // condicional». Las dos categorías son bonificables y ninguna penaliza.
            Module01CatalogSO catalog = Catalog(Zone("z",
                Item("nucleo", SafetyItemCategory.Core, 10f),
                Item("reflective", SafetyItemCategory.ConditionDependent, 3f)));

            Assert.AreEqual(3f, Score(catalog, "nucleo", "reflective").PreparednessBonus, Tol);
        }

        [Test]
        public void El_bono_se_conserva_aunque_no_haya_nada_que_juzgar()
        {
            Module01CatalogSO catalog = Catalog(Zone("z",
                Item("O04_guantes", SafetyItemCategory.Optional, 2f)));

            SelectionScore score = Score(catalog, "O04_guantes");

            Assert.AreEqual(0, score.Judged);
            Assert.AreEqual(2f, score.PreparednessBonus, Tol);
        }

        [Test]
        public void Al_combinar_el_tope_se_aplica_una_sola_vez()
        {
            // Topar por sección y luego sumar daría 30 donde la hoja da 15.
            Module01CatalogSO a = Catalog(Zone("a",
                Item("na", SafetyItemCategory.Core, 10f),
                Item("oa", SafetyItemCategory.Optional, 12f)));
            Module01CatalogSO b = Catalog(Zone("b",
                Item("nb", SafetyItemCategory.Core, 10f),
                Item("ob", SafetyItemCategory.Optional, 12f)));

            SelectionScore total = SelectionScore.Combine(
                SelectionScore.Of(a, Grade(a, "na", "oa")),
                SelectionScore.Of(b, Grade(b, "nb", "ob")));

            Assert.AreEqual(24f, total.BonusEarned, Tol);
            Assert.AreEqual(15f, total.PreparednessBonus, Tol);
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
            // Dos inapropiados del mismo peso, uno marcado como deteriorado y otro no. Lo que
            // fija este test es que el icono no mueve ni un punto: resta por ser inapropiado,
            // no por estar roto. Los 10 de deducción dejan el riesgo en 20, y con lo esencial
            // entero son 90.
            SafetyItemSO damaged = Item("helmet_damaged", SafetyItemCategory.Inappropriate, 10f);
            damaged.MarkDeterioratedForTests();
            SafetyItemSO plain = Item("helmet_wrong", SafetyItemCategory.Inappropriate, 10f);
            Module01CatalogSO catalog = Catalog(Zone("head",
                Item("helmet_ok", SafetyItemCategory.Core, 40f), damaged, plain));

            SelectionLedger conRoto = new SelectionLedger();
            conRoto.Add("helmet_ok");
            conRoto.Add("helmet_damaged");

            SelectionLedger conEquivocado = new SelectionLedger();
            conEquivocado.Add("helmet_ok");
            conEquivocado.Add("helmet_wrong");

            SelectionScore roto = SelectionScore.Of(catalog, SelectionGrader.Grade(catalog, conRoto));
            SelectionScore equivocado = SelectionScore.Of(catalog, SelectionGrader.Grade(catalog, conEquivocado));

            Assert.AreEqual(90f, roto.SafetyScore, 0.01f, "70 de esencial más 30 − 10 de riesgo.");
            Assert.AreEqual(roto.SafetyScore, equivocado.SafetyScore, 0.01f,
                "Cambiar el icono no puede cambiar la nota.");
        }
    }
}
