using UnityEngine;

namespace RideSafe.Tutorial.Presentation
{
    /// <summary>
    /// En la bienvenida, mientras la voz explica cómo se elige, el mando derecho aparece junto al
    /// panel con el gatillo iluminado y pulsándose (cliente, 2026-11-11, 12:23: «mientras ocurre la
    /// animación del gatillo»). Lo enciende y lo apaga la bienvenida por eventos de la escena.
    /// </summary>
    [AddComponentMenu("RideSafe/Tutorial/Welcome Trigger Hint")]
    public class WelcomeTriggerHint : MonoBehaviour
    {
        [SerializeField] private ControllerHint3DView _view;

        [Tooltip("Lo que se está leyendo: el panel de bienvenida. El mando se pone a su lado.")]
        [SerializeField] private Transform _focus;

        public void Show()
        {
            if (_view == null || !_view.Show(new PhysicalBinding(XRNodeRole.RightHand, ControllerControl.Trigger)))
            {
                Debug.LogWarning("[RideSafe.Tutorial] El mando de la bienvenida no se pudo enseñar.", this);
                return;
            }
            Follow();
        }

        public void Hide()
        {
            if (_view != null)
                _view.Hide();
        }

        // La vista es world-locked: la primera llamada la coloca y las siguientes solo la mueven si
        // la cabeza gira o se aleja, así que se le pasa la cabeza cada fotograma.
        private void LateUpdate()
        {
            if (_view != null && _view.IsVisible)
                Follow();
        }

        private void Follow()
        {
            Camera head = Camera.main;
            if (head == null)
                return;
            Transform h = head.transform;
            // Junto a lo que se lee si está a la vista; si no (en el garaje), delante de la cabeza.
            bool focused = _focus != null && _focus.gameObject.activeInHierarchy;
            _view.Place(new Pose(h.position, h.rotation), focused ? _focus.position : h.position + h.forward);
        }

        /// <summary>Usado por el montador de escena.</summary>
        public void Configure(ControllerHint3DView view, Transform focus)
        {
            _view = view;
            _focus = focus;
        }
    }
}
