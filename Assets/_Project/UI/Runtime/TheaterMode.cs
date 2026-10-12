using System.Collections;
using UnityEngine;

namespace RideSafe.UI
{
    /// <summary>
    /// Apaga el entorno 3D alrededor del visor mientras se proyecta un video, para que la
    /// pantalla sea lo único que se mire.
    /// <para>
    /// <b>Cómo oscurece, y por qué así.</b> El atenuador es un panel negro en la capa
    /// <c>UI</c>, que dibuja la cámara de overlay del stack de URP. Esa cámara limpia
    /// profundidad, así que todo lo que pinta queda por delante de la geometría sin importar
    /// a qué distancia esté: el panel tapa el garaje entero. Y como está más lejos de la
    /// cámara que los paneles de la interfaz, dentro de esa misma pasada se ordena <b>detrás</b>
    /// de ellos, de modo que el video y sus botones siguen encima y legibles.
    /// </para>
    /// <para>
    /// No se usa <c>ScreenSpaceOverlay</c>, que sería más corto de escribir: un canvas de
    /// overlay se dibuja por encima de absolutamente todo, también del propio video, y además
    /// no funciona en VR, que es a donde va esto.
    /// </para>
    /// <para>
    /// Es un componente aparte y sin nada del módulo 1 justamente porque la reunión lo pidió
    /// reutilizable para las pantallas de retroalimentación de los módulos 2 a 5.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/UI/Theater Mode")]
    [DisallowMultipleComponent]
    public class TheaterMode : MonoBehaviour
    {
        [Tooltip("Grupo del panel negro que oscurece el entorno.")]
        [SerializeField] private CanvasGroup _dim;

        [Tooltip("Cuánto se oscurece. 1 es negro total; conviene dejar algo de sala.")]
        [SerializeField, Range(0f, 1f)] private float _dimAmount = .88f;

        [Tooltip("Segundos del fundido. Entrar y salir de golpe molesta en VR.")]
        [SerializeField, Min(0f)] private float _fadeSeconds = .45f;

        private Coroutine _running;

        /// <summary>¿Está el entorno atenuado ahora mismo?</summary>
        public bool IsOn { get; private set; }

        /// <summary>Opacidad actual del atenuador. Para que los tests puedan afirmar sobre ella.</summary>
        public float CurrentAlpha => _dim == null ? 0f : _dim.alpha;

        private void Awake() => Apply(0f);

        /// <summary>Enciende o apaga el teatro con fundido.</summary>
        public void SetOn(bool on)
        {
            IsOn = on;
            float target = on ? _dimAmount : 0f;

            if (!Application.isPlaying || !isActiveAndEnabled || _fadeSeconds <= 0f)
            {
                Apply(target);
                return;
            }

            if (_running != null)
                StopCoroutine(_running);
            _running = StartCoroutine(FadeTo(target));
        }

        /// <summary>Sin fundido. Para montar la escena o para los tests.</summary>
        public void SetOnImmediate(bool on)
        {
            IsOn = on;
            if (_running != null)
            {
                StopCoroutine(_running);
                _running = null;
            }
            Apply(on ? _dimAmount : 0f);
        }

        private IEnumerator FadeTo(float target)
        {
            float from = _dim == null ? 0f : _dim.alpha;
            float elapsed = 0f;
            while (elapsed < _fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                Apply(Mathf.Lerp(from, target, elapsed / _fadeSeconds));
                yield return null;
            }
            Apply(target);
            _running = null;
        }

        private void Apply(float alpha)
        {
            if (_dim == null)
                return;
            _dim.alpha = alpha;
            // Nunca bloquea el puntero: por debajo no hay nada que pulsar, y bloquear aqui
            // apagaria los botones de pausa y repetir, que estan por encima pero comparten
            // raycaster en algunas configuraciones.
            _dim.blocksRaycasts = false;
            _dim.interactable = false;
            if (_dim.gameObject.activeSelf != alpha > .001f)
                _dim.gameObject.SetActive(alpha > .001f);
        }

        /// <summary>Solo para tests: inyecta el grupo sin pasar por el inspector.</summary>
        public void ConfigureForTests(CanvasGroup dim, float dimAmount = .88f, float fadeSeconds = 0f)
        {
            _dim = dim;
            _dimAmount = dimAmount;
            _fadeSeconds = fadeSeconds;
        }
    }
}
