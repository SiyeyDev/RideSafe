using System.Collections.Generic;
using UnityEngine;
using RideSafe.UI;

namespace RideSafe.Module01
{
    /// <summary>
    /// Conecta el PF_Module01_UI generado con la lógica del módulo: la lista lateral,
    /// los tableros comparativos y la vista de explicación.
    /// <para>
    /// Nada se guarda como override en el prefab, porque RideSafeUIPrefabBuilder lo
    /// regenera con ids internos nuevos y esos overrides desaparecerían en silencio.
    /// El binding localiza todo por id de panel. Si algo falta, dice exactamente qué,
    /// y <see cref="FindMissing"/> permite que un test lo detecte antes de abrir la escena.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/Module 01/Module 01 Binding")]
    public class Module01Binding : MonoBehaviour
    {
        public const string OrientationPanel = "Orientation";
        public const string PreparationPanel = "Preparation";
        public const string ReviewPanel = "ReviewChoices";
        public const string DiagnosticPanel = "Diagnostic";
        public const string ComparisonPanel = "Comparison";
        public const string ExplanationPanel = "Explanation";

        [SerializeField] private ModuleUI _module;

        private ChecklistView _preparationList;
        private ChecklistView _diagnosticList;
        private ComparisonView _comparison;
        private ExplanationView _explanation;

        public ModuleUI Module => _module;
        public ChecklistView PreparationList => _preparationList;
        public ChecklistView DiagnosticList => _diagnosticList;
        public ComparisonView Comparison => _comparison;
        public ExplanationView Explanation => _explanation;

        private void Awake()
        {
            if (_module == null)
                _module = GetComponentInChildren<ModuleUI>(true);

            if (_module == null)
            {
                Debug.LogError("[Module01] No se encontro ModuleUI bajo el binding.", this);
                return;
            }

            List<string> missing = FindMissing(_module);
            if (missing.Count > 0)
            {
                Debug.LogError("[Module01] PF_Module01_UI ya no coincide con el binding. Falta: " +
                               string.Join(", ", missing) + ". Fue regenerado con nombres distintos?", this);
                return;
            }

            _preparationList = _module.Get<ChecklistView>(PreparationPanel);
            _diagnosticList = _module.Get<ChecklistView>(DiagnosticPanel);
            _comparison = _module.Get<ComparisonView>(ComparisonPanel);
            _explanation = _module.Get<ExplanationView>(ExplanationPanel);
        }

        /// <summary>Qué espera el binding y no está. Vacío significa que el contrato se cumple.</summary>
        public static List<string> FindMissing(ModuleUI module)
        {
            List<string> missing = new List<string>();
            if (module == null)
            {
                missing.Add("ModuleUI");
                return missing;
            }

            if (module.Get<ChecklistView>(PreparationPanel) == null)
                missing.Add(PreparationPanel + "/ChecklistView");
            if (module.Get<ChecklistView>(DiagnosticPanel) == null)
                missing.Add(DiagnosticPanel + "/ChecklistView");
            if (module.Get<ComparisonView>(ComparisonPanel) == null)
                missing.Add(ComparisonPanel + "/ComparisonView");
            if (module.Get<ExplanationView>(ExplanationPanel) == null)
                missing.Add(ExplanationPanel + "/ExplanationView");

            return missing;
        }
    }
}
