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

        public GameObject Sidebar { get; private set; }
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
            // Resolve by panel/name at runtime: regenerated prefabs retain this contract.
            _module.gameObject.SetActive(true);
            _preparationList.gameObject.SetActive(true); // initialize its row template before hiding the panel
            Transform list = _preparationList.transform.Find("ChecklistPanel");
            if (list != null)
            {
                Sidebar = list.gameObject;
                list.SetParent(_module.transform, false);
                var rect = (RectTransform)list;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.pivot = new Vector2(.5f, 1f);
                rect.anchoredPosition = new Vector2(430, 200);
                rect.sizeDelta = new Vector2(250, 300);
            }
            Localize(OrientationPanel, "Title", "Orientation_Title");
            Localize(OrientationPanel, "Body", "Orientation_Body");
            LocalizeButton(OrientationPanel, "StartButton", "Start");
            LocalizeButton(OrientationPanel, "RepeatAudioButton", "RepeatAudio");
            Localize(ReviewPanel, "Title", "Review_Title");
            Localize(ReviewPanel, "Subtitle", "Review_Subtitle");
            Localize(ReviewPanel, "LockNoticeText", "Review_Lock");
            LocalizeButton(ReviewPanel, "KeepChoosingButton", "KeepChoosing");
            LocalizeButton(ReviewPanel, "ConfirmButton", "Confirm");
            LocalizeButton(ComparisonPanel, "ExplainButton", "WatchVideo");
            LocalizeButton(ExplanationPanel, "PauseButton", "Pause");
            LocalizeButton(ExplanationPanel, "ReplayButton", "Replay");
            LocalizeButton(ExplanationPanel, "ContinueButton", "Continue");
            Localize(ExplanationPanel, "Caption", "VideoPlaceholder");
            foreach (var panel in new[]{OrientationPanel, ExplanationPanel})
            {
                var subtitle = _module.Get<RectTransform>(panel).Find("Subtitle");
                if (subtitle != null) subtitle.gameObject.SetActive(false);
            }
            foreach (string name in new[]{"Badge", "ItemCard", "Explanation"})
            {
                var child = _explanation.transform.Find(name);
                if(child != null) child.gameObject.SetActive(false);
            }
            if (Sidebar != null)
            {
                Attach(Sidebar.transform.Find("Title").GetComponent<TMPro.TMP_Text>(), "Sidebar_Title");
                Attach(Sidebar.transform.Find("Footnote").GetComponent<TMPro.TMP_Text>(), "Sidebar_Note");
                Attach(Sidebar.transform.Find("EmptyState/Text").GetComponent<TMPro.TMP_Text>(), "Sidebar_Empty");
            }
            _preparationList.ConfigureLabels("", "", "");
            _module.HideAll();
        }

        private void Localize(string panel, string name, string key)
        { Attach(Module01Experience.Find<TMPro.TMP_Text>(_module.Get<RectTransform>(panel), name), key); }
        private void LocalizeButton(string panel, string name, string key)
        { var button = Module01Experience.Find<UnityEngine.UI.Button>(_module.Get<RectTransform>(panel), name); if(button != null) Attach(button.GetComponentInChildren<TMPro.TMP_Text>(true), key); }
        private static void Attach(TMPro.TMP_Text text, string key)
        { if(text == null) return; var label=text.GetComponent<Module01Text>(); if(label==null)label=text.gameObject.AddComponent<Module01Text>(); label.Configure("Module1/"+key); }

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



