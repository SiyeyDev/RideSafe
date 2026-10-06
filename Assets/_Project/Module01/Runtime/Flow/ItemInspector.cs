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
        [Tooltip("Dónde se instancia el objeto presentado.")]
        [SerializeField] private Transform _stage;

        [Tooltip("Grados por segundo del giro de presentación.")]
        [SerializeField] private float _rotationSpeed = 25f;

        [Tooltip("Texto del nombre, arriba del objeto.")]
        [SerializeField] private TMP_Text _nameLabel;

        [Tooltip("Raíz del popup de confirmación, abajo del objeto.")]
        [SerializeField] private GameObject _confirmRoot;

        private GameObject _instance;
        private ILocalizationProvider _localization;

        public event Action<SafetyItemSO> Accepted;
        public event Action<SafetyItemSO> Declined;

        public SafetyItemSO Current { get; private set; }
        public bool IsPresenting => Current != null;

        private void Awake()
        {
            _localization = ServiceLocator.Instance.RequestService<ILocalizationProvider>();
            SetChromeVisible(false);
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

            Current = item;

            if (_stage != null && item.DisplayPrefab != null)
                _instance = Instantiate(item.DisplayPrefab, _stage.position, _stage.rotation, _stage);

            if (_nameLabel != null)
                _nameLabel.text = _localization != null ? _localization.GetTranslation(item.NameKey) : item.NameKey;

            SetChromeVisible(true);
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

        private void SetChromeVisible(bool visible)
        {
            if (_confirmRoot != null)
                _confirmRoot.SetActive(visible);
            if (_nameLabel != null)
                _nameLabel.gameObject.SetActive(visible);
        }
    }
}
