using System.Collections.Generic;

namespace RideSafe.Module01
{
    /// <summary>
    /// Resultado de una sección. Las listas se mantienen separadas a propósito:
    /// mezclar lo opcional con lo omitido produciría exactamente la penalización
    /// falsa que la narrativa prohíbe.
    /// </summary>
    public class SelectionReport
    {
        public IReadOnlyList<string> CoreSelected { get; }
        public IReadOnlyList<string> CoreOmitted { get; }
        public IReadOnlyList<string> ConditionDependentSelected { get; }
        public IReadOnlyList<string> OptionalSelected { get; }
        public IReadOnlyList<string> InappropriateSelected { get; }

        /// <summary>Hay núcleo omitido o inapropiado elegido. Lo opcional nunca cuenta.</summary>
        public bool HasCriticalProblem => CoreOmitted.Count > 0 || InappropriateSelected.Count > 0;

        public SelectionReport(List<string> coreSelected, List<string> coreOmitted,
                               List<string> conditionDependentSelected, List<string> optionalSelected,
                               List<string> inappropriateSelected)
        {
            CoreSelected = coreSelected;
            CoreOmitted = coreOmitted;
            ConditionDependentSelected = conditionDependentSelected;
            OptionalSelected = optionalSelected;
            InappropriateSelected = inappropriateSelected;
        }
    }
}
