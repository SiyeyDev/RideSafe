using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RideSafe.Module01
{
    /// <summary>
    /// Marca el EPP de la mesa como «ya seleccionado» con un holograma verde.
    /// <para>
    /// Lo pidió el cliente el 2026-10-16. Antes un elemento elegido <b>desaparecía</b> de la
    /// mesa, y desaparecido tenía el collider apagado: no recibía el clic y no había forma de
    /// cambiar de opinión sin ir al checklist. Marcado y a la vista, se puede volver a pulsar.
    /// </para>
    /// <para>
    /// <b>Hijos con la misma malla y el renderer original apagado</b>, nunca un material
    /// sustituido en el renderer del arte. Tres razones, y las tres ya costaron tiempo en este
    /// proyecto: cambiar la lista de materiales de un renderer solo afecta a la primera
    /// submalla; restaurar «lo que había antes» a mano es la clase de cosa que deja una
    /// regresión de arte que no aparece en ningún <c>git diff</c>; y así apagar la marca es
    /// borrar un hijo, sin nada que deshacer.
    /// </para>
    /// <para>
    /// El original se apaga mientras el holograma está puesto porque si no los dos se dibujan a
    /// la vez y no se lee como holograma, se lee como el objeto con un tinte raro.
    /// </para>
    /// <para>
    /// Mismo trato que <see cref="InspectionHighlight"/>: el color va por
    /// <see cref="MaterialPropertyBlock"/>, así los trece EPP comparten un material.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/Module 01/Selected Holo")]
    [DisallowMultipleComponent]
    public class SelectedHolo : MonoBehaviour
    {
        /// <summary>Nombre de los hijos que llevan el holograma. Se excluyen al barrer.</summary>
        public const string MarkName = "SelectedHolo";

        private static readonly int HoloColorId = Shader.PropertyToID("_HoloColor");

        [Tooltip("Material del holograma. M_SelectedHolo.")]
        [SerializeField] private Material _holoMaterial;

        [Tooltip("Verde de «ya seleccionado». El mismo que el de un punto resuelto.")]
        [SerializeField] private Color _color = new Color(.46f, .83f, .54f, .9f);

        private readonly List<Renderer> _marks = new List<Renderer>();
        private readonly List<Renderer> _hidden = new List<Renderer>();

        /// <summary>¿Está puesto el holograma?</summary>
        public bool IsOn { get; private set; }

        /// <summary>Las mallas en verde. Un par de coderas son dos.</summary>
        public IReadOnlyList<Renderer> Marks => _marks;

        /// <summary>
        /// El color que va a llegar al shader, leído del bloque de propiedades y no de un campo
        /// guardado: lo que importa es lo que se va a dibujar.
        /// </summary>
        public Color CurrentColor
        {
            get
            {
                if (_marks.Count == 0 || _marks[0] == null)
                    return Color.clear;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                _marks[0].GetPropertyBlock(block);
                return block.GetColor(HoloColorId);
            }
        }

        public void SetOn(bool value)
        {
            if (IsOn == value)
                return;
            IsOn = value;
            if (value)
                Build();
            else
                Clear();
        }

        private void OnDestroy() => Clear();

        /// <summary>
        /// Cuelga una malla en verde de cada malla visible y apaga la original. Un objeto sin
        /// mallas visibles —las esferas de clic de los cinco puntos del vehículo tienen el
        /// renderer apagado— no deja nada y no es un fallo.
        /// </summary>
        private void Build()
        {
            Clear();

            foreach (MeshRenderer source in GetComponentsInChildren<MeshRenderer>(true))
            {
                if (source == null || !source.enabled)
                    continue;
                // Ni la propia marca ni el contorno del vehículo: son avisos, no el objeto.
                if (source.gameObject.name == MarkName ||
                    source.gameObject.name == InspectionHighlight.OutlineName)
                    continue;

                MeshFilter filter = source.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                    continue;

                GameObject go = new GameObject(MarkName, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(source.transform, false);
                go.layer = source.gameObject.layer;
                go.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;

                MeshRenderer mark = go.GetComponent<MeshRenderer>();
                mark.sharedMaterial = _holoMaterial;
                // Un holograma no es geometría: ni proyecta sombra, ni la recibe, ni se
                // ilumina. Y sobre todo no entra en el horneado, que aquí es de los EPP de la
                // mesa y está hecho.
                mark.shadowCastingMode = ShadowCastingMode.Off;
                mark.receiveShadows = false;
                mark.lightProbeUsage = LightProbeUsage.Off;
                mark.reflectionProbeUsage = ReflectionProbeUsage.Off;
                mark.staticShadowCaster = false;

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                mark.GetPropertyBlock(block);
                block.SetColor(HoloColorId, _color);
                mark.SetPropertyBlock(block);

                _marks.Add(mark);
                _hidden.Add(source);
                source.enabled = false;
            }
        }

        /// <summary>Borra las marcas y devuelve los renderers que apagó.</summary>
        private void Clear()
        {
            foreach (Renderer mark in _marks)
                if (mark != null)
                    Discard(mark.gameObject);
            _marks.Clear();

            foreach (Renderer hidden in _hidden)
                if (hidden != null)
                    hidden.enabled = true;
            _hidden.Clear();
        }

        /// <summary>
        /// En modo edición <c>Destroy</c> no se aplica hasta el siguiente ciclo, así que la
        /// marca vieja seguiría colgando y el barrido siguiente marcaría la marca.
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
        public void ConfigureForTests(Material holoMaterial, Color color)
        {
            _holoMaterial = holoMaterial;
            _color = color;
        }
    }
}
