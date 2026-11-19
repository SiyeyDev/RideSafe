using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Deja en las mesas solo los EPP de la hoja del cliente (reunión del 2026-11-11: «solo cuentan
    /// los EPP del documento; el resto sobra») y los reparte sin que se encimen.
    /// <para>
    /// Quita de la escena lo que salió de <see cref="Module01Library"/> (<see cref="Module01Library.Retired"/>),
    /// saca el casco de correa cortada (H01) del prefab de decoración apagado donde lo dejó el
    /// arte —puntuaba −30 y no se veía— y vuelve a correr el reparto de las mesas. Idempotente.
    /// </para>
    /// </summary>
    public static class Module01ClientListCleanup
    {
        public const string ScenePath = "Assets/_Project/Scene/RideSafe_Garaje_v5.unity";
        private const string EppRoot = "epps";

        [MenuItem("RideSafe/Módulo 1/Dejar solo los EPP de la hoja del cliente")]
        public static void ApplyToOpenScene()
        {
            int removed = RemoveRetired();
            RescueHelmetStrap();
            LayAlong("rain_jacket", depthAlongX: true);
            // El poncho va colgado donde lo dejó el arte; la primera pasada de este comando lo bajó a la tapa.
            RestorePose("poncho", new Vector3(3.737f, -0.025f, 5.459f),
                new Quaternion(-0.5000008f, -0.49999923f, -0.49999923f, 0.5000008f));
            // El artista tiene `casco 1` a 1,1 en su escena; la copia de v5 se quedó en 1 desde el
            // garaje v5 y no se veía porque la caja `epps` dejó de compararse.
            WorldObjectTaskEntity damaged = Object.FindObjectsByType<WorldObjectTaskEntity>(FindObjectsInactive.Include)
                .FirstOrDefault(x => x.ItemId == "helmet_damaged");
            if (damaged != null)
                damaged.transform.localScale = Vector3.one * 1.1f;
            Module01SceneEntities.Apply();
            Module01EppLayout.LayoutOpenScene();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[M1-BATCH] Fuera " + removed + " objetos de EPP que no están en la hoja.");
        }

        /// <summary>Para -executeMethod: rehace el catálogo, limpia la v5 y la guarda.</summary>
        public static void RunBatch()
        {
            Module01Greybox.RebuildEppCatalog();
            DeleteRetiredAssets();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ApplyToOpenScene();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }

        /// <summary>Para -executeMethod: abre la v5 y la revisa sin mover nada.</summary>
        public static void CheckBatch()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Module01EppLayout.CheckOpenScene();
        }

        private static int RemoveRetired()
        {
            HashSet<string> retired = new HashSet<string>(Module01Library.Retired);
            var doomed = new HashSet<GameObject>();
            foreach (WorldObjectTaskEntity e in Object.FindObjectsByType<WorldObjectTaskEntity>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (retired.Contains(e.ItemId))
                    doomed.Add(e.gameObject);

            // Las instancias de arte se llaman «P12_damaged_cable_lock»; algunas no llevan entidad.
            GameObject root = GameObject.Find(EppRoot);
            if (root != null)
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (System.Text.RegularExpressions.Regex.IsMatch(t.name, @"^P\d\d_"))
                        doomed.Add(t.gameObject);

            int count = 0;
            foreach (GameObject go in doomed)
            {
                if (go == null)
                    continue;
                GameObject target = PrefabUtility.IsPartOfPrefabInstance(go) && !PrefabUtility.IsOutermostPrefabInstanceRoot(go)
                    ? null
                    : go;
                if (target == null)
                {
                    // Hijo de otro prefab (la decoración del garaje): no se puede borrar, se apaga.
                    go.SetActive(false);
                    foreach (WorldObjectTaskEntity e in go.GetComponents<WorldObjectTaskEntity>())
                        Object.DestroyImmediate(e);
                }
                else
                    Object.DestroyImmediate(target);
                count++;
            }
            return count;
        }

        /// <summary>
        /// H01 vive dentro de <c>enviroment_decoration</c>, cuya raíz está apagada: se desempaqueta
        /// esa instancia y el casco pasa a <c>epps</c>, encendido.
        /// </summary>
        private static void RescueHelmetStrap()
        {
            WorldObjectTaskEntity strap = Object.FindObjectsByType<WorldObjectTaskEntity>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(e => e.ItemId == "helmet_strap");
            GameObject root = GameObject.Find(EppRoot);
            if (strap == null || root == null || strap.transform.IsChildOf(root.transform))
                return;

            GameObject outer = PrefabUtility.GetOutermostPrefabInstanceRoot(strap.gameObject);
            if (outer != null)
                PrefabUtility.UnpackPrefabInstance(outer, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            strap.transform.SetParent(root.transform, true);
            strap.gameObject.SetActive(true);
            Debug.Log("[M1-BATCH] H01 sale de la decoración apagada y pasa a 'epps'.");
        }

        /// <summary>
        /// Una prenda más honda que larga se gira 90° para que no cuelgue del borde de su mueble.
        /// La mesa de la entrada se mira a 90°, así que su fondo es la X del mundo. Idempotente.
        /// </summary>
        private static void LayAlong(string itemId, bool depthAlongX)
        {
            WorldObjectTaskEntity e = Object.FindObjectsByType<WorldObjectTaskEntity>(FindObjectsInactive.Include)
                .FirstOrDefault(x => x.ItemId == itemId);
            if (e == null)
                return;
            Renderer[] renderers = e.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers)
                b.Encapsulate(r.bounds);
            if (depthAlongX ? b.size.x > b.size.z : b.size.z > b.size.x)
                e.transform.RotateAround(b.center, Vector3.up, 90f);
        }

        private static void RestorePose(string itemId, Vector3 position, Quaternion rotation)
        {
            WorldObjectTaskEntity e = Object.FindObjectsByType<WorldObjectTaskEntity>(FindObjectsInactive.Include)
                .FirstOrDefault(x => x.ItemId == itemId);
            if (e != null)
                e.transform.SetPositionAndRotation(position, rotation);
        }

        private static void DeleteRetiredAssets()
        {
            foreach (string id in Module01Library.Retired)
                AssetDatabase.DeleteAsset($"Assets/_Project/Module01/Data/Items/SO_Item_{id}.asset");
            foreach (string guid in AssetDatabase.FindAssets("RS_M1_P t:Texture2D", new[] { EppThumbnailRenderer.Folder }))
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
        }
    }
}
