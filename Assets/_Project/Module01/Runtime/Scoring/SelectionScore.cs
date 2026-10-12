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
                case PerformanceRank.Knowledgeable: return "Module1/Rank_Knowledgeable";
                case PerformanceRank.NeedsImprovement: return "Module1/Rank_NeedsImprovement";
                case PerformanceRank.Reconsider: return "Module1/Rank_Reconsider";
                default: return "Module1/Rank_Inexperienced";
            }
        }

        /// <summary>Clave del nombre del rango, para el encabezado de la pantalla final.</summary>
        public static string TitleKey(this PerformanceRank rank) => FeedbackKey(rank) + "_Title";
    }

    /// <summary>
    /// La nota de una sección: qué porcentaje de lo juzgable salió bien, y en qué rango cae.
    /// <para>
    /// <b>Qué se juzga:</b> los elementos de núcleo —tomarlos es acertar, omitirlos fallar— y
    /// los inapropiados —no tomarlos es acertar, tomarlos fallar—. Lo opcional y lo que depende
    /// de la condición <b>no entran</b>: penalizarlos sería exactamente la penalización falsa
    /// que la narrativa prohíbe, y premiarlos convertiría «llévatelo todo» en la estrategia
    /// ganadora.
    /// </para>
    /// <para>
    /// <b>La disposición final</b> (circular / servicio / no circular) se evalúa aparte, en
    /// <see cref="ReadinessDecision"/>, y no se mezcla aquí: con pocos elementos en juego, un
    /// go/no-go mal elegido movería la nota mucho más o mucho menos según cuántos EPP tenga el
    /// catálogo ese día, que es justo lo que no debe pasar en una evaluación.
    /// </para>
    /// <para>
    /// Sin Unity y sin escena, como el resto de <c>Scoring</c>.
    /// </para>
    /// </summary>
    public readonly struct SelectionScore
    {
        /// <summary>Aciertos sobre lo juzgable, de 0 a 100, truncado.</summary>
        public int Percent { get; }

        /// <summary>Cuántos elementos entraron en la nota.</summary>
        public int Judged { get; }

        /// <summary>Cuántos de esos salieron bien.</summary>
        public int Correct { get; }

        public PerformanceRank Rank { get; }

        public bool UnlocksNextModule => Rank.UnlocksNextModule();

        private SelectionScore(int percent, int judged, int correct, PerformanceRank rank)
        {
            Percent = percent;
            Judged = judged;
            Correct = correct;
            Rank = rank;
        }

        public static SelectionScore Of(Module01CatalogSO catalog, SelectionReport report)
        {
            if (catalog == null || report == null)
                return new SelectionScore(100, 0, 0, PerformanceRank.Knowledgeable);

            int judged = 0;
            int correct = 0;

            foreach (SafetyItemSO item in catalog.AllItems)
            {
                if (item == null)
                    continue;

                switch (item.Category)
                {
                    case SafetyItemCategory.Core:
                        judged++;
                        if (Contains(report.CoreSelected, item.ItemId))
                            correct++;
                        break;

                    case SafetyItemCategory.Inappropriate:
                        judged++;
                        if (!Contains(report.InappropriateSelected, item.ItemId))
                            correct++;
                        break;
                }
            }

            // Nada que juzgar es un error de datos, no un fallo del aprendiz: no ha hecho nada
            // mal, así que no se le suspende por ello.
            if (judged == 0)
                return new SelectionScore(100, 0, 0, PerformanceRank.Knowledgeable);

            // Truncado, nunca redondeado: redondear convertiría un 74.6 % en aprobado y abriría
            // el módulo 2 por un artefacto de coma flotante.
            int percent = correct * 100 / judged;
            return new SelectionScore(percent, judged, correct, RankOf(percent));
        }

        public static PerformanceRank RankOf(int percent)
        {
            if (percent >= 75) return PerformanceRank.Knowledgeable;
            if (percent >= 50) return PerformanceRank.NeedsImprovement;
            if (percent >= 25) return PerformanceRank.Reconsider;
            return PerformanceRank.Inexperienced;
        }

        private static bool Contains(System.Collections.Generic.IReadOnlyList<string> list, string id)
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
