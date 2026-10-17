using System;
using Cachacos;
using TMPro;
using UnityEngine;

namespace RideSafe.Module01
{
    /// <summary>
    /// Presenta un elemento al frente del aprendiz girando despacio, con su nombre
    /// arriba y la confirmación abajo.
    /// <para>
    /// No decide nada sobre corrección: responder que sí solo registra la elección.
    /// Responder que no devuelve el objeto a su sitio sin dejar rastro, y la zona
    /// sigue abierta.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/Module 01/Item Inspector")]
    public class ItemInspector : MonoBehaviour
    {
        /// <summary>
        /// Lado mayor al que se normaliza todo objeto presentado. El montador de escena lo usa
        /// para colocar el nombre y el popup fuera de la silueta del modelo, así que vive aquí
        /// y no como número suelto en los dos sitios.
        /// </summary>
        public const float DisplaySize = .32f;

        [Tooltip("Dónde se instancia el objeto presentado.")]
        [SerializeField] private Transform _stage;

        [Tooltip("Grados por segundo del giro de presentación.")]
        [SerializeField] private float _rotationSpeed = 25f;

        [Tooltip("Texto del nombre, arriba del objeto.")]
        [SerializeField] private TMP_Text _nameLabel;

        [Tooltip("Raíz del popup de confirmación, abajo del objeto.")]
        [SerializeField] private GameObject _confirmRoot;

        [Tooltip("Texto de la pregunta de confirmación.")]
        [SerializeField] private TMP_Text _questionLabel;

        [SerializeField] private UnityEngine.UI.Button _acceptButton;
        [SerializeField] private UnityEngine.UI.Button _declineButton;

        [Tooltip("Clave de la pregunta. Nunca texto literal.")]
        [SerializeField] private string _questionKey = "M1/Confirm_Question";

        private GameObject _instance;
        [SerializeField] private GameObject _pedestal;

        [Tooltip("Por donde suena la locución del elemento. Opcional.")]
        [SerializeField] private AudioSource _voice;
        private string _activeQuestion;
        public void SetQuestion(string key) => _activeQuestion = key;

        /// <summary>
        /// Pregunta de ESTA presentación, cuando es distinta de la de la zona. Un EPP ya
        /// elegido pregunta si quitarlo.
        /// <para>
        /// Va por presentación y no por <see cref="SetQuestion"/> porque esa es la de la zona
        /// entera y se queda puesta: pisarla dejaría «¿quitarlo?» en el siguiente elemento, que
        /// no está elegido.
        /// </para>
        /// </summary>
        private string _presentQuestion;

        /// <summary>
        /// La clave que se va a pintar, por orden: la de esta presentación, la de la zona, la
        /// del prefab. Expuesta para que un test afirme sin leer la pantalla.
        /// <para>
        /// Con <c>IsNullOrEmpty</c> y no <c>??</c>: la cadena vacía no la atrapa <c>??</c>, y un
        /// <c>SetQuestion("")</c> dejaba la pregunta muda. Ya pasó.
        /// </para>
        /// </summary>
        public string QuestionKey
        {
            get
            {
                if (!string.IsNullOrEmpty(_presentQuestion))
                    return _presentQuestion;
                return string.IsNullOrEmpty(_activeQuestion) ? _questionKey : _activeQuestion;
            }
        }
        private ILocalizationProvider _localization;

        public event Action<SafetyItemSO> Accepted;
        public event Action<SafetyItemSO> Declined;

        /// <summary>
        /// Un elemento acaba de salir al pedestal. Lo escucha quien tenga que esconder su
        /// gemelo de la mesa: lo que gira aquí es una copia, así que sin eso se ven dos.
        /// </summary>
        public event Action<SafetyItemSO> Presenting;

        /// <summary>Se cerró la presentación, por aceptar, por rechazar o por presentar otro.</summary>
        public event Action Dismissed;

        public SafetyItemSO Current { get; private set; }
        public bool IsPresenting => Current != null;

        private void Awake()
        {
            _localization = ServiceLocator.Instance.RequestService<ILocalizationProvider>();
            if (_localization != null) _localization.LanguageChanged += RefreshText;

            if (_acceptButton != null)
                _acceptButton.onClick.AddListener(Accept);
            if (_declineButton != null)
                _declineButton.onClick.AddListener(Decline);

            SetChromeVisible(false);
        }

