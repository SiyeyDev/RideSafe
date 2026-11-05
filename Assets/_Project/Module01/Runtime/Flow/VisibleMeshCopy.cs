using System.Collections.Generic;
using UnityEngine;

namespace RideSafe.Module01
{
    /// <summary>
    /// Una COPIA de las mallas visibles de un objeto, con la pose que tienen en el mundo. La usan
    /// el pedestal (<see cref="ItemInspector"/>) y el fotógrafo de las tarjetas de EPP, para que
    /// lo que se ve en la tarjeta sea exactamente lo que se ve en el pedestal y en la mesa.
    /// <para>
    /// Copia y no el original: mover la pieza la haría desaparecer de la bici —o el EPP de la
    /// mesa— justo mientras el aprendiz la inspecciona.
    /// </para>
    /// <para>
    /// Solo lo <b>visible</b>: las esferas de clic de los cinco puntos del vehículo llevan el
    /// renderer apagado, y de ahí no se puede copiar nada.
    /// </para>
    /// </summary>
    public static class VisibleMeshCopy
    {
        /// <summary>
        /// La copia bajo <paramref name="parent"/>, o null si no hay nada que copiar o si una pieza
        /// está fundida por el static batching (entonces hay que ir al prefab).
        /// </summary>
        public static GameObject Of(IEnumerable<MeshRenderer> renderers, string rootName, Transform parent)
        {
            GameObject root = null;
            foreach (MeshRenderer renderer in renderers)
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
                            Object.Destroy(root);
                        else
                            Object.DestroyImmediate(root);
                    }
                    return null;
                }

                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                    continue;

                if (root == null)
                {
                    root = new GameObject(rootName);
                    root.transform.SetParent(parent, false);
                }

                GameObject piece = new GameObject(
                    renderer.gameObject.name, typeof(MeshFilter), typeof(MeshRenderer));
                piece.transform.SetParent(root.transform, false);
                // La pose del MUNDO, que es la que el arte le dio. Quien la usa recoloca y escala
                // el conjunto despues, asi que da igual donde caiga ahora.
                piece.transform.SetPositionAndRotation(
                    renderer.transform.position, renderer.transform.rotation);
                piece.transform.localScale = renderer.transform.lossyScale;
                piece.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                piece.GetComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
            }

            return root;
        }
    }
}
