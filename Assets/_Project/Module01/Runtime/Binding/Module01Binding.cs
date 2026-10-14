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
                // Medido: con 250 de ancho la fila queda en 188 y la etiqueta, que se estira
                // con 44 por la izquierda y 90 por la derecha, en 54 px. Por eso "Casco
                // deteriorado" salia partido en trozos de seis letras. El espanol necesita
                // mas sitio que el ingles con el que se diseno el kit.
                // A la derecha y abajo, para dejar libre el centro del campo visual: en el
                // centro es donde esta el objeto que el aprendiz tiene que mirar.
                //
                // 520 de ancho y no 420. Medido en PlayMode: con 420, la nota al pie en espanol
                // ocupaba 128 px de alto -o sea que envolvia a mas lineas de las que el layout
                // habia contado- y su primera linea se montaba sobre la ultima fila del
                // checklist. Es la misma causa de siempre: el espanol es mas largo que el ingles
                // con el que se diseno el kit.
                rect.anchoredPosition = new Vector2(520, 60);
                rect.sizeDelta = new Vector2(520, 300);

                // Y que acompañe la vista en vez de ir clavado a la cabeza. Una UI pegada a la
                // cabeza 1:1 es la causa habitual de incomodidad en VR; esta solo se mueve
                // cuando el aprendiz gira lo suficiente, y vuelve despacio.
                GazeFollower follower = Sidebar.GetComponent<GazeFollower>();
                if (follower == null)
                    follower = Sidebar.AddComponent<GazeFollower>();
                // 1.6 m de distancia, 10° por debajo de la linea de ojos y 24° a la derecha.
                // Sin el desplazamiento lateral el componente deja el checklist justo delante
                // del objeto a inspeccionar, que es lo contrario de lo que se pidio.
                follower.Configure(1.6f, 10f, 24f);
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
            // El boton se coloca DEBAJO de la lista, no en un sitio fijo: la columna crece con
            // las filas y a partir de 7 se le echaba encima.
            var explain = Module01Experience.Find<UnityEngine.UI.Button>(
                _module.Get<RectTransform>(ComparisonPanel), "ExplainButton");
            if (_comparison != null && explain != null)
                _comparison.KeepBelow((RectTransform)explain.transform, 24f);
            // La leyenda estaba escrita a mano en ingles y nunca pasaba por I2, asi que salia
            // en ingles incluso con el resto del modulo en espanol.
            // Ya no se localiza ninguna leyenda al pie: no la hay. La palabra del estado la
            // resuelve ComparisonView fila por fila, con las mismas claves Module1/Legend_*.
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

        /// <summary>
        /// Cuelga la clave de I2 del texto que se llame <paramref name="name"/> dentro del panel.
        /// <para>
        /// Avisa si no lo encuentra. Antes callaba, y por eso renombrar los cuatro objetos de la
        /// leyenda la dejó entera en inglés —con el resto del módulo en español— sin que ningún
        /// test se enterara: solo se vio en una captura de PlayMode.
        /// </para>
        /// </summary>
        private void Localize(string panel, string name, string key)
        {
            TMPro.TMP_Text text = Module01Experience.Find<TMPro.TMP_Text>(_module.Get<RectTransform>(panel), name);
            if (text == null)
            {
                Debug.LogWarning(
                    $"[Module01] En el panel '{panel}' no hay ningun texto llamado '{name}': " +
                    $"la clave Module1/{key} no se aplica y ese texto saldra como lo dejo el prefab.", this);
                return;
            }
            Attach(text, key);
        }
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



