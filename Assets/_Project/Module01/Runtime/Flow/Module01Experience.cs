using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cachacos;
using RideSafe.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;

namespace RideSafe.Module01
{
    /// <summary>Presentation flow shared by onboarding and the editor's quick-start command.</summary>
    public class Module01Experience : MonoBehaviour
    {
        [SerializeField] private Module01Director _director;
        [SerializeField] private Module01Binding _binding;
        [SerializeField] private GuidedTour _tour;
        [SerializeField] private ItemInspector _inspector;
        [SerializeField] private ZoneRunner _runner;
        [SerializeField] private ModuleUI _onboarding;
        [SerializeField] private Transform _door;
        [SerializeField] private Transform _camera;
        [SerializeField] private SubtitleView _subtitles;
        [SerializeField] private VideoPlayer _video;
        [SerializeField] private GameObject _sidebar;
        [SerializeField] private GameObject _navigation;
        [SerializeField] private GameObject _decision;
        [SerializeField] private GameObject _instruction;
        [SerializeField] private TMP_Text _instructionText;
        [SerializeField] private TMP_Text _summary;
        [SerializeField] private Module01CatalogSO _allItems;
        [Tooltip("Con un asset aquí, Begin salta las pantallas del módulo 0 y usa estos valores. Vacío corre el onboarding completo.")]
        [SerializeField] private Module00SettingsSO _quickStart;
        public UnityEvent onOrientation = new UnityEvent();
        public UnityEvent onLeaveOrientation = new UnityEvent();
        public string Phase { get; private set; } = "Outside";
        public bool SubtitlesEnabled { get; private set; } = true;
        /// <summary>Lo que el módulo 0 dejó decidido. Es lo que leen los requisitos de secuencia.</summary>
        public Module01Context Context { get; } = new Module01Context();
        private ILocalizationProvider _localization;
        private bool _started;
        private bool _videoComplete;
        private bool _readinessCorrect;
        private List<ComparisonEntry> _personalEntries;
        private List<ComparisonEntry> _vehicleEntries;
        private string _instructionKey;
        private readonly List<(Button button, UnityAction action)> _listeners = new List<(Button, UnityAction)>();

