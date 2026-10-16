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
        [SerializeField] private Module01CatalogSO _allItems;
        [Tooltip("Atenúa el garaje durante los videos. Opcional: sin él no hay efecto cine.")]
        [SerializeField] private TheaterMode _theater;
        [Tooltip("Barra de salida del reporte final: «Continuar al Módulo 2» y «Repetir Módulo».")]
        [SerializeField] private GameObject _result;
        [Tooltip("Con un asset aquí, Begin salta las pantallas del módulo 0 y usa estos valores. Vacío corre el onboarding completo.")]
        [SerializeField] private Module00SettingsSO _quickStart;
        public UnityEvent onOrientation = new UnityEvent();
        public UnityEvent onLeaveOrientation = new UnityEvent();

        /// <summary>
        /// El aprendiz aprobó y pulsó «Continuar al Módulo 2». Es un evento y no una llamada
        /// porque el módulo 2 todavía no existe: cuando exista, se engancha aquí sin tocar esta
        /// clase. Mientras tanto el botón sigue siendo honesto —solo se enciende al aprobar—,
        /// simplemente no lleva a ningún sitio.
        /// </summary>
        public UnityEvent onContinueToModule2 = new UnityEvent();
        public string Phase { get; private set; } = "Outside";
        public bool SubtitlesEnabled { get; private set; } = true;
        /// <summary>Lo que el módulo 0 dejó decidido. Es lo que leen los requisitos de secuencia.</summary>
        public Module01Context Context { get; } = new Module01Context();
        private ILocalizationProvider _localization;
        private bool _started;
        private bool _videoComplete;
        private bool _readinessCorrect;
        private ReadinessChoice _choice;
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
            if (_result != null)
            {
                _result.SetActive(false);
                Bind(_result.transform, "RepeatModuleButton", RepeatModule);
                Bind(_result.transform, "ContinueModule2Button", () => onContinueToModule2.Invoke());
            }
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
            // Apagar el onboarding lo hace Entrance, con la pantalla ya en negro: hacerlo aqui
            // dejaba ver el garaje un instante antes del fundido.
            StartCoroutine(Entrance());
        }
        private IEnumerator Entrance()
        {
            Phase = "Entrance";
            // A negro antes de dejar el modulo 0 y de vuelta ya dentro del garaje: el corte
            // entre la pantalla de configuracion y el mundo 3D es el salto mas brusco del
            // recorrido, y en VR un corte asi es justo lo que marea.
            yield return _tour.FadeToBlack();
            if (_onboarding != null) _onboarding.gameObject.SetActive(false);
            yield return _tour.FadeFromBlack();

            Vector3 closed = _door.position;
            float elapsed = 0;
            while (elapsed < .8f) { elapsed += Time.unscaledDeltaTime; _door.position = Vector3.Lerp(closed, closed + Vector3.up * 3.2f, elapsed / .8f); yield return null; }
            _tour.TryGoTo("head");
            while (_tour.IsMoving) yield return null;
            _binding.Module.Show("Orientation");
            Phase = "Orientation";
            onOrientation.Invoke();
        }
        /// <summary>
        /// Vuelve a empezar el módulo desde la primera zona. Es lo único que desbloquean los
        /// rangos por debajo del 75 %.
        /// </summary>
        private void RepeatModule()
        {
            if (Phase != "Finished") return;
            if (_result != null) _result.SetActive(false);
            _binding.Module.HideAll();
            _personalEntries = null;
            _vehicleEntries = null;
            _readinessCorrect = false;
            StartZones();
        }

        private void StartZones()
        {
            onLeaveOrientation.Invoke();
            _binding.PreparationList.Clear();
            // El director no consulta el contexto: quien lo tiene es esta clase. Va antes
            // de Begin, que es donde se arma el recorrido del vehiculo.
            _director.SelectVehicle(Context.Get(Module01Context.VehicleKey));
            _director.Begin();
        }
        private void EnterZone(ZoneSO zone) => StartCoroutine(Arrive(zone));
        private IEnumerator Arrive(ZoneSO zone)
        {
            Phase = "Moving";
            _runner.Begin(null);
            _binding.Module.HideAll(); _sidebar.SetActive(false); _navigation.SetActive(false); _instruction.SetActive(false);
            while (_tour.IsMoving) yield return null;
            // Y tambien a que el vehiculo acabe de girar. En las zonas del vehiculo no hay
            // teletransporte, asi que sin esta espera la zona se daba por lista al instante y
            // el aprendiz podia pulsar un punto que todavia estaba viajando: medido, el de las
            // llantas se quedaba un 5 % por debajo del borde inferior de la pantalla.
            while (_director.Stage != null && _director.Stage.IsTurning) yield return null;
            _runner.Begin(zone);
            _inspector.SetQuestion(_director.InVehicleSection ? "M1/Inspect_Question" : "M1/Confirm_Question");
            _sidebar.SetActive(true); _navigation.SetActive(true);
            _instructionKey = zone.TitleKey;
            RefreshInstruction();
            Phase = "Choosing";
        }
        private void RefreshInstruction()
        {
            if (string.IsNullOrEmpty(_instructionKey)) return;
            _instructionText.text = UIText.Resolve(_instructionKey) + "\n" + UIText.Resolve(_director.InVehicleSection ? "M1/Inspect_Instruction" : "M1/Choose_Instruction");
            _instruction.SetActive(SubtitlesEnabled && (Phase == "Choosing" || Phase == "Moving"));
        }
        private void Update()
        {
            if (Phase == "Choosing")
            {
                _navigation.SetActive(!_inspector.IsPresenting);
                _instruction.SetActive(SubtitlesEnabled && !_inspector.IsPresenting);
                // El checklist tambien se aparta mientras hay un objeto en el pedestal.
                // Al bajarlo para liberar el centro del campo visual paso a solaparse con el
                // popup de confirmacion -lo caza AssertHudDoesNotOverlap-, y ademas mientras
                // el aprendiz responde "si o no" a un objeto que tiene delante, la lista de lo
                // que lleva elegido no le aporta nada.
                _sidebar.SetActive(!_inspector.IsPresenting);
            }
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
        /// <summary>
        /// El apto / no apto del vehiculo. Solo registra la eleccion y avanza: el veredicto se
        /// lee en la nota del reporte que viene a continuacion, no en un rotulo suelto.
        /// </summary>
        private void Decide(ReadinessChoice choice)
        {
            if (Phase != "Decision") return;
            _decision.SetActive(false);
            _choice = choice;
            _director.AdvanceZone();
        }
        private void ShowPersonalReport(SelectionReport report) { _personalEntries = Entries(report); ShowReport(_personalEntries); }
        private void ShowVehicleReport(SelectionReport report)
        {
            // El informe llega por el evento porque todavia no esta publicado en el director:
            // se asigna justo antes de avisar.
            _readinessCorrect = ReadinessDecision.IsCorrect(_choice, report);
            _vehicleEntries = Entries(report);
            ShowReport(_vehicleEntries,
                UIText.Resolve(_readinessCorrect ? "M1/Decision_Correct" : "M1/Decision_Review"));
        }
        private List<ComparisonEntry> Entries(SelectionReport report)
        {
            var ids = new HashSet<string>(report.CoreSelected.Concat(report.CoreOmitted).Concat(report.OptionalSelected).Concat(report.ConditionDependentSelected).Concat(report.InappropriateSelected));
            return ReportPresenter.BuildEntries(_allItems, report, UIText.Resolve).Where(e => ids.Contains(e.Detail)).ToList();
        }
        /// <summary>
        /// «Mi Elección»: un único panel con lo que el aprendiz eligió.
        /// <para>
        /// Antes pintaba dos tablas, la suya y una «Referencia para este recorrido» derivada de
        /// la primera. La reunión la mandó quitar, y con razón: la referencia se calculaba desde
        /// las mismas filas cambiando los estados, así que no aportaba información nueva y
        /// duplicaba doce renglones en una pantalla que ya iba justa.
        /// </para>
        /// </summary>
        private void ShowReport(List<ComparisonEntry> entries, string verdict = null)
        {
            Phase = "Report";
            _binding.Module.Show("Comparison");
            // El veredicto de la decision va aqui, en la nota del panel, y no en un rotulo
            // flotante sobre el HUD: medido en PlayMode, aquel se cruzaba con "Ver explicacion".
            string note = UIText.Resolve("M1/MyChoice_Note");
            // Separador de punto medio, el mismo que usa el titulo dos lineas mas abajo. Antes
            // eran tres espacios y en la captura se leia como un hueco raro, no como dos frases.
            if (!string.IsNullOrEmpty(verdict))
                note += "  ·  " + verdict;
            _binding.Comparison.SetSingleList(
                UIText.Resolve("M1/MyChoice_Title"),
                note,
                entries);
        }

        /// <summary>
        /// El mismo panel, con el veredicto en su encabezado. Separar el rango en una tarjeta
        /// aparte no cabía —medido: por debajo de la leyenda quedan unos 80 de los 556 del
        /// marco— y además partía en dos lo que la reunión pidió unificar.
        /// </summary>
        private void ShowVerdict(List<ComparisonEntry> entries, SelectionScore score)
        {
            Phase = "Report";
            _binding.Module.Show("Comparison");
            _binding.Comparison.SetSingleList(
                UIText.Resolve("M1/MyChoice_Title") + "  ·  "
                    + UIText.Resolve(score.Rank.TitleKey()) + "  ·  " + score.Percent + "%",
                UIText.Resolve(score.Rank.FeedbackKey()) + "  ·  "
                    + UIText.Resolve(_readinessCorrect ? "M1/Final_Complete" : "M1/Decision_Review"),
                entries);
        }
        private void PlayVideo()
        {
            if (Phase != "Report") return;
            Phase = "Video";
            _binding.Module.Show("Explanation");
            // No se puede saltar el video: la reunion lo pidio. Se apaga el boton en vez de
            // ignorar el clic, porque un boton que responde y no hace nada parece roto.
            _binding.Explanation.SetContinueEnabled(false);
            // Y se apaga el garaje alrededor, que es el "efecto cine".
            if (_theater != null) _theater.SetOn(true);
            // La pantalla toma la textura del reproductor en runtime: así no depende de un override
            // guardado en la escena, que es justo lo que se perdía y dejaba el video en negro.
            if (_binding.Explanation.VideoSurface != null) _binding.Explanation.VideoSurface.texture = _video.targetTexture;
            // Demo media can be replaced independently for the personal and vehicle reviews.
            _video.clip = _director.InVehicleSection ? _vehicleVideo : _personalVideo;
            _videoComplete = false;
            StartCoroutine(PrepareAndPlay());
        }

        /// <summary>
        /// Prepara el clip y lo arranca.
        /// <para>
        /// <b>Esperando a <c>isPrepared</c>, no al evento <c>prepareCompleted</c>.</b> Play()
        /// sobre un clip sin preparar se queda en <c>prepared=true, playing=false</c> y la
        /// pantalla sale negra, así que hay que preparar primero; pero colgarse del evento tiene
        /// su propio fallo: <b>si el reproductor ya estaba preparado, <c>Prepare()</c> no vuelve
        /// a emitirlo</b> y el video se queda esperando un aviso que no llega. Sondear el estado
        /// no tiene esa carrera.
        /// </para>
        /// <para>
        /// <c>waitForFirstFrame</c> en false para no esperar a que alguien dibuje la Game View,
        /// que en un test de PlayMode no pasa. Y si la preparación no llega, se desbloquea el
        /// botón igual: un video que no carga no puede encerrar al aprendiz en la pantalla.
        /// </para>
        /// </summary>
        private IEnumerator PrepareAndPlay()
        {
            _video.waitForFirstFrame = false;

            // Stop() antes de preparar, siempre: devuelve el reproductor a un estado limpio y es
            // barato aunque no estuviera reproduciendo.
            _video.Stop();

            if (!_video.isPrepared)
            {
                _video.Prepare();
                float until = Time.realtimeSinceStartup + 10f;
                while (!_video.isPrepared && Time.realtimeSinceStartup < until)
                    yield return null;
            }

            if (!_video.isPrepared)
            {
                Debug.LogError("[Module01] El video no llego a prepararse; se desbloquea Continuar.");
                _videoComplete = true;
                _binding.Explanation.SetContinueEnabled(true);
                yield break;
            }

            if (Phase != "Video")
            {
                Debug.LogWarning($"[Module01] La fase paso a '{Phase}' mientras el video se preparaba; no se arranca.");
                yield break;
            }

            _video.Play();

            // Un reintento, y despues rendirse abriendo la puerta.
            //
            // VideoPlayer puede devolver playing=false y paused=true en el mismo fotograma de
            // Play(), sin error ni excepcion. La causa mas comun no es el clip: es que la
            // aplicacion no tiene el foco —Unity pausa el video entonces—, que es lo que pasa
            // siempre en una corrida automatizada. Tambien puede serlo un archivo que la maquina
            // no sepa decodificar.
            //
            // Pase lo que pase, lo que NO puede ocurrir es que el aprendiz se quede encerrado en
            // una pantalla cuyo boton solo se abre al terminar un video que nunca empieza.
            float hasta = Time.realtimeSinceStartup + 1.5f;
            while (!_video.isPlaying && Phase == "Video" && Time.realtimeSinceStartup < hasta)
                yield return null;
            if (!_video.isPlaying && Phase == "Video")
                _video.Play();

            hasta = Time.realtimeSinceStartup + 4f;
            while (!_video.isPlaying && Phase == "Video" && Time.realtimeSinceStartup < hasta)
                yield return null;

            if (!_video.isPlaying && Phase == "Video")
            {
                // Aviso, no error: es una condicion de la que el modulo sabe salir, y marcarla
                // como error tiñe de rojo cualquier test que pase por aqui.
                Debug.LogWarning(
                    "[Module01] El video no arranca (prepared=" + _video.isPrepared + ", paused=" + _video.isPaused
                    + "). Se desbloquea Continuar para no dejar al aprendiz encerrado en una pantalla cuyo boton "
                    + "solo abre al terminar un video que nunca empieza.");
                _videoComplete = true;
                _binding.Explanation.SetContinueEnabled(true);
            }
        }
        [SerializeField] private VideoClip _personalVideo;
        [SerializeField] private VideoClip _vehicleVideo;
        private void VideoEnded(VideoPlayer _) { _videoComplete = true; _binding.Explanation.SetContinueEnabled(true); }
        /// <summary>
        /// Un video que no carga tambien desbloquea el boton: si no, un archivo roto deja al
        /// aprendiz encerrado en una pantalla de la que no se puede salir.
        /// </summary>
        private void VideoError(VideoPlayer _, string message)
        {
            _videoComplete = true;
            _binding.Explanation.SetContinueEnabled(true);
            Debug.LogError("[Module01] Video: " + message);
        }
        private void FinishVideo()
        {
            if (Phase != "Video") return;
            if (!_videoComplete) return;
            if (_theater != null) _theater.SetOn(false);
            _video.Stop();
            _binding.Module.HideAll();
            _binding.PreparationList.Clear();
            _director.ContinueAfterReview();
        }
        /// <summary>
        /// Pantalla final: «Mi Elección», la nota con su rango y lo que esa nota desbloquea.
        /// <para>
        /// La nota se calcula <b>aquí</b> y no durante el recorrido, que es el «checkpoint
        /// final» que pidió la reunión: hasta este punto el aprendiz puede volver atrás y
        /// cambiar de opinión, así que mostrarle una nota antes sería puntuar un borrador.
        /// </para>
        /// </summary>
        private void ShowFinalReport()
        {
            var entries = new List<ComparisonEntry>(_personalEntries); entries.AddRange(_vehicleEntries);
            SelectionScore score = _director.FinalScore;
            ShowVerdict(entries, score);
            Phase = "Finished";
            Find<Button>(_binding.Comparison.transform, "ExplainButton").gameObject.SetActive(false);

            if (_result != null) _result.SetActive(true);
            // Solo el aprobado abre el modulo 2. Los demas rangos dejan repetir, y nada mas:
            // el boton de continuar se apaga en vez de desaparecer para que se vea que existe
            // y que hay algo que conseguir.
            SetInteractable(_result, "ContinueModule2Button", score.UnlocksNextModule);
            SetInteractable(_result, "RepeatModuleButton", true);
        }

        private static void SetInteractable(GameObject root, string name, bool value)
        {
            if (root == null) return;
            Button button = Find<Button>(root.transform, name);
            if (button != null) button.interactable = value;
        }
        private void Bind(Transform root, string name, UnityAction action)
        { var b = Find<Button>(root, name); if (b == null) { Debug.LogError("[Module01] Falta boton " + name); return; } b.onClick.AddListener(action); _listeners.Add((b,action)); }
        public static T Find<T>(Transform root, string name) where T : Component => root.GetComponentsInChildren<T>(true).FirstOrDefault(c => c.name == name);
        private void OnDestroy()
        {
            foreach (var listener in _listeners) if (listener.button != null) listener.button.onClick.RemoveListener(listener.action);
            if (_localization != null) _localization.LanguageChanged -= RefreshInstruction;
            if (_director != null) { _director.ZoneEntered -= EnterZone; _director.PersonalSectionCompleted -= ShowPersonalReport; _director.VehicleSectionCompleted -= ShowVehicleReport; _director.Finished -= ShowFinalReport; }
        }
    }
}




