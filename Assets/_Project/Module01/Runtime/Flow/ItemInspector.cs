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
        private string _activeQuestion;
        public void SetQuestion(string key) => _activeQuestion = key;
        private ILocalizationProvider _localization;

        public event Action<SafetyItemSO> Accepted;
        public event Action<SafetyItemSO> Declined;

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

        private void Update()
        {
            if (_instance != null)
                _instance.transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime, Space.World);
        }

        public void Present(SafetyItemSO item)
        {
            if (item == null)
                return;
            Dismiss();
            Current = item;
            if (_stage != null)
            {
                _instance = new GameObject("PresentedItem");
                _instance.transform.SetParent(_stage, false);
                GameObject model = item.DisplayPrefab != null ? Instantiate(item.DisplayPrefab, _instance.transform) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
                model.transform.SetParent(_instance.transform, false);
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
            Current = null;
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