        private void Start()
        {
            _localization = ServiceLocator.Instance.RequestService<ILocalizationProvider>();
            if (_localization != null) _localization.LanguageChanged += RefreshInstruction;
            // Un único contexto para toda la sesión: el onboarding lo va llenando a medida que
            // el aprendiz elige, y el runner lee de ahí. Acepta asignación antes o después de su Awake.
            var service = GetComponent<RideSafe.TaskSequence.TaskSequenceService>();
            if (service != null) service.ContextLookup = Context.Lookup;
            _sidebar = _binding.Sidebar;
            _binding.Module.gameObject.SetActive(true);
            _binding.Module.HideAll();
            _sidebar.SetActive(false); _navigation.SetActive(false); _decision.SetActive(false); _instruction.SetActive(false);
            Bind(_binding.Module.Get<RectTransform>("Orientation"), "StartButton", StartZones);
            Bind(_binding.Module.Get<RectTransform>("Orientation"), "RepeatAudioButton", () => onOrientation.Invoke());
            Bind(_navigation.transform, "NextZoneButton", NextZone);
            Bind(_navigation.transform, "PreviousZoneButton", PreviousZone);
            Bind(_binding.Module.Get<RectTransform>("ReviewChoices"), "KeepChoosingButton", KeepChoosing);
            Bind(_binding.Module.Get<RectTransform>("ReviewChoices"), "ConfirmButton", Submit);
            Bind(_binding.Comparison.transform, "ExplainButton", PlayVideo);
            Bind(_binding.Explanation.transform, "PauseButton", () => { if (_video.isPlaying) _video.Pause(); else _video.Play(); });
            Bind(_binding.Explanation.transform, "ReplayButton", () => { _video.time = 0; _video.Play(); });
            Bind(_binding.Explanation.transform, "ContinueButton", FinishVideo);
            Bind(_decision.transform, "RoadReadyButton", () => Decide(ReadinessChoice.RoadReady));
            Bind(_decision.transform, "NeedsServiceButton", () => Decide(ReadinessChoice.NeedsService));
            Bind(_decision.transform, "DoNotRideButton", () => Decide(ReadinessChoice.DoNotRide));
            _director.ZoneEntered += EnterZone;
            _director.PersonalSectionCompleted += ShowPersonalReport;
            _director.VehicleSectionCompleted += ShowVehicleReport;
            _director.Finished += ShowFinalReport;
            _inspector.Accepted += _ => _navigation.SetActive(Phase == "Choosing");
            _inspector.Declined += _ => _navigation.SetActive(Phase == "Choosing");
            _video.loopPointReached += VideoEnded;
            _video.errorReceived += VideoError;
            if (_onboarding != null)
            {
                Bind(_onboarding.Get<RectTransform>("Welcome"), "BeginButton", Begin);
                var language = _onboarding.Get<ChoicePanel>("Language");
                language.onSelectionChanged.AddListener(SetLanguage);
                language.onContinue.AddListener(id => { SetLanguage(id); _onboarding.Show("Jurisdiction"); });
                // Las elecciones del módulo 0 se publican en el contexto, no solo pasan de pantalla:
                // de esto depende que elegir escooter corra sus secuencias y no las de la e-bike.
                _onboarding.Get<ChoicePanel>("Jurisdiction").onContinue.AddListener(id => { Context.Set(Module01Context.JurisdictionKey, id); _onboarding.Show("Vehicle"); });
                _onboarding.Get<ChoicePanel>("Vehicle").onContinue.AddListener(id => { Context.Set(Module01Context.VehicleKey, id); _onboarding.Show("Comfort"); });
                _onboarding.Get<ComfortSettingsPanel>("Comfort").onStart.AddListener(StartFromOnboarding);
            }
        }
        /// <summary>Begin: con parámetros de prueba asignados entra al módulo 1; si no, al onboarding.</summary>
        private void Begin()
        {
            if (_quickStart == null) { _onboarding.Show("Language"); return; }
            Apply(_quickStart);
            StartEntrance();
        }
        private void SetLanguage(string value)
        {
            _localization?.SetLanguage(value);
            Context.Set(Module01Context.LanguageKey, value);
        }
        /// <summary>Aplica una pasada escrita del módulo 0: idioma, jurisdicción, vehículo y confort.</summary>
        private void Apply(Module00SettingsSO settings)
        {
            SetLanguage(settings.Language);
            Context.Set(Module01Context.JurisdictionKey, settings.Jurisdiction);
            Context.Set(Module01Context.VehicleKey, settings.Vehicle);
            ApplyComfort(settings.Comfort);
        }
        private void ApplyComfort(ComfortSettings settings)
        {
            SubtitlesEnabled = settings.Subtitles;
            _subtitles.SetSubtitlesEnabled(settings.Subtitles);
            _subtitles.SetTextScale(settings.TextScalePercent);
        }
        private void StartFromOnboarding(ComfortSettings settings)
        {
            ApplyComfort(settings);
            StartEntrance();
        }
        /// <summary>
        /// Entrada para el menú de pruebas del Editor: salta las pantallas del módulo 0 y conserva
        /// la entrada real al garaje. Sin asset asignado no inventa valores, avisa.
        /// </summary>
        public void StartWithDefaults(Module00SettingsSO settings = null)
        {
            var scripted = settings ?? _quickStart;
            if (scripted == null) { Debug.LogError("[Module01] Falta el asset de parametros del modulo 0 en _quickStart."); return; }
            Apply(scripted);
            StartEntrance();
        }
        private void StartEntrance()
        {
            if (_started) return;
            _started = true;
            if (_onboarding != null) _onboarding.gameObject.SetActive(false);
            StartCoroutine(Entrance());
        }
        private IEnumerator Entrance()
        {
            Phase = "Entrance";
            Vector3 closed = _door.position;
            float elapsed = 0;
            while (elapsed < .8f) { elapsed += Time.unscaledDeltaTime; _door.position = Vector3.Lerp(closed, closed + Vector3.up * 3.2f, elapsed / .8f); yield return null; }
            _tour.TryGoTo("head");
            while (_tour.IsMoving) yield return null;
            _binding.Module.Show("Orientation");
            Phase = "Orientation";
            onOrientation.Invoke();
        }
        private void StartZones()
        {
            onLeaveOrientation.Invoke();
            _binding.PreparationList.Clear();
            _director.Begin();
        }
        private void EnterZone(ZoneSO zone) => StartCoroutine(Arrive(zone));
        private IEnumerator Arrive(ZoneSO zone)
        {
            Phase = "Moving";
            _runner.Begin(null);
            _binding.Module.HideAll(); _sidebar.SetActive(false); _navigation.SetActive(false); _instruction.SetActive(false);
            while (_tour.IsMoving) yield return null;
            _runner.Begin(zone);
            _inspector.SetQuestion(_director.InVehicleSection ? "Module1/Inspect_Question" : "Module1/Confirm_Question");
            _sidebar.SetActive(true); _navigation.SetActive(true);
            _instructionKey = zone.TitleKey;
            RefreshInstruction();
            Phase = "Choosing";
        }
        private void RefreshInstruction()
        {
            if (string.IsNullOrEmpty(_instructionKey)) return;
            _instructionText.text = UIText.Resolve(_instructionKey) + "\n" + UIText.Resolve(_director.InVehicleSection ? "Module1/Inspect_Instruction" : "Module1/Choose_Instruction");
            _instruction.SetActive(SubtitlesEnabled && (Phase == "Choosing" || Phase == "Moving"));
        }
        private void Update()
        {
            if (Phase == "Choosing") { _navigation.SetActive(!_inspector.IsPresenting); _instruction.SetActive(SubtitlesEnabled && !_inspector.IsPresenting); }
            if (Phase == "Video")
            {
                _binding.Explanation.SetPlaying(_video.isPlaying);
                if (_video.length > 0) _binding.Explanation.SetVideoProgress((float)(_video.time / _video.length));
            }
        }
        private void NextZone()
        {
            if (Phase != "Choosing" || _inspector.IsPresenting || _tour.IsMoving) return;
            if (!_director.IsLastZone) { _director.AdvanceZone(); return; }
            _runner.Begin(null); _navigation.SetActive(false); _instruction.SetActive(false); _sidebar.SetActive(false);
            if (_director.InVehicleSection) { Phase = "Decision"; _decision.SetActive(true); }
            else
            {
                Phase = "Review";
                _binding.Module.Show("ReviewChoices");
                var entries = _runner.Ledger.Selected.Select(id => new KeyValuePair<string,string>(id, NameOf(id)));
                _binding.Module.Get<ChecklistView>("ReviewChoices").SetItems(entries);
            }
        }
        private void PreviousZone() { if (Phase == "Choosing" && !_inspector.IsPresenting) _director.PreviousZone(); }
        private string NameOf(string id) => _allItems.TryGetItem(id, out var item) ? UIText.Resolve(item.NameKey) : id;
        private void KeepChoosing() => EnterZone(_director.CurrentZone);
        private void Submit() { if (Phase == "Review") _director.AdvanceZone(); }
        private void Decide(ReadinessChoice choice)
        {
            if (Phase != "Decision") return;
            _decision.SetActive(false);
            _director.AdvanceZone();
            _readinessCorrect = ReadinessDecision.IsCorrect(choice, _director.VehicleReport);
            _summary.text = UIText.Resolve(_readinessCorrect ? "Module1/Decision_Correct" : "Module1/Decision_Review");
            _summary.gameObject.SetActive(true);
        }
        private void ShowPersonalReport(SelectionReport report) { _personalEntries = Entries(report); ShowReport(_personalEntries); }
        private void ShowVehicleReport(SelectionReport report) { _vehicleEntries = Entries(report); ShowReport(_vehicleEntries); }
        private List<ComparisonEntry> Entries(SelectionReport report)
        {
            var ids = new HashSet<string>(report.CoreSelected.Concat(report.CoreOmitted).Concat(report.OptionalSelected).Concat(report.ConditionDependentSelected).Concat(report.InappropriateSelected));
            return ReportPresenter.BuildEntries(_allItems, report, UIText.Resolve).Where(e => ids.Contains(e.Detail)).ToList();
        }
        private void ShowReport(List<ComparisonEntry> entries)
        {
            Phase = "Report";
            _binding.Module.Show("Comparison");
            _binding.Comparison.SetContent(UIText.Resolve("Module1/Report_Title"), UIText.Resolve("Module1/Report_Subtitle"), UIText.Resolve("Module1/Report_Yours"), entries, UIText.Resolve("Module1/Report_Reference"), Reference(entries));
        }
        private List<ComparisonEntry> Reference(List<ComparisonEntry> entries) => entries.Where(e => e.Status != ItemStatus.NotSuited).Select(e => new ComparisonEntry(e.Status == ItemStatus.CoreOmitted ? ItemStatus.Selected : e.Status, e.Title, e.Detail)).ToList();
        private void PlayVideo()
        {
            if (Phase != "Report") return;
            Phase = "Video"; _summary.gameObject.SetActive(false);
            _binding.Module.Show("Explanation");
            // La pantalla toma la textura del reproductor en runtime: así no depende de un override
            // guardado en la escena, que es justo lo que se perdía y dejaba el video en negro.
            if (_binding.Explanation.VideoSurface != null) _binding.Explanation.VideoSurface.texture = _video.targetTexture;
            // Demo media can be replaced independently for the personal and vehicle reviews.
            _video.clip = _director.InVehicleSection ? _vehicleVideo : _personalVideo;
            _videoComplete = false;
            // Play() sobre un clip sin preparar se queda en prepared=true, playing=false cuando la
            // decodificacion tarda, y la pantalla se ve en negro. Se prepara primero y se arranca
            // en el callback, que no tiene carrera. waitForFirstFrame off para no esperar a que
            // alguien dibuje la Game View, que en un test de PlayMode no pasa.
            _video.waitForFirstFrame = false;
            _video.prepareCompleted -= PlayPrepared;
            _video.prepareCompleted += PlayPrepared;
            _video.Prepare();
        }
        private void PlayPrepared(VideoPlayer player)
        {
            player.prepareCompleted -= PlayPrepared;
            if (Phase == "Video") player.Play();
        }
        [SerializeField] private VideoClip _personalVideo;
        [SerializeField] private VideoClip _vehicleVideo;
        private void VideoEnded(VideoPlayer _) => _videoComplete = true;
        private void VideoError(VideoPlayer _, string message) { _videoComplete = true; Debug.LogError("[Module01] Video: " + message); }
        private void FinishVideo()
        {
            if (Phase != "Video") return;
            _video.Stop();
            _binding.Module.HideAll();
            _binding.PreparationList.Clear();
            _director.ContinueAfterReview();
        }
        private void ShowFinalReport()
        {
            var entries = new List<ComparisonEntry>(_personalEntries); entries.AddRange(_vehicleEntries);
            ShowReport(entries); Phase = "Finished";
            _summary.text = UIText.Resolve(_readinessCorrect ? "Module1/Final_Complete" : "Module1/Decision_Review");
            _summary.gameObject.SetActive(true);
            Find<Button>(_binding.Comparison.transform, "ExplainButton").gameObject.SetActive(false);
        }
        private void Bind(Transform root, string name, UnityAction action)
        { var b = Find<Button>(root, name); if (b == null) { Debug.LogError("[Module01] Falta boton " + name); return; } b.onClick.AddListener(action); _listeners.Add((b,action)); }
        public static T Find<T>(Transform root, string name) where T : Component => root.GetComponentsInChildren<T>(true).FirstOrDefault(c => c.name == name);
        private void OnDestroy()
        {
            foreach (var listener in _listeners) if (listener.button != null) listener.button.onClick.RemoveListener(listener.action);
            if (_video != null) _video.prepareCompleted -= PlayPrepared;
            if (_localization != null) _localization.LanguageChanged -= RefreshInstruction;
            if (_director != null) { _director.ZoneEntered -= EnterZone; _director.PersonalSectionCompleted -= ShowPersonalReport; _director.VehicleSectionCompleted -= ShowVehicleReport; _director.Finished -= ShowFinalReport; }
        }
    }
}




