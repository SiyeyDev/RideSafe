using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// El podio del artista bajo lo que se presenta (PO, 2026-11-06), en lugar del cilindro
    /// primitivo: <c>podium.fbx</c>, a su tamaño real y con su material emisivo. No trae luces; su
    /// «iluminación» es esa emisión.
    /// <para>
    /// <c>InspectorPedestal</c> se queda, porque <see cref="ItemInspector"/> lo enciende y lo apaga,
    /// y pasa a ser un grupo: la instancia del FBX, sin prefab de por medio para que una entrega
    /// nueva del artista llegue sola, y <c>Surface</c>, la cara de arriba donde se apoyan los EPP.
    /// </para>
    /// <para>
    /// El popup no se toca. El disco se adelanta hacia la cámara y en pantalla su borde cercano
    /// cuelga más que su centro; al pasar de 26 a 40 cm bajaba hasta el popup, y bajar el popup
    /// dejaba sus botones en el límite de la vista. Así que el podio sube lo justo para que ese borde
    /// caiga donde caía el del cilindro. Idempotente: posición absoluta.
    /// </para>
    /// </summary>
    internal static class Module01Podium
    {
        public const string ModelPath = "Assets/_Project/Models/CITY/MODELADOS 3d/podium.fbx";
        private const string ModelName = "Podium";
        private const string SurfaceName = "Surface";
        /// <summary>Cómo mira el podio al aprendiz (PO, 2026-11-06). Va con la cabeza: la misma cara en cada parada.</summary>
        public const float PodiumYaw = -80f;
        /// <summary>
        /// El borde cercano e inferior del cilindro de 26 cm, en la presentación (centro en −.23 y
        /// 1.12, medio alto .02): lo que el popup ya deja despejado.
        /// </summary>
        private const float ClearedRimY = -.25f, ClearedRimZ = .99f;

        [MenuItem("RideSafe/Módulo 1/Poner el podio del artista bajo los EPP")]
        public static void ApplyToOpenScene()
        {
            Apply();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Module01] Podium in " + scene.path);
        }

        public static void Apply()
        {
            ItemInspector inspector = Object.FindAnyObjectByType<ItemInspector>(FindObjectsInactive.Include);
            if (inspector == null)
                throw new System.InvalidOperationException("No hay ItemInspector en la escena abierta.");
            SerializedObject serialized = new SerializedObject(inspector);
            GameObject pedestal = (GameObject)serialized.FindProperty("_pedestal").objectReferenceValue;
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (pedestal == null || model == null)
                throw new System.InvalidOperationException("Falta el pedestal del inspector o " + ModelPath);

            // El cilindro primitivo y su escala aplastada.
            foreach (Component primitive in new Component[] { pedestal.GetComponent<MeshRenderer>(), pedestal.GetComponent<MeshFilter>() })
                if (primitive != null)
                    Object.DestroyImmediate(primitive);
            pedestal.transform.localScale = Vector3.one;

            Transform podium = pedestal.transform.Find(ModelName);
            if (podium == null)
            {
                podium = ((GameObject)PrefabUtility.InstantiatePrefab(model, pedestal.transform)).transform;
                podium.name = ModelName;
            }
            podium.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, PodiumYaw, 0f));
            podium.localScale = Vector3.one;
            Bounds mesh = podium.GetComponent<MeshFilter>().sharedMesh.bounds;
            pedestal.transform.localPosition = WhereTheRimStaysClear(pedestal.transform.localPosition, mesh);

            Transform surface = pedestal.transform.Find(SurfaceName);
            if (surface == null)
            {
                surface = new GameObject(SurfaceName).transform;
                surface.SetParent(pedestal.transform, false);
            }
            surface.localPosition = new Vector3(0f, mesh.max.y, 0f);
            serialized.FindProperty("_surface").objectReferenceValue = surface;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(pedestal);
        }

        /// <summary>A la altura en la que el borde cercano del disco se proyecta donde el del cilindro.</summary>
        private static Vector3 WhereTheRimStaysClear(Vector3 position, Bounds mesh)
        {
            float rimZ = position.z - mesh.extents.z;
            position.y = ClearedRimY / ClearedRimZ * rimZ - mesh.min.y;
            return position;
        }
    }
}
