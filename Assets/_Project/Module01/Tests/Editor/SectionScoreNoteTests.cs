using System;
using NUnit.Framework;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// La nota que se enseña al cerrar <b>cada</b> sección (documento del equipo, 2026-11-04:
    /// «evaluar de forma independiente la sección de EPPs y la de Revisión del Vehículo»).
    /// <para>
    /// Hasta hoy la puntuación solo se componía en la pantalla final, así que quien no llegaba
    /// al final del módulo no veía una nota en ninguna parte. Y esa pantalla final se va a
    /// reordenar, así que la nota se ancla aquí, a la sección, que es lo que no se mueve.
    /// </para>
    /// <para>
    /// Se prueba con <c>translate</c> identidad: así la clave de I2 sale literal y la aserción
    /// mira <b>qué</b> se dice, no cómo está traducido.
    /// </para>
    /// </summary>
    public class SectionScoreNoteTests : CatalogFixture
    {
        private static readonly Func<string, string> TalCual = k => k;

        private SelectionScore Nota(Module01CatalogSO catalog, params string[] elegidos)
        {
            SelectionLedger ledger = new SelectionLedger();
            foreach (string id in elegidos)
                ledger.Add(id);
            return SelectionScore.Of(catalog, SelectionGrader.Grade(catalog, ledger));
        }

        private Module01CatalogSO DosEsenciales() => Catalog(
            Zone("proteccion",
                Item("E_casco", SafetyItemCategory.Core, 40f),
                Item("E_calzado", SafetyItemCategory.Core, 10f)));

        [Test]
        public void La_nota_de_seccion_lleva_su_porcentaje()
        {
            SelectionScore score = Nota(DosEsenciales(), "E_casco", "E_calzado");

            string nota = SectionScoreNote.Compose(score, TalCual);

            Assert.That(nota, Does.Contain(score.Percent.ToString()),
                        "la cifra de la sección tiene que salir en su nota");
        }

        [Test]
        public void Aprobada_la_seccion_la_nota_dice_que_se_puede_seguir()
        {
            SelectionScore score = Nota(DosEsenciales(), "E_casco", "E_calzado");
            Assert.IsFalse(score.FailsExam, "premisa: con todo cogido aprueba");

            string nota = SectionScoreNote.Compose(score, TalCual);

            Assert.That(nota, Does.Contain("M1/Section_CanAdvance"));
            Assert.That(nota, Does.Not.Contain("M1/Section_MustRepeat"));
        }

        [Test]
        public void Reprobada_la_seccion_la_nota_dice_que_hay_que_repetirla()
        {
            SelectionScore score = Nota(DosEsenciales(), "E_calzado");
            Assert.IsTrue(score.FailsExam, "premisa: dejarse el casco reprueba");

            string nota = SectionScoreNote.Compose(score, TalCual);

            Assert.That(nota, Does.Contain("M1/Section_MustRepeat"));
            Assert.That(nota, Does.Not.Contain("M1/Section_CanAdvance"));
        }
    }
}