        private void OnDestroy()
        {
            if (_localization != null) _localization.LanguageChanged -= RefreshText;
            if (_acceptButton != null)
                _acceptButton.onClick.RemoveListener(Accept);
            if (_declineButton != null)
                _declineButton.onClick.RemoveListener(Decline);
        }

        /// <summary>
        /// ¿Lo que hay en el pedestal es un objeto, o el signo de los puntos sin pieza? El signo
        /// <b>no gira</b>: es texto plano y al dar media vuelta se quedaría de canto y luego del
        /// revés. Lo que gira es lo que tiene volumen.
        /// </summary>
        private bool _spins;

        /// <summary>Lo que se ve en el pedestal, para que un test afirme sin mirar la pantalla.</summary>
        public bool PresentsSolid => _spins;

        /// <summary>
        /// Lo que hay montado sobre el pedestal ahora mismo, o null. Expuesto para que un test
        /// afirme lo que se va a dibujar en vez de mirar una captura.
        /// </summary>
        public Transform Presented => _instance == null ? null : _instance.transform;

        private void Update()
        {
            if (_instance != null && _spins)
                _instance.transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime, Space.World);
        }

        public void Present(SafetyItemSO item) => Present(item, null, null);

        public void Present(SafetyItemSO item, Transform source) => Present(item, source, null);

