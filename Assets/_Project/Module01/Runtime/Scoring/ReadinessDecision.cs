namespace RideSafe.Module01
{
    /// <summary>La fila 8 de los dos checklists: disposición ante lo encontrado.</summary>
    public enum ReadinessChoice
    {
        RoadReady = 0,
        NeedsService = 1,
        DoNotRide = 2
    }

    /// <summary>
    /// Decide si la disposición elegida corresponde a lo que el aprendiz encontró.
    /// <para>
    /// Con una falla crítica presente, tanto no arrancar como mandar a servicio son
    /// respuestas válidas: el módulo enseña detección y disposición, no reparación,
    /// y no hay una única salida correcta.
    /// </para>
    /// </summary>
    public static class ReadinessDecision
    {
        public static bool IsCorrect(ReadinessChoice choice, SelectionReport report)
        {
            if (report == null)
                return false;

            return report.HasCriticalProblem
                ? choice != ReadinessChoice.RoadReady
                : choice == ReadinessChoice.RoadReady;
        }
    }
}
