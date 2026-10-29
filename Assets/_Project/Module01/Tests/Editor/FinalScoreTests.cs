using NUnit.Framework;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// La nota con la que se cierra el módulo y las tres puertas que pidió la PO el 2026-10-29:
    /// <list type="number">
    /// <item>la sección de EPP se repite si faltan ≥2 esenciales <b>o</b> se eligen ≥2 peligros;</item>
    /// <item>la del vehículo se repite con el mismo criterio, y se repite <b>solo ella</b>;</item>
    /// <item>el <b>promedio</b> de las dos tiene que pasar de 70 o se repite el módulo entero y
    /// no se abre el módulo 2.</item>
    /// </list>
    /// <para>
    /// Las tres son independientes a propósito, y conviene que se vea: alguien puede pasar los
    /// dos checkpoints de sección —un solo fallo en cada una— y suspender el promedio, porque
    /// los checkpoints cuentan errores y el promedio mide cuánto te llevaste.
    /// </para>
    /// <para>
    /// Las dos secciones se construyen con catálogos de verdad en vez de notas inventadas: lo
    /// que hay que comprobar es que los <b>pesos</b> de la hoja llegan hasta la cifra final, y
    /// una nota puesta a mano se saltaría justo ese tramo.
    /// </para>
    /// </summary>
    public class FinalScoreTests : CatalogFixture
    {
        private const float Tol = 0.01f;

        private const string Casco = "E01_casco";
        private const string Calzado = "E04_calzado";
        private const string Ropa = "E05_ropa_cenida";

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
        /// La sección de EPP de día claro: los tres esenciales de la hoja —casco 40, calzado 20,
        /// ropa ceñida 5— y dos peligros de grupos distintos.
        /// </summary>
        private Module01CatalogSO Epp() => Catalog(Zone("proteccion",
            Item(Casco, SafetyItemCategory.Core, 40f),
            Item(Calzado, SafetyItemCategory.Core, 20f),
            Item(Ropa, SafetyItemCategory.Core, 5f),
            Item("H02_agrietado", SafetyItemCategory.Inappropriate, 30f, "helmet_integrity"),
            Item("H04_chanclas", SafetyItemCategory.Inappropriate, 15f, "footwear_control")));

        /// <summary>
        /// La del vehículo: cinco verificaciones, <b>todas del mismo peso</b>. Es la regla que
        /// pidió la PO —«cada punto tiene que equivaler lo que necesite para que la sumatoria
        /// de 100»— y con cinco puntos iguales sale sola: cada uno vale 20.
        /// </summary>
        private Module01CatalogSO Vehiculo() => Catalog(Zone("preoperacional",
            Item("V1_frenos", SafetyItemCategory.Core, 1f),
            Item("V2_ruedas", SafetyItemCategory.Core, 1f),
            Item("V3_direccion", SafetyItemCategory.Core, 1f),
            Item("V4_luces", SafetyItemCategory.Core, 1f),
            Item("V5_electrico", SafetyItemCategory.Core, 1f)));

        private static readonly string[] TodoElVehiculo =
            { "V1_frenos", "V2_ruedas", "V3_direccion", "V4_luces", "V5_electrico" };

        private static string[] Menos(int cuantos)
        {
            string[] todo = TodoElVehiculo;
            string[] parcial = new string[todo.Length - cuantos];
            System.Array.Copy(todo, parcial, parcial.Length);
            return parcial;
        }

        // ------------------------------------------------- cada sección vale 100 por su cuenta

        [Test]
        public void Los_cinco_puntos_del_vehiculo_valen_veinte_cada_uno()
        {
            // «El total debe ser 100, entonces cada punto tiene que equivaler lo que necesite
            // para que la sumatoria de 100». No hay que escribir el 20 en ningún sitio: con
            // pesos iguales la normalización del bloque esencial lo reparte.
            Assert.AreEqual(100f, Score(Vehiculo(), TodoElVehiculo).SafetyScore, Tol);
            Assert.AreEqual(80f, Score(Vehiculo(), Menos(1)).SafetyScore, Tol);
            Assert.AreEqual(60f, Score(Vehiculo(), Menos(2)).SafetyScore, Tol);
            Assert.AreEqual(40f, Score(Vehiculo(), Menos(3)).SafetyScore, Tol);
            Assert.AreEqual(20f, Score(Vehiculo(), Menos(4)).SafetyScore, Tol);
            Assert.AreEqual(0f, Score(Vehiculo()).SafetyScore, Tol);
        }

        [Test]
        public void Todos_los_esenciales_del_equipo_dan_cien()
        {
            Assert.AreEqual(100f, Score(Epp(), Casco, Calzado, Ropa).SafetyScore, Tol);
        }

        // ------------------------------------------------------------------ el promedio

        [Test]
        public void La_nota_del_modulo_es_el_promedio_de_las_dos_secciones()
        {
            SelectionScore epp = Score(Epp(), Casco, Calzado, Ropa);
            SelectionScore vehiculo = Score(Vehiculo(), Menos(2));

            FinalScore final = SelectionScore.Average(epp, vehiculo);

            Assert.AreEqual(100f, final.Epp.SafetyScore, Tol);
            Assert.AreEqual(60f, final.Vehicle.SafetyScore, Tol);
            Assert.AreEqual(80f, final.Average, Tol, "(100 + 60) / 2.");
            Assert.AreEqual(80, final.Percent);
        }

        [Test]
        public void Promediar_y_no_sumar_pesos_es_lo_que_hace_contar_al_vehiculo()
        {
            // Aquí está el motivo del cambio. El equipo tiene 65 puntos de peso y el vehículo
            // 5: sumando pesos, dejar 4 de las 5 verificaciones sin hacer apenas se nota —94,3
            // sobre 100— porque los 65 del equipo lo tapan. Promediando sale 60, que es lo que
            // de verdad pasó: una sección perfecta y la otra casi en blanco.
            SelectionScore epp = Score(Epp(), Casco, Calzado, Ropa);
            SelectionScore vehiculo = Score(Vehiculo(), Menos(4));

            Assert.AreEqual(100f, epp.SafetyScore, Tol);
            Assert.AreEqual(20f, vehiculo.SafetyScore, Tol);

            Assert.AreEqual(60f, SelectionScore.Average(epp, vehiculo).Average, Tol);
            Assert.AreEqual(94.3f, SelectionScore.Combine(epp, vehiculo).SafetyScore, Tol,
                "Lo que daba la fórmula anterior, para que se vea la diferencia.");
        }

        [Test]
        public void El_orden_de_las_dos_secciones_no_cambia_el_promedio()
        {
            SelectionScore epp = Score(Epp(), Calzado, Ropa);
            SelectionScore vehiculo = Score(Vehiculo(), Menos(1));

            Assert.AreEqual(SelectionScore.Average(epp, vehiculo).Average,
                            SelectionScore.Average(vehiculo, epp).Average, Tol);
        }

        [Test]
        public void El_promedio_se_redondea_a_un_decimal_igual_que_las_secciones()
        {
            // 38,5 y 100 promedian 69,25 → 69,3. El decimal importa: el corte está en «más de
            // 70», así que un promedio de 70,05 pasa o repite según cómo se redondee.
            FinalScore final = SelectionScore.Average(
                Score(Epp(), Calzado, Ropa), Score(Vehiculo(), TodoElVehiculo));

            Assert.AreEqual(69.3f, final.Average, Tol);
            Assert.AreEqual(69, final.Percent);
        }

        [Test]
        public void El_medio_punto_sube_como_en_la_hoja_y_no_al_par()
        {
            // REDONDEAR de Excel sube el medio: 59,25 → 59,3. `Mathf.Round` redondea al PAR y
            // le sale 59,2, que es lo que hacía este código hasta el 2026-10-29. Parece una
            // menudencia de un decimal, pero con un promedio de 70,05 al par da 70,0 y manda
            // repetir el módulo donde la hoja da 70,1 y deja pasar.
            Assert.AreEqual(59.3f, SelectionScore.Round1(59.25f), 0.001f);
            Assert.AreEqual(59.4f, SelectionScore.Round1(59.35f), 0.001f, "Y el siguiente también.");
            Assert.AreEqual(70.1f, SelectionScore.Round1(70.05f), 0.001f, "El borde del módulo.");
            Assert.AreEqual(38.5f, SelectionScore.Round1(38.4615f), 0.001f, "Lo que no cae en medio.");
        }

        // ------------------------------------------------- la tercera puerta: pasar de 70

        [Test]
        public void Con_mas_de_setenta_puede_continuar_al_modulo_dos()
        {
            FinalScore final = SelectionScore.Average(
                Score(Epp(), Casco, Calzado, Ropa), Score(Vehiculo(), Menos(2)));

            Assert.AreEqual(80f, final.Average, Tol);
            Assert.IsTrue(final.CanContinue);
            Assert.IsFalse(final.MustRepeatModule);
        }

        [Test]
        public void Setenta_justo_no_alcanza_porque_la_regla_es_mas_de_setenta()
        {
            // «Si en el promedio no tiene más de 70, entonces la persona tiene que repetir todo
            // el módulo». Más de 70, no 70 o más: el 70 exacto repite.
            FinalScore final = SelectionScore.Average(
                Score(Epp(), Casco, Calzado, Ropa),   // 100
                Score(Vehiculo(), Menos(3)));         // 40 → promedio justo 70

            Assert.AreEqual(70f, final.Average, Tol);
            Assert.IsTrue(final.MustRepeatModule, "El corte es estricto.");
            Assert.IsFalse(final.CanContinue);
        }

        [Test]
        public void Por_debajo_de_setenta_repite_el_modulo_entero_y_no_abre_el_siguiente()
        {
            FinalScore final = SelectionScore.Average(
                Score(Epp(), Calzado, Ropa),          // 38,5
                Score(Vehiculo(), Menos(1)));         // 80

            Assert.AreEqual(59.3f, final.Average, Tol, "(38,5 + 80) / 2 = 59,25 → 59,3.");
            Assert.IsTrue(final.MustRepeatModule);
            Assert.IsFalse(final.CanContinue);
            Assert.AreEqual(PerformanceRank.NeedsImprovement, final.Rank);
        }

        [Test]
        public void Un_cien_redondo_pasa_sin_discusion()
        {
            FinalScore final = SelectionScore.Average(
                Score(Epp(), Casco, Calzado, Ropa), Score(Vehiculo(), TodoElVehiculo));

            Assert.AreEqual(100f, final.Average, Tol);
            Assert.AreEqual(PerformanceRank.Knowledgeable, final.Rank);
            Assert.IsTrue(final.CanContinue);
        }

        [Test]
        public void El_corte_del_modulo_y_el_corte_del_rango_son_cosas_distintas()
        {
            // El rango aprueba en 75 y la puerta del módulo en «más de 70»: entre 70,1 y 74,9
            // se puede continuar al módulo 2 sin salir aprobado. No es un bug, son dos reglas
            // de dos reuniones distintas y las dos están en pie.
            FinalScore final = SelectionScore.Average(
                Score(Epp(), Casco, Calzado, Ropa, "H04_chanclas"),  // 85
                Score(Vehiculo(), Menos(2)));                        // 60

            Assert.AreEqual(72.5f, final.Average, Tol);
            Assert.IsTrue(final.CanContinue, "Pasa la puerta del módulo...");
            Assert.AreEqual(PerformanceRank.NeedsImprovement, final.Rank, "...sin estar aprobado.");
        }

        // --------------------------------- las dos primeras puertas: repetir solo esa sección

        [TestCase(2, true, TestName = "Dos verificaciones del vehículo sin hacer: repite la sección")]
        [TestCase(1, false, TestName = "Una sola sin hacer: avisa y sigue")]
        public void El_checkpoint_del_vehiculo_cuenta_omisiones_igual_que_el_de_los_EPP(
            int omitidas, bool repite)
        {
            SelectionScore vehiculo = Score(Vehiculo(), Menos(omitidas));

            Assert.AreEqual(omitidas, vehiculo.EssentialsMissing);
            Assert.AreEqual(repite, vehiculo.MustRepeatSelection);
            Assert.AreEqual(!repite, vehiculo.CanAdvance);
        }

        [Test]
        public void Dos_esenciales_del_equipo_sin_llevar_repiten_la_seleccion()
        {
            SelectionScore epp = Score(Epp(), Casco);

            Assert.AreEqual(2, epp.EssentialsMissing);
            Assert.IsTrue(epp.MustRepeatSelection, "≥2 esenciales: repite la parte de EPP.");
        }

        [Test]
        public void Dos_peligros_elegidos_repiten_la_seleccion_aunque_este_todo_lo_esencial()
        {
            SelectionScore epp = Score(Epp(), Casco, Calzado, Ropa, "H02_agrietado", "H04_chanclas");

            Assert.AreEqual(3, epp.Correct, "Los tres esenciales están.");
            Assert.AreEqual(2, epp.HazardsTaken);
            Assert.IsTrue(epp.MustRepeatSelection, "≥2 peligros: repite igual.");
        }

        [Test]
        public void Pasar_los_dos_checkpoints_no_garantiza_pasar_el_promedio()
        {
            // El caso que hace falta ver antes de la demo: un fallo en cada sección deja pasar
            // los dos checkpoints —cuentan errores, y uno no llega a dos— y sin embargo el
            // promedio se queda en 59,3 y manda a repetir el módulo entero. Las tres puertas
            // miden cosas distintas y esto lo deja escrito.
            SelectionScore epp = Score(Epp(), Calzado, Ropa);      // falta el casco: 1 omisión
            SelectionScore vehiculo = Score(Vehiculo(), Menos(1)); // 1 verificación sin hacer

            Assert.IsTrue(epp.CanAdvance, "Un solo olvido de EPP deja pasar a los vehículos.");
            Assert.IsTrue(vehiculo.CanAdvance, "Una sola verificación deja cerrar el vehículo.");

            FinalScore final = SelectionScore.Average(epp, vehiculo);

            Assert.AreEqual(59.3f, final.Average, Tol);
            Assert.IsTrue(final.MustRepeatModule, "Pero el promedio no pasa de 70.");
        }

        [Test]
        public void Suspender_una_seccion_entera_no_hunde_la_otra_por_debajo_de_cero()
        {
            // El suelo de 0 de cada sección importa aquí: si una nota pudiera ser negativa, un
            // desastre en los EPP se comería la nota de un vehículo revisado bien.
            SelectionScore epp = Score(Epp(), "H02_agrietado", "H04_chanclas"); // 0 esencial, −45

            Assert.AreEqual(0f, epp.SafetyScore, Tol);

            FinalScore final = SelectionScore.Average(epp, Score(Vehiculo(), TodoElVehiculo));

            Assert.AreEqual(50f, final.Average, Tol, "(0 + 100) / 2, no un número negativo.");
            Assert.IsTrue(final.MustRepeatModule);
        }

        // ------------------------------------------------------------------------ bordes

        [Test]
        public void Dos_secciones_sin_tocar_dan_cero_y_repiten()
        {
            FinalScore final = SelectionScore.Average(Score(Epp()), Score(Vehiculo()));

            Assert.AreEqual(0f, final.Average, Tol);
            Assert.AreEqual(PerformanceRank.Inexperienced, final.Rank);
            Assert.IsTrue(final.MustRepeatModule);
        }

        [Test]
        public void El_promedio_de_dos_notas_vacias_no_suspende_a_nadie()
        {
            // Un catálogo sin nada que juzgar es un error de datos, no un fallo del aprendiz:
            // las dos secciones dan el bloque entero y el promedio sale 100. Si esto diera 0,
            // un asset mal generado suspendería a todo el mundo sin que nadie supiera por qué.
            Module01CatalogSO vacio = Catalog(Zone("z"));

            FinalScore final = SelectionScore.Average(Score(vacio), Score(vacio));

            Assert.AreEqual(100f, final.Average, Tol);
            Assert.IsTrue(final.CanContinue);
        }

        [Test]
        public void El_corte_del_modulo_esta_en_el_codigo_y_no_disperso_en_numeros_sueltos()
        {
            Assert.AreEqual(70f, FinalScore.PassMark, Tol);
        }
    }
}