        /// <summary>
        /// Presenta el elemento en el pedestal. <paramref name="source"/> es el objeto de la
        /// escena del que copiar: el EPP de la mesa, o la pieza del vehículo cuando la hay.
        /// <para>
        /// <b>Nunca una esfera blanca.</b> Los cinco puntos del vehículo no tienen modelo propio
        /// —no existe «el freno» como asset—, y antes se les fabricaba una primitiva: al pulsar
        /// «llantas» lo que giraba era una bola. Desde el garaje v8 el manubrio y la rueda
        /// trasera existen de verdad, así que se enseña <b>una copia de la pieza</b>; y donde no
        /// hay pieza se enseña el <c>!</c>, que es el mismo idioma que el marcador del mundo.
        /// </para>
        /// </summary>
        public void Present(SafetyItemSO item, Transform source, string questionKey)
        {
            if (item == null)
                return;
            Dismiss();
            Current = item;
            _presentQuestion = questionKey;
            // Antes de instanciar la copia: así el de la mesa ya está escondido cuando el del
            // pedestal aparece, y no hay un fotograma con los dos a la vista.
            Presenting?.Invoke(item);
            if (_stage != null)
            {
                _instance = new GameObject("PresentedItem");
                _instance.transform.SetParent(_stage, false);
                GameObject model = BuildDisplay(item, source);
                model.transform.SetParent(_instance.transform, false);
                _spins = model.GetComponentsInChildren<Renderer>().Length > 0;
                foreach (Collider collider in model.GetComponentsInChildren<Collider>()) collider.enabled = false;
                var renderers = model.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    float longest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                    if (longest > 0) model.transform.localScale *= DisplaySize / longest;
                    bounds = renderers[0].bounds;
                    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    model.transform.position += _stage.position - bounds.center;
                }
            }
            RefreshText();
            SetChromeVisible(true);
            Speak(item);
        }

        /// <summary>
        /// Qué se pone sobre el pedestal, por orden: una copia del objeto que está en la
        /// escena, su modelo del catálogo, o el signo de exclamación. Lo que no se pone nunca
        /// es una esfera.
        /// <para>
        /// <b>Primero la copia del objeto de la escena, y no el prefab del catálogo.</b> Medido
        /// el 2026-10-16: instanciar el FBX da una pose distinta a la que el arte dejó en la
        /// mesa en <b>12 de los 13</b> EPP —el casco bueno y las gafas a 270° en Y, el jean
        /// ajustado a 270° en X y con escala 0.1, el teléfono a (270, 270, 0)—, así que un
        /// pantalón doblado que en la mesa se ve plano salía de canto en el pedestal. Y en los
        /// pares que entrega el arte (coderas, rodilleras) el FBX es <i>una</i> pieza y la mesa
        /// tiene <i>dos</i>, así que el pedestal enseñaba media codera. El cliente lo vio como
        /// «el del pedestal no es el de la mesa», y tenía razón.
        /// </para>
        /// <para>
        /// El prefab del catálogo se queda como respaldo: sirve al greybox y a cualquier
        /// elemento que algún día no tenga objeto en la escena.
        /// </para>
        /// </summary>
        private GameObject BuildDisplay(SafetyItemSO item, Transform source)
        {
            GameObject copy = CopyOf(source);
            if (copy != null)
                return copy;

            if (item.DisplayPrefab != null)
                return Instantiate(item.DisplayPrefab, _instance.transform);

            return BuildGlyph();
        }

        /// <summary>
        /// Una COPIA de las mallas visibles de <paramref name="source"/>, conservando la pose
        /// que tienen en el mundo. Devuelve null si no hay nada que copiar.
        /// <para>
        /// Copia y no el original: mover la pieza la haría desaparecer de la bici —o el EPP de
        /// la mesa— justo mientras el aprendiz la inspecciona.
        /// </para>
        /// <para>
        /// Solo lo <b>visible</b>: las esferas de clic de los cinco puntos del vehículo llevan
        /// el renderer apagado, y de ahí no se puede copiar nada. Esos caen al signo, que es lo
        /// correcto y lo que ya hacían.
        /// </para>
        /// </summary>
        private GameObject CopyOf(Transform source)
        {
            if (source == null)
                return null;

            GameObject root = null;
            foreach (MeshRenderer renderer in source.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer == null || !renderer.enabled)
                    continue;
                // El verde de «ya seleccionado» y el contorno del vehículo son avisos del
                // mundo, no el objeto: copiarlos pondría el aviso girando en el pedestal.
                if (renderer.gameObject.name == SelectedHolo.MarkName ||
                    renderer.gameObject.name == InspectionHighlight.OutlineName)
                    continue;

                // Fundido por el static batching, su MeshFilter apunta a la malla combinada de
                // toda la escena, con los vertices ya en el mundo: la copia sale vacia o en otra
                // parte. Paso en Quest 2026-10-16 con las botas, marcadas Static. Va el prefab.
                if (renderer.isPartOfStaticBatch)
                {
                    if (root != null)
                    {
                        if (Application.isPlaying)
                            Destroy(root);
                        else
                            DestroyImmediate(root);
                    }
                    return null;
                }

                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                    continue;

                if (root == null)
                {
                    root = new GameObject(source.name);
                    root.transform.SetParent(_instance.transform, false);
                }

                GameObject piece = new GameObject(
                    renderer.gameObject.name, typeof(MeshFilter), typeof(MeshRenderer));
                piece.transform.SetParent(root.transform, false);
                // La pose del MUNDO, que es la que el arte le dio. Present recoloca y escala el
                // conjunto despues, asi que da igual donde caiga ahora.
                piece.transform.SetPositionAndRotation(
                    renderer.transform.position, renderer.transform.rotation);
                piece.transform.localScale = renderer.transform.lossyScale;
                piece.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                piece.GetComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
            }

            return root;
        }

        /// <summary>
        /// El mismo <c>!</c> que marca el punto en el mundo, para los que no tienen ni modelo ni
        /// pieza: el freno vive dentro del manubrio, la batería dentro del marco y el faro no
        /// existe como objeto.
        /// </summary>
        private GameObject BuildGlyph()
        {
            GameObject go = new GameObject("Glyph", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(_instance.transform, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(70f, 112f);
            go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            // El escalado generico de Present mide Renderers, y un canvas no tiene: si no se
            // escala aqui, el signo mide 112 METROS. Ya paso con el marcador del mundo.
            go.transform.localScale = Vector3.one * (DisplaySize / 112f);

            GameObject label = new GameObject("Text", typeof(RectTransform));
            label.transform.SetParent(go.transform, false);
            RectTransform rect = (RectTransform)label.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            TMPro.TextMeshProUGUI text = label.AddComponent<TMPro.TextMeshProUGUI>();
            // The brand font of the popup's header, not TMP's default (AutoHand's AmmoText here).
            if (_nameLabel != null) text.font = _nameLabel.font;
            text.text = "!";
            text.fontSize = 82f; // Manrope's '!' at 86 asks 117.5 of height in a 112 box
            text.fontStyle = TMPro.FontStyles.Bold;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.color = new Color(1f, .42f, .05f, 1f);
            text.outlineColor = new Color32(10, 20, 35, 255);
            text.outlineWidth = .22f;
            return go;
        }

        /// <summary>Solo para tests y para el montador de escena.</summary>
        public void ConfigureForTests(Transform stage) => _stage = stage;

        /// <summary>
        /// Suelta la locución del elemento, si existe.
        /// <para>
        /// Hoy no hay ningún clip en el proyecto, así que esto no suena nunca y <b>no avisa de
        /// nada</b>: un aviso por cada objeto en cada recorrido sería ruido, no información.
        /// La falta de locuciones está donde tiene que estar, en el documento de pendientes.
        /// </para>
        /// </summary>
        [Tooltip("Carpeta de Resources donde viven las locuciones, por idioma.")]
        [SerializeField] private string _voiceFolder = "Sounds/M1";

        /// <summary>
        /// Lo que se oye al sacar el elemento al pedestal: <b>su nombre</b>, en el idioma
        /// elegido.
        /// <para>
        /// El nombre y no el «por qué». Decir «es la única protección de la cabeza» mientras el
        /// aprendiz decide si se lo lleva es darle la respuesta: sesga la elección y el
        /// ejercicio deja de medir nada. El motivo de cada cosa se explica <b>al final</b>, con
        /// el video. Lo pidió Charlie el 2026-10-16, después de oírlo al revés.
        /// </para>
        /// <para>
        /// Se resuelve por idioma y no desde <see cref="SafetyItemSO.PromptClip"/> porque ese
        /// campo es <b>un solo clip</b>: con él, quien juegue en inglés oiría español. La
        /// locución vive en <c>Resources/Sounds/M1/&lt;idioma&gt;/&lt;clave&gt;</c>, que es justo donde
        /// la escribe la herramienta que las genera, así que no hay que arrastrar ni un asset.
        /// </para>
        /// <para>
        /// <see cref="SafetyItemSO.PromptClip"/> sigue mandando si alguien cablea uno a mano:
        /// sirve para una locución suelta que no venga de la herramienta.
        /// </para>
        /// <para>
        /// Sin clip no pasa nada y no se avisa: que falte una locución es el estado normal
        /// mientras se graban.
        /// </para>
        /// </summary>
        private void Speak(SafetyItemSO item)
        {
            if (_voice == null)
                return;

            AudioClip clip = item.PromptClip;
            if (clip == null && _localization != null)
                clip = Cachacos.LocalizedResource.Load<AudioClip>(
                    _voiceFolder, _localization.CurrentLanguage, item.NameKey);
            if (clip == null)
                return;

            _voice.Stop();
            _voice.clip = clip;
            _voice.Play();
        }

        private void RefreshText()
        {
            if (Current == null) return;
            if (_nameLabel != null) _nameLabel.text = Translate(Current.NameKey);
            // ?? no atrapa la cadena vacia, y un SetQuestion("") dejaba la pregunta en blanco.
            if (_questionLabel != null)
                _questionLabel.text = Translate(QuestionKey);
        }

        public void Accept()
        {
            if (!IsPresenting)
                return;
            SafetyItemSO item = Current;
            Dismiss();
            Accepted?.Invoke(item);
        }

        public void Decline()
        {
            if (!IsPresenting)
                return;
            SafetyItemSO item = Current;
            Dismiss();
            Declined?.Invoke(item);
        }

        private void Dismiss()
        {
            bool was = Current != null;
            Current = null;
            _presentQuestion = null;
            if (was)
                Dismissed?.Invoke();
            if (_instance != null)
            {
                // DestroyImmediate en editor fuera de Play; Destroy en runtime.
                if (Application.isPlaying)
                    Destroy(_instance);
                else
                    DestroyImmediate(_instance);
                _instance = null;
            }
            SetChromeVisible(false);
        }

        /// <summary>
        /// Mismo contrato que <see cref="UI.UIText.Resolve"/>: si el término no existe o está
        /// vacío, se cae a la clave.
        /// <para>
        /// Sin esta caída, un término borrado del I2 no se ve como "falta traducir" sino como
        /// un popup SIN pregunta, que es mucho más difícil de notar y de atribuir.
        /// </para>
        /// </summary>
        private string Translate(string key)
        {
            if (string.IsNullOrEmpty(key))
                return key;
            string text = _localization != null ? _localization.GetTranslation(key) : null;
            return string.IsNullOrEmpty(text) ? key : text;
        }

        private void SetChromeVisible(bool visible)
        {
            if (_pedestal != null) _pedestal.SetActive(visible);
            if (_confirmRoot != null)
                _confirmRoot.SetActive(visible);
            if (_nameLabel != null)
                _nameLabel.gameObject.SetActive(visible);
        }
    }
}
