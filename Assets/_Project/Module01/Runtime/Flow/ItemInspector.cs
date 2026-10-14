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
        [SerializeField] private string _questionKey = "Module1/Confirm_Question";

        private GameObject _instance;
        [SerializeField] private GameObject _pedestal;

        [Tooltip("Por donde suena la locución del elemento. Opcional.")]
        [SerializeField] private AudioSource _voice;
        private string _activeQuestion;
        public void SetQuestion(string key) => _activeQuestion = key;
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

        private void Update()
        {
            if (_instance != null && _spins)
                _instance.transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime, Space.World);
        }

        public void Present(SafetyItemSO item) => Present(item, null);

        /// <summary>
        /// Presenta el elemento en el pedestal. <paramref name="realPart"/> es la pieza del
        /// vehículo que le corresponde, cuando la hay.
        /// <para>
        /// <b>Nunca una esfera blanca.</b> Los cinco puntos del vehículo no tienen modelo propio
        /// —no existe «el freno» como asset—, y antes se les fabricaba una primitiva: al pulsar
        /// «llantas» lo que giraba era una bola. Desde el garaje v8 el manubrio y la rueda
        /// trasera existen de verdad, así que se enseña <b>una copia de la pieza</b>; y donde no
        /// hay pieza se enseña el <c>!</c>, que es el mismo idioma que el marcador del mundo.
        /// </para>
        /// </summary>
        public void Present(SafetyItemSO item, Transform realPart)
        {
            if (item == null)
                return;
            Dismiss();
            Current = item;
            // Antes de instanciar la copia: así el de la mesa ya está escondido cuando el del
            // pedestal aparece, y no hay un fotograma con los dos a la vista.
            Presenting?.Invoke(item);
            if (_stage != null)
            {
                _instance = new GameObject("PresentedItem");
                _instance.transform.SetParent(_stage, false);
                GameObject model = BuildDisplay(item, realPart);
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
        /// Qué se pone sobre el pedestal, por orden: su propio modelo, la pieza real del
        /// vehículo, o el signo de exclamación. Lo que no se pone nunca es una esfera.
        /// </summary>
        private GameObject BuildDisplay(SafetyItemSO item, Transform realPart)
        {
            if (item.DisplayPrefab != null)
                return Instantiate(item.DisplayPrefab, _instance.transform);

            if (realPart != null)
            {
                MeshFilter source = realPart.GetComponent<MeshFilter>();
                MeshRenderer paint = realPart.GetComponent<MeshRenderer>();
                if (source != null && source.sharedMesh != null)
                {
                    // Una COPIA. Mover la pieza original la haría desaparecer de la bici justo
                    // mientras el aprendiz la inspecciona.
                    GameObject copy = new GameObject(realPart.name, typeof(MeshFilter), typeof(MeshRenderer));
                    copy.transform.SetParent(_instance.transform, false);
                    copy.GetComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                    if (paint != null)
                        copy.GetComponent<MeshRenderer>().sharedMaterials = paint.sharedMaterials;
                    return copy;
                }
            }

            return BuildGlyph();
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
            text.text = "!";
            text.fontSize = 86f;
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
        private void Speak(SafetyItemSO item)
        {
            if (_voice == null || item.PromptClip == null)
                return;
            _voice.Stop();
            _voice.clip = item.PromptClip;
            _voice.Play();
        }

        private void RefreshText()
        {
            if (Current == null) return;
            if (_nameLabel != null) _nameLabel.text = Translate(Current.NameKey);
            // ?? no atrapa la cadena vacia, y un SetQuestion("") dejaba la pregunta en blanco.
            if (_questionLabel != null)
                _questionLabel.text = Translate(string.IsNullOrEmpty(_activeQuestion) ? _questionKey : _activeQuestion);
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
