using UnityEngine;
using UnityEngine.Rendering;

namespace RideSafe.Module01
{
    /// <summary>
    /// Delinea la pieza real del vehículo que toca revisar: el manubrio para la dirección,
    /// la rueda para las llantas. Es lo que pidió la reunión y hasta el garaje v8 no se podía
    /// hacer, porque las mallas que entregaba el equipo de arte eran una pieza única sin hijos.
    /// <para>
    /// <b>El contorno es un hijo con la misma malla, nunca un material añadido al
    /// <c>MeshRenderer</c> del arte.</b> Dos razones. Una: añadir un material a la lista de un
    /// renderer solo repite la <i>primera</i> submalla, así que una pieza con dos materiales
    /// —y la bici tiene dos— se delinearía a medias. Y dos: al cambiar de vehículo no hay nada
    /// que restaurar, se borra el hijo y la pieza queda como estaba.
    /// </para>
    /// <para>
    /// El color va por <see cref="MaterialPropertyBlock"/> y no por instancia de material: así
    /// los cinco puntos comparten un solo material y encenderlos no multiplica draw calls.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/Module 01/Inspection Highlight")]
    [DisallowMultipleComponent]
    public class InspectionHighlight : MonoBehaviour, IInspectionCue
    {
        /// <summary>Nombre del hijo que lleva el contorno. Se busca por él para no duplicarlo.</summary>
        public const string OutlineName = "Outline";

        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");

        [Tooltip("Material de casco invertido. M_InspectionOutline.")]
        [SerializeField] private Material _outlineMaterial;

        [Tooltip("Naranja neón del punto que toca revisar ahora. El mismo que el signo.")]
        [SerializeField] private Color _activeColor = new Color(1f, .42f, .05f, 1f);

        [Tooltip("Color de un punto ya resuelto. El mismo que el signo.")]
        [SerializeField] private Color _resolvedColor = new Color(.46f, .83f, .54f, .9f);

        private MeshRenderer _outline;
        private bool _highlighted;
        private bool _resolved;
        private bool _obscured;

        /// <summary>El contorno colgado de la pieza, o null si este punto no tiene pieza.</summary>
        public Renderer Outline => _outline;

        /// <summary>
        /// La pieza del vehículo que delinea, o null si este punto no tiene ninguna. La usa el
        /// inspector para enseñar la pieza de verdad en el pedestal en vez de una esfera.
        /// </summary>
        public Transform Part => _outline == null ? null : _outline.transform.parent;

        /// <summary>
        /// Color que lleva puesto el contorno. Se lee del bloque de propiedades del renderer,
        /// no de un campo guardado, para que un test afirme lo que se va a dibujar de verdad.
        /// </summary>
        public Color CurrentColor
        {
            get
            {
                if (_outline == null)
                    return Color.clear;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                _outline.GetPropertyBlock(block);
                return block.GetColor(OutlineColorId);
            }
        }

        /// <summary>
        /// Cuelga el contorno de <paramref name="part"/>. Devuelve si pudo: un punto sin pieza
        /// real —el freno, la batería y el faro no existen como objeto, y el patinete entero es
        /// una sola malla— devuelve <c>false</c> y se queda con el signo de exclamación.
        /// </summary>
        public bool Bind(Transform part)
        {
            Release();

            if (part == null)
                return false;

            MeshFilter filter = part.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                Debug.LogWarning(
                    $"[Module01] La pieza '{part.name}' no tiene malla, asi que no se puede " +
                    $"delinear; '{name}' se queda con el signo de exclamacion.", this);
                return false;
            }

            // Un contorno de una sesion anterior que alguien guardara en la escena.
            Transform stale = part.Find(OutlineName);
            if (stale != null)
                Discard(stale.gameObject);

            GameObject go = new GameObject(OutlineName, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(part, false);
            go.layer = part.gameObject.layer;
            go.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;

            _outline = go.GetComponent<MeshRenderer>();
            _outline.sharedMaterial = _outlineMaterial;
            // Un contorno no es geometria: ni proyecta sombra, ni la recibe, ni se ilumina.
            // Dejarlo participar lo mete ademas en el horneado, que aqui viene dado del v8.
            _outline.shadowCastingMode = ShadowCastingMode.Off;
            _outline.receiveShadows = false;
            _outline.lightProbeUsage = LightProbeUsage.Off;
            _outline.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _outline.staticShadowCaster = false;

            Repaint();
            return true;
        }

        /// <summary>
        /// Suelta la pieza y borra el contorno. Hay que llamarlo al cambiar de vehículo: el
        /// contorno cuelga de la malla <b>anterior</b>, y dejarlo ahí significa que la bici
        /// apagada se queda con el manubrio delineado.
        /// </summary>
        public void Release()
        {
            if (_outline != null)
                Discard(_outline.gameObject);
            _outline = null;
        }

        public void SetHighlighted(bool value)
        {
            if (_highlighted == value)
                return;
            _highlighted = value;
            Repaint();
        }

        public void SetResolved(bool value)
        {
            if (_resolved == value)
                return;
            _resolved = value;
            Repaint();
        }

        public void SetObscured(bool value)
        {
            if (_obscured == value)
                return;
            _obscured = value;
            Repaint();
        }

        /// <summary>
        /// Solo se delinea el punto de la zona en curso y los ya resueltos. Delinear las cinco
        /// piezas a la vez es no señalar ninguna.
        /// </summary>
        private void Repaint()
        {
            if (_outline == null)
                return;

            _outline.enabled = !_obscured && (_highlighted || _resolved);

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            _outline.GetPropertyBlock(block);
            block.SetColor(OutlineColorId, _resolved ? _resolvedColor : _activeColor);
            _outline.SetPropertyBlock(block);
        }

        private void OnDestroy() => Release();

        /// <summary>
        /// En modo edición <c>Destroy</c> no se aplica hasta el siguiente ciclo, así que el
        /// contorno viejo seguiría colgando y <see cref="Bind"/> dejaría dos.
        /// </summary>
        private static void Discard(Object target)
        {
            if (target == null)
                return;
            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }

        /// <summary>Solo para tests y para el montador de escena.</summary>
        public void ConfigureForTests(Material outlineMaterial, Color active, Color resolved)
        {
            _outlineMaterial = outlineMaterial;
            _activeColor = active;
            _resolvedColor = resolved;
            Repaint();
        }
    }
}
