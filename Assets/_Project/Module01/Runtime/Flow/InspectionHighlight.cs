using System.Collections.Generic;
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
    /// <para>
    /// <b>Lo que brilla se puede tocar.</b> Además del contorno, el punto gana un área de clic
    /// del tamaño de la pieza, que solo responde cuando ese punto es el <i>único</i> por
    /// revisar. Prueba en Quest 2026-10-16: en «Ruedas y estructura» brillaba la rueda entera
    /// y lo único que respondía era la esfera invisible de 15 cm en el borde de la llanta. Con
    /// varios puntos a la vez —la cabina— cada uno se queda con su esfera: el manubrio es todo
    /// el tren delantero y su área enterraría los frenos (ver <c>VehicleStage.MoveToPart</c>).
    /// </para>
    /// <para>
    /// <b>Un punto puede delinear varias piezas</b> (e-bike de 2026-10-21: dos frenos, dos ruedas,
    /// dos luces). Cada una lleva su contorno y su área de clic —una caja que abarcara las dos
    /// ruedas taparía la bici entera—, y la primera es la que manda: la esfera va a ella.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/Module 01/Inspection Highlight")]
    [DisallowMultipleComponent]
    public class InspectionHighlight : MonoBehaviour, IInspectionCue
    {
        /// <summary>Nombre del hijo que lleva el contorno. Se busca por él para no duplicarlo.</summary>
        public const string OutlineName = "Outline";

        /// <summary>Nombre del hijo del punto con el área de clic del tamaño de la pieza.</summary>
        public const string PartAreaName = "PartClickArea";

        // Los puntos con pieza, para saber si este es el único por revisar.
        private static readonly List<InspectionHighlight> Bound = new List<InspectionHighlight>();

        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");

        [Tooltip("Material de casco invertido. M_InspectionOutline.")]
        [SerializeField] private Material _outlineMaterial;

        [Tooltip("Naranja neón del punto que toca revisar ahora. El mismo que el signo.")]
        [SerializeField] private Color _activeColor = new Color(1f, .42f, .05f, 1f);

        [Tooltip("Color de un punto ya resuelto. El mismo que el signo.")]
        [SerializeField] private Color _resolvedColor = new Color(.46f, .83f, .54f, .9f);

        // Uno por pieza, en el mismo orden que _parts.
        private readonly List<Transform> _parts = new List<Transform>();
        private readonly List<MeshRenderer> _outlines = new List<MeshRenderer>();
        private readonly List<BoxCollider> _areas = new List<BoxCollider>();
        private bool _highlighted;
        private bool _resolved;
        private bool _obscured;

        /// <summary>El contorno de la primera pieza, o null si este punto no tiene pieza.</summary>
        public Renderer Outline => _outlines.Count > 0 ? _outlines[0] : null;

        /// <summary>
        /// La primera pieza del vehículo que delinea —la que lleva la esfera—, o null si este
        /// punto no tiene ninguna.
        /// </summary>
        public Transform Part => _parts.Count > 0 ? _parts[0] : null;

        /// <summary>
        /// Todas las piezas que delinea. Las usa el inspector para enseñar en el pedestal
        /// exactamente lo que brilla en el vehículo.
        /// </summary>
        public IReadOnlyList<Transform> Parts => _parts;

        /// <summary>
        /// Color que lleva puesto el contorno. Se lee del bloque de propiedades del renderer,
        /// no de un campo guardado, para que un test afirme lo que se va a dibujar de verdad.
        /// </summary>
        public Color CurrentColor
        {
            get
            {
                Renderer outline = Outline;
                if (outline == null)
                    return Color.clear;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                outline.GetPropertyBlock(block);
                return block.GetColor(OutlineColorId);
            }
        }

        /// <summary>
        /// Cuelga el contorno de <paramref name="part"/>. Devuelve si pudo: un punto sin pieza
        /// real —la batería vive dentro del chasis en los dos vehículos y el faro no existe en
        /// ninguno— devuelve <c>false</c> y se queda con el signo de exclamación.
        /// </summary>
        public bool Bind(Transform part) => BindParts(part == null ? new Transform[0] : new[] { part });

        /// <summary>
        /// Cuelga un contorno y un área de clic de cada una de <paramref name="parts"/>. Las que
        /// no tengan malla se saltan avisando, sin dejar sin contorno a las demás. Devuelve si
        /// delineó alguna.
        /// </summary>
        public bool BindParts(IEnumerable<Transform> parts)
        {
            Release();

            if (parts != null)
                foreach (Transform part in parts)
                    BindOne(part);

            if (_parts.Count == 0)
                return false;
            Bound.Add(this);
            Repaint();
            return true;
        }

        private void BindOne(Transform part)
        {
            if (part == null)
                return;

            MeshFilter filter = part.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                Debug.LogWarning(
                    $"[Module01] La pieza '{part.name}' no tiene malla, asi que no se puede " +
                    $"delinear; '{name}' no la delinea.", this);
                return;
            }

            // Un contorno de una sesion anterior que alguien guardara en la escena.
            Transform stale = part.Find(OutlineName);
            if (stale != null)
                Discard(stale.gameObject);

            GameObject go = new GameObject(OutlineName, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(part, false);
            go.layer = part.gameObject.layer;
            go.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;

            MeshRenderer outline = go.GetComponent<MeshRenderer>();
            // Un material por submalla: con uno solo Unity dibuja la primera y nada mas, y el
            // Manubrio del scooter o la pantalla de la e-bike se quedaban medio delineados.
            Material[] materials = new Material[Mathf.Max(1, filter.sharedMesh.subMeshCount)];
            for (int i = 0; i < materials.Length; i++)
                materials[i] = _outlineMaterial;
            outline.sharedMaterials = materials;
            // Un contorno no es geometria: ni proyecta sombra, ni la recibe, ni se ilumina.
            // Dejarlo participar lo mete ademas en el horneado, que aqui viene dado del v8.
            outline.shadowCastingMode = ShadowCastingMode.Off;
            outline.receiveShadows = false;
            outline.lightProbeUsage = LightProbeUsage.Off;
            outline.reflectionProbeUsage = ReflectionProbeUsage.Off;
            outline.staticShadowCaster = false;

            _parts.Add(part);
            _outlines.Add(outline);
            _areas.Add(CreateArea(filter.sharedMesh));
            PlaceArea(_areas.Count - 1);
        }

        /// <summary>
        /// Suelta la pieza y borra el contorno. Hay que llamarlo al cambiar de vehículo: el
        /// contorno cuelga de la malla <b>anterior</b>, y dejarlo ahí significa que la bici
        /// apagada se queda con el manubrio delineado.
        /// </summary>
        public void Release()
        {
            foreach (MeshRenderer outline in _outlines)
                if (outline != null)
                    Discard(outline.gameObject);
            foreach (BoxCollider area in _areas)
                if (area != null)
                    Discard(area.gameObject);
            _outlines.Clear();
            _areas.Clear();
            _parts.Clear();
            if (Bound.Remove(this))
                RefreshAreas();
        }

        /// <summary>
        /// Una caja con la malla de la pieza, colgada del punto —es el punto el que recibe el
        /// clic, y el evento sube por los padres, no baja—. Se pone sobre la pieza en
        /// <see cref="PlaceArea"/>.
        /// </summary>
        private BoxCollider CreateArea(Mesh mesh)
        {
            GameObject go = new GameObject(PartAreaName, typeof(BoxCollider));
            go.transform.SetParent(transform, false);
            go.layer = gameObject.layer;

            BoxCollider area = go.GetComponent<BoxCollider>();
            area.center = mesh.bounds.center;
            area.size = mesh.bounds.size;
            Collider sphere = GetComponent<Collider>();
            area.isTrigger = sphere == null || sphere.isTrigger;
            area.enabled = false;
            return area;
        }

        /// <summary>
        /// Encima de la pieza, cada vez que se enciende: <c>VehicleStage.MoveToPart</c> mueve el
        /// punto después de <see cref="Bind"/>, y el área, que cuelga de él, se iba con él
        /// —0,83 m al lado de la rueda en la escena—.
        /// </summary>
        private void PlaceArea(int index)
        {
            Transform part = _parts[index], area = _areas[index].transform;
            if (part == null)
                return;
            area.SetPositionAndRotation(part.position, part.rotation);
            Vector3 own = part.lossyScale, parent = transform.lossyScale;
            area.localScale = new Vector3(own.x / parent.x, own.y / parent.y, own.z / parent.z);
        }

        private bool Pending => _areas.Count > 0 && _highlighted && !_resolved && !_obscured &&
                                gameObject.activeInHierarchy;

        private static void RefreshAreas()
        {
            Bound.RemoveAll(h => h == null);
            int pending = 0;
            foreach (InspectionHighlight h in Bound)
                if (h.Pending)
                    pending++;
            foreach (InspectionHighlight h in Bound)
            {
                bool on = h.Pending && pending == 1;
                for (int i = 0; i < h._areas.Count; i++)
                {
                    BoxCollider area = h._areas[i];
                    if (area == null)
                        continue;
                    if (on && !area.enabled)
                        h.PlaceArea(i);
                    area.enabled = on;
                }
            }
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
            RefreshAreas();
            foreach (MeshRenderer outline in _outlines)
            {
                if (outline == null)
                    continue;
                outline.enabled = !_obscured && (_highlighted || _resolved);

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                outline.GetPropertyBlock(block);
                block.SetColor(OutlineColorId, _resolved ? _resolvedColor : _activeColor);
                outline.SetPropertyBlock(block);
            }
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
