using NUnit.Framework;
using RideSafe.UI;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// La regla del examen por sección (documento del equipo, 2026-11-04): una sección se
    /// reprueba con la nota <b>por debajo de 70</b> o con <b>2 o más omisiones</b>, y entonces
    /// toca ver el video y repetir la selección.
    /// <para>
    /// Es una regla <b>aparte</b> de <see cref="SelectionScore.CanAdvance"/>, que sale de la hoja
    /// del cliente y solo cuenta esenciales y peligros, sin mirar la nota. Las dos conviven: la
    /// de la hoja decide lo que la hoja decide, y ésta es la del examen.
    /// </para>
    /// </summary>
    public class ExamRuleTests : CatalogFixture
    {
        private SelectionScore Score(Module01CatalogSO catalog, params string[] chosen)
        {
            SelectionLedger ledger = new SelectionLedger();
            foreach (string id in chosen)
                ledger.Add(id);
            return SelectionScore.Of(catalog, SelectionGrader.Grade(catalog, ledger));
        }

        /// <summary>Dos esenciales desiguales: dejarse el gordo hunde la nota con una sola falta.</summary>
        private Module01CatalogSO DosEsenciales() => Catalog(
            Zone("proteccion",
                Item("E_gordo", SafetyItemCategory.Core, 40f),
                Item("E_chico", SafetyItemCategory.Core, 10f)));

        /// <summary>Diez esenciales iguales: faltar dos deja la nota alta y aun así reprueba.</summary>
        private Module01CatalogSO DiezEsenciales()
        {
            SafetyItemSO[] items = new SafetyItemSO[10];
            for (int i = 0; i < items.Length; i++)
                items[i] = Item("E" + i, SafetyItemCategory.Core, 10f);
            return Catalog(Zone("vehiculo", items));
        }

        [Test]
        public void Con_todo_lo_esencial_y_sin_peligros_la_seccion_aprueba()
        {
            SelectionScore score = Score(DosEsenciales(), "E_gordo", "E_chico");

            Assert.That(score.EssentialsMissing, Is.Zero, "premisa: no falta ningún esencial");
            Assert.That(score.FailsExam, Is.False, "sin faltas y con la nota alta no se repite");
        }

        [Test]
        public void Con_la_nota_bajo_70_la_seccion_reprueba_aunque_solo_falte_un_esencial()
        {
            SelectionScore score = Score(DosEsenciales(), "E_chico");

            Assert.That(score.EssentialsMissing, Is.EqualTo(1), "premisa: falta uno solo");
            Assert.That(score.SafetyScore, Is.LessThan(70f), "premisa: la nota queda bajo el corte");
            Assert.That(score.FailsExam, Is.True, "bajo 70 se repite, aunque la hoja dejara avanzar");
        }

        [Test]
        public void Dos_omisiones_reprueban_aunque_la_nota_pase_de_70()
        {
            SelectionScore score = Score(DiezEsenciales(),
                "E0", "E1", "E2", "E3", "E4", "E5", "E6", "E7");

            Assert.That(score.EssentialsMissing, Is.EqualTo(2), "premisa: dos omisiones");
            Assert.That(score.SafetyScore, Is.GreaterThanOrEqualTo(70f),
                        "premisa: la nota por sí sola dejaría pasar");
            Assert.That(score.FailsExam, Is.True, "dos omisiones mandan repetir por sí solas");
        }
    }
}
