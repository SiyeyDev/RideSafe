using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RideSafe.UI
{
    /// <summary>
    /// Marca un objeto vacío como carpeta del Hierarchy: solo agrupa, en el origen, sin giro ni
    /// escala, y no tiene lógica.
    /// <para>
    /// Para el código las carpetas no existen: lo que estaba en la raíz y ahora vive en una sigue
    /// siendo «de primer nivel» (<see cref="TopLevel"/>, <see cref="TopLevelObjects"/>). Así
    /// ordenar la escena (PO, 2026-10-27) no cambia qué encuentra cada herramienta ni qué paneles
    /// considera suyos la pista del tutorial.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("RideSafe/Scene Folder")]
    public sealed class SceneFolder : MonoBehaviour
    {
        /// <summary>
        /// El ancestro de primer nivel de <paramref name="t"/>: el que cuelga de la raíz o de una
        /// carpeta. Es lo que antes daba <c>transform.root</c>.
        /// </summary>
        public static Transform TopLevel(Transform t)
        {
            while (t.parent != null && t.parent.GetComponent<SceneFolder>() == null)
                t = t.parent;
            return t;
        }

        /// <summary>Los objetos de primer nivel de la escena, entrando en las carpetas (también anidadas).</summary>
        public static IEnumerable<GameObject> TopLevelObjects(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (GameObject go in Expand(root))
                    yield return go;
        }

        private static IEnumerable<GameObject> Expand(GameObject go)
        {
            if (go.GetComponent<SceneFolder>() == null)
            {
                yield return go;
                yield break;
            }
            foreach (Transform child in go.transform)
                foreach (GameObject inner in Expand(child.gameObject))
                    yield return inner;
        }
    }
}
