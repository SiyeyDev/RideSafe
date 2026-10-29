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
        private Button _previousZoneButton;
        [SerializeField] private GameObject _decision;
        [SerializeField] private GameObject _instruction;
        [SerializeField] private TMP_Text _instructionText;
        [SerializeField] private Module01CatalogSO _allItems;
        [Tooltip("Atenúa el garaje durante los videos. Opcional: sin él no hay efecto cine.")]
        [SerializeField] private TheaterMode _theater;
        [Tooltip("Burbuja oscura alrededor de la cabeza mientras hay un punto del vehiculo abierto.")]
        [SerializeField] private FocusDome _focus;
        [Tooltip("Barra de salida del reporte final: «Continuar al Módulo 2» y «Repetir Módulo».")]
        [SerializeField] private GameObject _result;

        [Tooltip("Panel de la nota final: las dos secciones, su promedio y el veredicto.")]
        [SerializeField] private GameObject _finalScorePanel;
        // El panel aparte de la nota final y sus textos: desde el 2026-10-29 la nota va dentro de
        // «Mi Elección» (ScoreNote) y el panel solo se apaga. Siguen cableados por el constructor.
        [SerializeField] private TMP_Text _finalAverageText;
        [SerializeField] private TMP_Text _finalSectionsText;
        [SerializeField] private TMP_Text _finalVerdictText;
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
        /// <summary>The language the module 0 screens open in (PO, 2026-10-29).</summary>
        public const string DefaultLanguage = "en";
        private bool _started;
        private bool _videoComplete;
        private bool _readinessCorrect;
        private ReadinessChoice _choice;
        private List<ComparisonEntry> _personalEntries;
        private List<ComparisonEntry> _vehicleEntries;
        /// <summary>The vehicle section's result, kept for the go/no-go that now comes after its review.</summary>
        private SelectionReport _vehicleReport;
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
            _previousZoneButton = Find<Button>(_navigation.transform, "PreviousZoneButton");
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
            // Foco al revisar (Cindy, 2026-10-23): con el popup abierto se oscurece todo -el vehiculo
            // incluido- menos el popup y el pedestal, con fundido. Desde el 2026-10-26 tambien en las
            // mesas de EPP, para que destaque el elemento elegido: las tres quedan a 2,6 m o mas del
            // punto de vista y el radio es 1,6 (FocusDomeSceneTests).
            _inspector.Presenting += _ => { if (_focus != null) _focus.SetOn(true); };
            _inspector.Accepted += _ => { if (_focus != null) _focus.SetOn(false); };
            _inspector.Declined += _ => { if (_focus != null) _focus.SetOn(false); };
            _inspector.Dismissed += () => { if (_focus != null && _focus.IsOn) _focus.SetOn(false); };
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
                // Atrás, a la pantalla anterior. Cindy, 2026-10-21: lo pulsaba y no pasaba nada,
                // porque se cableó cada Continuar y ningún Atrás. Vehículo no lleva: tiene «Cambiar».
                language.onBack.AddListener(() => _onboarding.Show("Welcome"));
                _onboarding.Get<ChoicePanel>("Jurisdiction").onBack.AddListener(() => _onboarding.Show("Language"));
                Bind(_onboarding.Get<RectTransform>("Comfort"), "BackButton", () => _onboarding.Show("Vehicle"));
                // Every session opens in English (PO, 2026-10-29), whatever the device's language or
                // the one chosen last time (I2 remembers it); the learner picks theirs on the next screen.
                SetLanguage(DefaultLanguage);
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
            // La primera estación sale del catálogo, no de un nombre escrito aquí. Con las
            // mesas de la biblioteca del cliente «head» dejó de existir, y un TryGoTo a una
            // zona que no está no falla: deja al aprendiz plantado en la puerta mirando el
            // garaje, sin un solo error en consola.
            _tour.TryGoTo(FirstZoneId());
            while (_tour.IsMoving) yield return null;
            _binding.Module.Show("Orientation");
            Phase = "Orientation";
            onOrientation.Invoke();
        }
        /// <summary>
        /// La primera estación del recorrido, tomada del catálogo.
        /// <para>
        /// Antes esto era el literal <c>"head"</c>. Al pasar a las mesas de la biblioteca del
        /// cliente esa zona dejó de existir, y <see cref="GuidedTour.TryGoTo"/> con un id
        /// desconocido <b>no revienta</b>: devuelve false y el aprendiz se queda en la puerta
        /// sin que nada lo avise. Derivarlo del dato es lo que impide que vuelva a pasar al
        /// próximo cambio de catálogo.
        /// </para>
        /// </summary>
        private string FirstZoneId()
        {
            if (_allItems != null)
                foreach (ZoneSO zone in _allItems.Zones)
                    if (zone != null && !string.IsNullOrWhiteSpace(zone.ZoneId))
                        return zone.ZoneId;

            Debug.LogError("[Module01] El catálogo no tiene ninguna zona: el recorrido no "
                           + "puede empezar. Revisa SO_Module01Catalog.", this);
            return string.Empty;
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
            _vehicleReport = null;
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
            // Sin estación anterior en la primera de cada sección, el botón no se enseña: ahí no
            // hacía nada (Cindy, 2026-10-21).
            if (_previousZoneButton != null) _previousZoneButton.gameObject.SetActive(!_director.IsFirstZone);
            _instructionKey = zone.TitleKey;
            RefreshInstruction();
            Phase = "Choosing";
        }
        private void RefreshInstruction()
        {
            if (string.IsNullOrEmpty(_instructionKey)) return;
            // The station's name as a heading over the instruction, not one more line of the same weight.
            _instructionText.text = "<b><size=112%>" + UIText.Resolve(_instructionKey) + "</size></b>\n" +
                UIText.Resolve(_director.InVehicleSection ? "M1/Inspect_Instruction" : "M1/Choose_Instruction");
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
            // The vehicle section goes straight to its review («Mi Elección»); the go/no-go comes
            // after it and its video, right before the final report (Cindy, 2026-10-20).
            if (_director.InVehicleSection) _director.AdvanceZone();
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
        /// El apto / no apto del vehiculo, después de su «Mi Elección» y su video. Registra la
        /// elección contra el informe del vehículo y cierra el módulo: el veredicto se lee en
        /// el reporte final, no en un rotulo suelto.
        /// </summary>
        private void Decide(ReadinessChoice choice)
        {
            if (Phase != "Decision") return;
            _decision.SetActive(false);
            _choice = choice;
            _readinessCorrect = _vehicleReport != null && ReadinessDecision.IsCorrect(_choice, _vehicleReport);
            _director.ContinueAfterReview();
        }
        private void ShowPersonalReport(SelectionReport report) { _personalEntries = Entries(report); ShowReport(_personalEntries); }
        private void ShowVehicleReport(SelectionReport report)
        {
            // El informe llega por el evento porque todavia no esta publicado en el director:
            // se asigna justo antes de avisar. Se guarda para la decisión, que ahora viene después.
            _vehicleReport = report;
            _vehicleEntries = Entries(report);
            ShowReport(_vehicleEntries);
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
        private void ShowReport(List<ComparisonEntry> entries)
        {
            Phase = "Report";
            _binding.Module.Show("Comparison");
            // Sin nota bajo la lista (PO, 2026-10-29: «quitar ese texto inferior»): la lista y el
            // botón de la explicación, nada más.
            _binding.Comparison.SetSingleList(UIText.Resolve("M1/MyChoice_Title"), string.Empty, entries);
        }

        /// <summary>
        /// El mismo panel, con el veredicto en su encabezado. Separar el rango en una tarjeta
        /// aparte no cabía —medido: por debajo de la leyenda quedan unos 80 de los 556 del
        /// marco— y además partía en dos lo que la reunión pidió unificar.
        /// </summary>
        /// <para>
        /// La nota final ya no es una frase al pie: es la puntuación, dentro del mismo panel y bajo
        /// la lista (PO, 2026-10-29: «se ve todo muy pegado y feo, el texto de puntuación se puede
        /// poner en ese mismo panel»). Antes iba en un panel aparte montado encima de la lista.
        /// El porcentaje sale del título: lo dice la puntuación, en grande.
        /// </para>
        private void ShowVerdict(List<ComparisonEntry> entries, PerformanceRank rank, FinalScore final)
        {
            Phase = "Report";
            _binding.Module.Show("Comparison");
            _binding.Comparison.SetSingleList(
                UIText.Resolve("M1/MyChoice_Title") + "  ·  " + UIText.Resolve(rank.TitleKey()),
                ScoreNote(final),
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
            _binding.Explanation.ShowVideo(_video.targetTexture);
            // Demo media can be replaced independently for the personal and vehicle reviews.
            // Cada vehículo trae el suyo: la revisión de la bici no es la de la patineta.
            _video.clip = _director.InVehicleSection ? _director.SelectedVehicle.ExplanationVideo : _personalVideo;
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
            // After the vehicle's video comes its go/no-go; Decide closes the module.
            if (_director.InVehicleSection) { Phase = "Decision"; _decision.SetActive(true); return; }
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
            // Una sola cifra manda en toda la pantalla. Antes la barra de rango salía de
            // Combine (suma de pesos) y el panel del promedio de Average: dos números
            // distintos para la misma nota, uno encima del otro.
            FinalScore final = _director.ModuleScore;
            ShowVerdict(entries, final.Rank, final);
            Phase = "Finished";
            Find<Button>(_binding.Comparison.transform, "ExplainButton").gameObject.SetActive(false);
            // La puntuación va dentro de «Mi Elección»; el panel aparte ya no se enseña.
            if (_finalScorePanel != null) _finalScorePanel.SetActive(false);

            if (_result != null) _result.SetActive(true);
            // Solo el aprobado abre el modulo 2. Los demas rangos dejan repetir, y nada mas:
            // el boton de continuar se apaga en vez de desaparecer para que se vea que existe
            // y que hay algo que conseguir.
            SetInteractable(_result, "ContinueModule2Button", final.CanContinue);
            SetInteractable(_result, "RepeatModuleButton", true);
        }

        /// <summary>
        /// La nota final, para la caja bajo la lista de «Mi Elección»: el título, el promedio en
        /// grande, de dónde sale, y si pasa o repite el módulo entero.
        /// <para>
        /// El veredicto se escribe con palabras y no solo con el número: «59 de 100» no le dice
        /// a nadie si puede seguir. Los textos salen de I2, nunca literales. Los tamaños son
        /// relativos al cuerpo de la nota (15): el número, a 46 como en el panel de antes.
        /// </para>
        /// </summary>
        private string ScoreNote(FinalScore final)
        {
            string sections = string.Format("{0} {1}   ·   {2} {3}",
                Traducir("M1/FinalScore_Gear"), Mathf.RoundToInt(final.Epp.SafetyScore),
                Traducir("M1/FinalScore_Vehicle"), Mathf.RoundToInt(final.Vehicle.SafetyScore));
            string verdict = Traducir(final.MustRepeatModule ? "M1/FinalScore_MustRepeat" : "M1/FinalScore_CanContinue");
            // La única frase de la nota de antes que se queda: si se equivocó en «¿circular o no?».
            if (!_readinessCorrect)
                verdict += "\n" + Traducir("M1/Decision_Review");
            return "<size=120%><b>" + Traducir("M1/FinalScore_Title") + "</b></size>\n"
                   + "<size=300%><b>" + final.Percent + " / 100</b></size>\n"
                   + sections + "\n"
                   + "<color=#4A6480>" + verdict + "</color>";
        }

        /// <summary>Texto de I2; si falta el término se ve la clave, que delata la falta.</summary>
        private string Traducir(string key)
        {
            string texto = _localization != null ? _localization.GetTranslation(key) : null;
            return string.IsNullOrEmpty(texto) ? key : texto;
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




