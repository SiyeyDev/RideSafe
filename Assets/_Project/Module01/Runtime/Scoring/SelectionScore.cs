using System.Collections.Generic;
using UnityEngine;

namespace RideSafe.Module01
{
    /// <summary>
    /// Los cuatro rangos que fijó la reunión. Solo el primero abre el módulo 2.
    /// </summary>
    public enum PerformanceRank
    {
        /// <summary>Menos del 25 %.</summary>
        Inexperienced = 0,

        /// <summary>Del 25 al 49 %.</summary>
        Reconsider = 1,

        /// <summary>Del 50 al 74 %.</summary>
        NeedsImprovement = 2,

        /// <summary>75 % o más. Aprobado.</summary>
        Knowledgeable = 3
    }

    /// <summary>Lo que cada rango significa para la pantalla final.</summary>
    public static class PerformanceRankExtensions
    {
        /// <summary>
        /// Solo el aprobado desbloquea «Continuar al Módulo 2». Los otros tres rangos
        /// desbloquean «Repetir Módulo» y nada más, que es lo que se acordó.
        /// </summary>
        public static bool UnlocksNextModule(this PerformanceRank rank) =>
            rank == PerformanceRank.Knowledgeable;

        /// <summary>Clave del texto —y de la locución— de recomendación del rango.</summary>
        public static string FeedbackKey(this PerformanceRank rank)
        {
            switch (rank)
            {
                case PerformanceRank.Knowledgeable: return "M1/Rank_Knowledgeable";
                case PerformanceRank.NeedsImprovement: return "M1/Rank_NeedsImprovement";
                case PerformanceRank.Reconsider: return "M1/Rank_Reconsider";
                default: return "M1/Rank_Inexperienced";
            }
        }

        /// <summary>Clave del nombre del rango, para el encabezado de la pantalla final.</summary>
        public static string TitleKey(this PerformanceRank rank) => FeedbackKey(rank) + "_Title";
    }


