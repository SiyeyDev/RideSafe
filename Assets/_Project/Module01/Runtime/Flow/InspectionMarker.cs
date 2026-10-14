using TMPro;
using UnityEngine;

namespace RideSafe.Module01
{
    /// <summary>
    /// El signo de exclamación flotante sobre un punto de inspección del vehículo, al estilo
    /// de los marcadores de misión de un RPG, como pidió la reunión.
    /// <para>
    /// Hace tres cosas: mirar siempre al aprendiz, latir despacio para que el ojo lo encuentre,
    /// y cambiar de estado cuando el punto de su zona está en revisión o ya se ha resuelto.
    /// </para>
    /// <para>
    /// <b>Es el aviso de respaldo, no el principal.</b> Donde el vehículo viene despiezado y la
    /// pieza existe de verdad, quien señala es <see cref="InspectionHighlight"/> con un contorno
    /// sobre la malla, y este signo se silencia: los dos a la vez marcan lo mismo dos veces. El
    /// signo sigue siendo lo único que hay para los puntos sin pieza propia —el freno vive dentro
    /// del <c>manubrio</c>, la batería dentro del <c>marco</c> y el faro no existe— y para el
    /// patinete entero, que sigue siendo una malla única sin hijos.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/Module 01/Inspection Marker")]
    [DisallowMultipleComponent]
    public class InspectionMarker : MonoBehaviour, IInspectionCue
    {
        [Tooltip("La '!' que flota sobre el punto.")]
        [SerializeField] private TMP_Text _glyph;

        [Tooltip("Lo que gira para mirar al aprendiz.")]
        [SerializeField] private Transform _billboard;

        [Tooltip("Naranja neón del punto que toca revisar ahora.")]
        [SerializeField] private Color _activeColor = new Color(1f, .42f, .05f, 1f);

        [Tooltip("Color de un punto de otra zona: presente pero sin reclamar atención.")]
        [SerializeField] private Color _idleColor = new Color(1f, 1f, 1f, .35f);

        [Tooltip("Color de un punto ya resuelto.")]
        [SerializeField] private Color _resolvedColor = new Color(.46f, .83f, .54f, .9f);

        [Tooltip("Cuánto crece y encoge el latido, en tanto por uno.")]
        [SerializeField, Range(0f, .5f)] private float _pulse = .14f;

        [Tooltip("Latidos por segundo.")]
        [SerializeField, Min(0f)] private float _pulseSpeed = 1.6f;

        private Transform _head;
        private Vector3 _baseScale;
        private bool _highlighted;
        private bool _resolved;
        private bool _obscured;
        private bool _suppressed;

        /// <summary>
        /// Lo silencia porque su punto ya se señala delineando la pieza real. Lo decide
        /// <see cref="VehicleStage"/> al encender el vehículo, y cambia con el vehículo: el
        /// mismo punto tiene pieza en la e-bike y no la tiene en el patinete.
        /// <para>
        /// Es una bandera aparte de <see cref="SetObscured"/> a propósito. Tapar es transitorio
        /// —dura lo que dura el popup— y el recorrido lo quita solo al cerrarlo; silenciar dura
        /// lo que dure el vehículo. Con una sola bandera, cerrar el popup devolvería el signo
        /// encima del contorno.
        /// </para>
        /// </summary>
        public void SetSuppressed(bool value)
        {
            if (_suppressed == value)
                return;
            _suppressed = value;
            Repaint();
        }

        /// <summary>¿Se está viendo el signo? Para que un test afirme sin mirar la pantalla.</summary>
        public bool IsVisible => _billboard != null && _billboard.gameObject.activeSelf;

        /// <summary>
        /// Lo tapa algo y no debe verse, pase lo que pase con su estado. Lo usa el recorrido
        /// mientras hay un objeto en el pedestal: el popup de confirmación ocupa el centro de la
        /// pantalla y un marcador por detrás se le cruza por encima —está en la capa de overlay,
        /// así que no lo tapa la geometría—.
        /// </summary>
        public void SetObscured(bool value)
        {
            if (_obscured == value)
                return;
            _obscured = value;
            Repaint();
        }

        /// <summary>¿Es el punto que toca revisar ahora?</summary>
        public bool IsHighlighted => _highlighted;

        /// <summary>¿Ya lo resolvió el aprendiz?</summary>
        public bool IsResolved => _resolved;

        /// <summary>Color actual del signo. Para que un test pueda afirmar sin mirar la pantalla.</summary>
        public Color CurrentColor => _glyph == null ? Color.clear : _glyph.color;

        private void Awake()
        {
            _baseScale = _billboard != null ? _billboard.localScale : Vector3.one;
            Repaint();
        }

        private void LateUpdate()
        {
            if (_billboard == null)
                return;

            if (_head == null)
            {
                Camera camera = Camera.main;
                if (camera != null)
                    _head = camera.transform;
            }

            if (_head != null)
            {
                // Los textos se leen desde delante, asi que el forward apunta AL CONTRARIO
                // del aprendiz, igual que en los canvas de world space.
                Vector3 away = _billboard.position - _head.position;
                if (away.sqrMagnitude > .0001f)
                    _billboard.rotation = Quaternion.LookRotation(away, Vector3.up);
            }

            // El latido solo lo da el punto activo: si latieran todos, ninguno destacaria.
            float scale = _highlighted && !_resolved
                ? 1f + Mathf.Sin(Time.unscaledTime * _pulseSpeed * Mathf.PI * 2f) * _pulse
                : 1f;
            _billboard.localScale = _baseScale * scale;
        }

        /// <summary>Lo enciende como punto en revisión, o lo apaga a presencia discreta.</summary>
        public void SetHighlighted(bool value)
        {
            if (_highlighted == value)
                return;
            _highlighted = value;
            Repaint();
        }

        /// <summary>Marca el punto como ya revisado. No se apaga: se queda visible en verde.</summary>
        public void SetResolved(bool value)
        {
            if (_resolved == value)
                return;
            _resolved = value;
            Repaint();
        }

        /// <summary>
        /// Solo se ven los puntos de la zona en curso y los ya resueltos. Los de otras zonas se
        /// apagan del todo.
        /// <para>
        /// No es solo orden: es que <b>se montaban unos sobre otros</b>. Los cinco cuelgan a
        /// distintas profundidades a lo largo del vehículo, y cuando este se gira de frente para
        /// enseñar el manubrio, el de las llantas y el del sistema eléctrico se proyectan casi
        /// en el mismo punto de la pantalla: dos signos de exclamación pisados. Medido en
        /// PlayMode, no supuesto.
        /// </para>
        /// </summary>
        private void Repaint()
        {
            // Que se vea o no es independiente de que haya glifo: un marcador sin texto sigue
            // teniendo que apagarse cuando lo silencian.
            if (_glyph != null)
                _glyph.color = _resolved ? _resolvedColor : _activeColor;
            if (_billboard != null)
                _billboard.gameObject.SetActive(
                    !_obscured && !_suppressed && (_highlighted || _resolved));
        }

        /// <summary>Solo para tests y para el montador de escena.</summary>
        public void ConfigureForTests(TMP_Text glyph, Transform billboard)
        {
            _glyph = glyph;
            _billboard = billboard;
            _baseScale = billboard != null ? billboard.localScale : Vector3.one;
            Repaint();
        }
    }
}