    /// <summary>
    /// La nota de una sección, con las fórmulas de
    /// <c>RideSafe_VR_equipment_image_reference_library_2026-10-19.xlsx</c>, pestaña «Sistema de
    /// puntuación». Es la hoja que entregó el cliente y se sigue al pie de la letra.
    /// <list type="bullet">
    /// <item><c>essential_score</c> = REDONDEAR(70 × peso esencial seleccionado / peso esencial
    /// aplicable; 1), de 0 a 70;</item>
    /// <item><c>hazard_score</c> = MAX(0; 30 − deducción por grupos de peligro), de 0 a 30;</item>
    /// <item><c>safety_score</c> = los dos sumados, de 0 a 100;</item>
    /// <item><c>can_advance_to_preop</c> = faltas esenciales &lt; 2 Y peligros &lt; 2.</item>
    /// </list>
    /// <para>
    /// <b>La nota y la decisión son cosas distintas, y la hoja las separa a propósito.</b> La
    /// nota es diagnóstico; quien avanza lo deciden los dos contadores. Divergen de verdad: un
    /// solo casco roto deja la nota en 70 —falla el corte de 75— pero es un único peligro, así
    /// que la hoja manda avanzar con feedback. No es una incoherencia, es que una cosa mide
    /// cuánto sabes y la otra si hay que repetir.
    /// </para>
    /// <para>
    /// <b>Por grupo, no por elemento.</b> «Usa la penalización máxima por grupo»: los dos cascos
    /// rotos son <c>helmet_integrity</c> y descuentan 30 una vez, no 60. Igual los audífonos y
    /// los AirPods, los dos <c>auditory_occlusion</c>.
    /// </para>
    /// <para>
    /// <b>Lo opcional no entra en la nota, va a su propio marcador.</b>
    /// <c>preparedness_bonus</c> = MIN(15; suma de bonos aplicables seleccionados), «se muestra
    /// por separado; no afecta aprobación». Es la salida de la hoja a un problema real: si lo
    /// recomendado restara al no llevarlo sería la penalización falsa que la narrativa prohíbe,
    /// y si sumara a la nota, «llévatelo todo» pasaría a ser la estrategia ganadora. Así el
    /// aprendiz ve dos cifras: cuán seguro va y cuán preparado vino.
    /// </para>
    /// <para>
    /// <b>La disposición final</b> (circular / servicio / no circular) se evalúa aparte, en
    /// <see cref="ReadinessDecision"/>, y no se mezcla aquí.
    /// </para>
    /// <para>Sin Unity y sin escena, como el resto de <c>Scoring</c>.</para>
    /// </summary>
    public readonly struct SelectionScore
    {
        /// <summary>Lo que vale el bloque esencial cuando se lleva entero. De la hoja.</summary>
        public const float EssentialBlock = 70f;

        /// <summary>Lo que vale el bloque de riesgo cuando no se cae en nada. De la hoja.</summary>
        public const float HazardBlock = 30f;

        /// <summary>Tope del bono de preparación. De la hoja: MIN(15; …).</summary>
        public const float BonusCap = 15f;

        /// <summary><c>essential_score</c>, de 0 a 70.</summary>
        public float EssentialScore { get; }

        /// <summary><c>hazard_score</c>, de 0 a 30.</summary>
        public float HazardScore { get; }

        /// <summary><c>safety_score</c>: los dos bloques sumados, de 0 a 100.</summary>
        public float SafetyScore => EssentialScore + HazardScore;

        /// <summary>La nota redondeada, que es lo que se enseña en pantalla.</summary>
        public int Percent => Mathf.RoundToInt(SafetyScore);

        /// <summary>Peso esencial conseguido, antes de normalizar.</summary>
        public float EssentialEarned { get; }

        /// <summary>Peso esencial que pedía el escenario. 0 = no había nada esencial.</summary>
        public float EssentialApplicable { get; }

        /// <summary>Puntos descontados ya resueltos por grupo, sin topar.</summary>
        public float HazardDeduction { get; }

        /// <summary>
        /// <c>preparedness_bonus</c>, de 0 a 15. Va <b>aparte de la nota</b>: no suma a
        /// <see cref="SafetyScore"/> ni decide nada.
        /// </summary>
        public float PreparednessBonus { get; }

        /// <summary>Lo que sumaban los recomendados antes del tope de 15.</summary>
        public float BonusEarned { get; }

        /// <summary><c>missing_count</c> de la hoja.</summary>
        public int EssentialsMissing { get; }

        /// <summary><c>hazard_count</c> de la hoja.</summary>
        public int HazardsTaken { get; }

        /// <summary>Cuántos elementos entraron en la nota.</summary>
        public int Judged { get; }

        /// <summary>Cuántos de esos salieron bien.</summary>
        public int Correct { get; }

        public PerformanceRank Rank { get; }

        /// <summary>
        /// <c>can_advance_to_preop</c>, literal de la hoja: pasa a la inspección del vehículo
        /// quien olvide menos de 2 esenciales y se lleve menos de 2 peligros.
        /// <para>
        /// Un fallo de cualquiera de las dos clases genera feedback y deja seguir; el segundo
        /// de la misma clase obliga a <b>repetir la selección después del video</b>.
        /// </para>
        /// </summary>
        public bool CanAdvance => EssentialsMissing < 2 && HazardsTaken < 2;

        /// <summary>
        /// Lo contrario de <see cref="CanAdvance"/>: hay que volver a la mesa tras el video.
        /// </summary>
        public bool MustRepeatSelection => !CanAdvance;

        /// <summary>
        /// Abre «Continuar al Módulo 2». <b>Esto no sale de la hoja</b> —la hoja solo llega
        /// hasta la inspección preoperacional—, viene del corte de ≥75 % que fijó la reunión.
        /// </summary>
        public bool UnlocksNextModule => Rank.UnlocksNextModule();

        private SelectionScore(float essentialScore, float hazardScore,
                               float essentialEarned, float essentialApplicable,
                               float hazardDeduction, float bonusEarned,
                               int essentialsMissing, int hazardsTaken,
                               int judged, int correct, PerformanceRank rank)
        {
            EssentialScore = essentialScore;
            HazardScore = hazardScore;
            EssentialEarned = essentialEarned;
            EssentialApplicable = essentialApplicable;
            HazardDeduction = hazardDeduction;
            BonusEarned = bonusEarned;
            PreparednessBonus = bonusEarned > BonusCap ? BonusCap : bonusEarned;
            EssentialsMissing = essentialsMissing;
            HazardsTaken = hazardsTaken;
            Judged = judged;
            Correct = correct;
            Rank = rank;
        }

        private static SelectionScore Empty =>
            new SelectionScore(EssentialBlock, HazardBlock, 0f, 0f, 0f, 0f, 0, 0, 0, 0,
                               PerformanceRank.Knowledgeable);

        public static SelectionScore Of(Module01CatalogSO catalog, SelectionReport report) =>
            Of(catalog, report, null);

        /// <param name="conditions">
        /// Las condiciones del escenario activas —«rain», «night»…— contra las que se mide
        /// <see cref="SafetyItemSO.BonusCondition"/>. <c>null</c> o vacío significa que solo
        /// aplican los bonos sin condición. Van como cadenas sueltas, y no como un objeto del
        /// módulo, para que <c>Scoring</c> siga sin depender de la escena ni del contexto.
        /// </param>
        public static SelectionScore Of(Module01CatalogSO catalog, SelectionReport report,
                                        ICollection<string> conditions)
        {
            if (catalog == null || report == null)
                return Empty;

            float essentialEarned = 0f, essentialApplicable = 0f;
            int essentialsMissing = 0, hazardsTaken = 0;
            int judged = 0, correct = 0;

            // «Usa la penalización máxima por grupo»: se guarda el mayor de cada grupo y se
            // suman los grupos, no los elementos.
            Dictionary<string, float> worstByGroup = new Dictionary<string, float>();

            // Lo mismo para los pares de bono excluyentes: la clave junta a los dos, así que
            // llevarse la prenda fluorescente y el chaleco reflectivo cobra solo el mejor.
            Dictionary<string, float> bestBonusByPair = new Dictionary<string, float>();
            float bonusEarned = 0f;

            foreach (SafetyItemSO item in catalog.AllItems)
            {
                if (item == null)
                    continue;

                switch (item.Category)
                {
                    case SafetyItemCategory.Core:
                        judged++;
                        essentialApplicable += item.Weight;
                        if (Contains(report.CoreSelected, item.ItemId))
                        {
                            essentialEarned += item.Weight;
                            correct++;
                        }
                        else
                        {
                            essentialsMissing++;
                        }
                        break;

                    case SafetyItemCategory.Inappropriate:
                        judged++;
                        if (Contains(report.InappropriateSelected, item.ItemId))
                        {
                            hazardsTaken++;
                            string group = item.HazardGroup;
                            if (!worstByGroup.TryGetValue(group, out float worst) || item.Weight > worst)
                                worstByGroup[group] = item.Weight;
                        }
                        else
                        {
                            correct++;
                        }
                        break;

                    // Lo recomendado no se juzga: solo puede sumar al bono, nunca a la nota.
                    case SafetyItemCategory.Optional:
                    case SafetyItemCategory.ConditionDependent:
                        bool chosen = Contains(report.OptionalSelected, item.ItemId)
                                   || Contains(report.ConditionDependentSelected, item.ItemId);
                        if (!chosen || !BonusApplies(item, conditions))
                            break;

                        string pair = ExclusivePairKey(item);
                        if (pair == null)
                        {
                            bonusEarned += item.Weight;
                        }
                        else if (!bestBonusByPair.TryGetValue(pair, out float best) || item.Weight > best)
                        {
                            bestBonusByPair[pair] = item.Weight;
                        }
                        break;
                }
            }

            foreach (KeyValuePair<string, float> p in bestBonusByPair)
                bonusEarned += p.Value;

            // Nada que juzgar es un error de datos, no un fallo del aprendiz: no ha hecho nada
            // mal, así que no se le suspende por ello. El bono sí se conserva.
            if (judged == 0)
                return new SelectionScore(EssentialBlock, HazardBlock, 0f, 0f, 0f, bonusEarned,
                                          0, 0, 0, 0, PerformanceRank.Knowledgeable);

            float deduction = 0f;
            foreach (KeyValuePair<string, float> g in worstByGroup)
                deduction += g.Value;

            return Build(essentialEarned, essentialApplicable, deduction, bonusEarned,
                         essentialsMissing, hazardsTaken, judged, correct);
        }

        /// <summary>
        /// «Suma de bonos <b>aplicables</b> seleccionados». Sin condición declarada aplica
        /// siempre; con ella, solo si el escenario la trae activa.
        /// </summary>
        private static bool BonusApplies(SafetyItemSO item, ICollection<string> conditions)
        {
            string needed = item.BonusCondition;
            if (needed.Length == 0)
                return true;
            return conditions != null && conditions.Contains(needed);
        }

        /// <summary>
        /// Clave común a los dos elementos de un par excluyente, para que den igual el orden en
        /// que aparezcan en el catálogo. <c>null</c> si el elemento no tiene pareja.
        /// </summary>
        private static string ExclusivePairKey(SafetyItemSO item)
        {
            string other = item.ExclusiveBonusWith;
            if (other.Length == 0)
                return null;
            string mine = item.ItemId;
            return string.CompareOrdinal(mine, other) <= 0 ? mine + "|" + other : other + "|" + mine;
        }

        /// <summary>
        /// Junta las notas de las dos secciones en una sola.
        /// <para>
        /// Se suman los <b>pesos</b> y las deducciones, no se promedian los porcentajes: las dos
        /// secciones tienen distinto número de elementos, así que promediar le daría a cada
        /// punto del vehículo más peso que a un EPP sin que nadie lo haya decidido.
        /// </para>
        /// <para>
        /// Sumar las deducciones sin volver a resolver los grupos vale porque <b>la sección del
        /// vehículo no tiene ningún elemento inapropiado</b>: sus cinco puntos son todos de
        /// núcleo, así que ningún grupo de peligro cruza de una sección a la otra. Si algún día
        /// el vehículo trae trampas, esto hay que rehacerlo con el diccionario entero.
        /// </para>
        /// </summary>
        public static SelectionScore Combine(SelectionScore a, SelectionScore b)
        {
            int judged = a.Judged + b.Judged;

            // Se suma lo ganado, no lo ya topado: si una sección llegara al tope y la otra
            // también, topar dos veces regalaría puntos que la hoja no da. El tope se aplica
            // una sola vez, al construir.
            float bonus = a.BonusEarned + b.BonusEarned;

            if (judged == 0)
                return new SelectionScore(EssentialBlock, HazardBlock, 0f, 0f, 0f, bonus,
                                          0, 0, 0, 0, PerformanceRank.Knowledgeable);

            return Build(a.EssentialEarned + b.EssentialEarned,
                         a.EssentialApplicable + b.EssentialApplicable,
                         a.HazardDeduction + b.HazardDeduction,
                         bonus,
                         a.EssentialsMissing + b.EssentialsMissing,
                         a.HazardsTaken + b.HazardsTaken,
                         judged,
                         a.Correct + b.Correct);
        }

        private static SelectionScore Build(float essentialEarned, float essentialApplicable,
                                            float hazardDeduction, float bonusEarned,
                                            int essentialsMissing, int hazardsTaken,
                                            int judged, int correct)
        {
            // Sin nada esencial en juego nadie puede fallar por omisión: el bloque se da entero.
            // Es el caso de una sección que solo tuviera trampas.
            float essential = essentialApplicable > 0f
                ? Round1(EssentialBlock * essentialEarned / essentialApplicable)
                : EssentialBlock;

            float hazard = HazardBlock - hazardDeduction;
            if (hazard < 0f) hazard = 0f;

            return new SelectionScore(essential, hazard, essentialEarned, essentialApplicable,
                                      hazardDeduction, bonusEarned, essentialsMissing, hazardsTaken,
                                      judged, correct, RankOf(essential + hazard));
        }

        /// <summary>REDONDEAR(x; 1) de la hoja.</summary>
        private static float Round1(float value) => Mathf.Round(value * 10f) / 10f;

        /// <summary>
        /// El rango a partir de la nota. Se compara contra el valor <b>sin redondear a entero</b>:
        /// un 74,6 no puede convertirse en aprobado y abrir el módulo 2 por un redondeo.
        /// </summary>
        public static PerformanceRank RankOf(float score)
        {
            if (score >= 75f) return PerformanceRank.Knowledgeable;
            if (score >= 50f) return PerformanceRank.NeedsImprovement;
            if (score >= 25f) return PerformanceRank.Reconsider;
            return PerformanceRank.Inexperienced;
        }

        private static bool Contains(IReadOnlyList<string> list, string id)
        {
            if (list == null)
                return false;
            for (int i = 0; i < list.Count; i++)
                if (list[i] == id)
                    return true;
            return false;
        }
    }
}
