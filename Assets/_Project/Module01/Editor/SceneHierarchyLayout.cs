using System.Collections.Generic;
using System.Linq;
using RideSafe.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Ordena la raíz de la escena en carpetas por función (PO, 2026-10-27): escenario,
    /// iluminación, jugador, vehículos, EPP, UI y lógica. Idempotente, y no mueve nada: las
    /// carpetas están en el origen y cada objeto conserva su posición en el mundo.
    /// <para>
    /// Lo que está en la raíz y no figura en <see cref="Folders"/> se avisa y se deja donde está:
    /// una entrega de arte o un sistema nuevo tiene que decidir su carpeta, no caer en una al azar.
    /// </para>
    /// </summary>
    public static class SceneHierarchyLayout
    {
        public readonly struct Folder
        {
            public Folder(string name, string parent, params string[] members)
            {
                Name = name;
                Parent = parent;
                Members = members;
            }

            public string Name { get; }

            /// <summary>Carpeta que la contiene, o null si va en la raíz.</summary>
            public string Parent { get; }

            /// <summary>Objetos de primer nivel que guarda, por nombre.</summary>
            public IReadOnlyList<string> Members { get; }
        }

        /// <summary>Las carpetas, en el orden en que salen en el Hierarchy.</summary>
        public static readonly Folder[] Folders =
        {
            new Folder("--- ESCENARIO ---", null,
                       "interior garage", "barrio_exterior", "enviroment_decoration", "House04",
                       "Garage Floor Collider"),
            new Folder("--- ILUMINACION ---", null, "luces", "Directional Light", "FX"),
            new Folder("--- JUGADOR ---", null, "XRPlayer", "DesktopMode", "Camera", "Main Camera"),
            new Folder("--- VEHICULOS ---", null, "ebike", "E_Scooter", "Module00 Vehicle Showroom"),
            // 'epps' trae los del artista, en su sitio (Module01ArtEpp); 'Module01_Spawned', los «!».
            new Folder("--- EPP ---", null, "epps", "Module01_Spawned"),
            new Folder("--- UI ---", null, "PF_VideoScreen", "PF_Subtitles", "TutorialHelp"),
            new Folder("--- LOGICA ---", null, "Module01", "Module01_Anchors", "Narration"),
        };

        [MenuItem("RideSafe/Escena/Ordenar el Hierarchy por función")]
        public static void ApplyToOpenScene()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            Apply(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void Apply(Scene scene)
        {
            Dictionary<string, Transform> made = new Dictionary<string, Transform>();
            int index = 0;
            foreach (Folder folder in Folders)
            {
                Transform parent = folder.Parent != null ? made[folder.Parent] : null;
                Transform holder = EnsureFolder(scene, folder.Name, parent);
                if (parent == null)
                    holder.SetSiblingIndex(index++);
                made[folder.Name] = holder;

                foreach (string member in folder.Members)
                    foreach (GameObject go in SceneFolder.TopLevelObjects(scene).Where(g => g.name == member).ToList())
                        if (go.transform.parent != holder)
                            Undo.SetTransformParent(go.transform, holder, "Ordenar el Hierarchy");
            }

            RemoveRetiredFolders(scene);

            string[] loose = scene.GetRootGameObjects()
                .Where(r => r.GetComponent<SceneFolder>() == null)
                .Select(r => r.name).ToArray();
            if (loose.Length > 0)
                Debug.LogWarning("[Escena] Sin carpeta declarada en SceneHierarchyLayout.Folders, se quedan en la " +
                                 "raíz: " + string.Join(", ", loose));
            else
                Debug.Log($"[Escena] Hierarchy de {scene.name} ordenado en {Folders.Length} carpetas.");
        }

        /// <summary>
        /// Borra las carpetas que ya no están en <see cref="Folders"/> y se quedaron vacías, como
        /// «Retirados (apagados)» cuando los EPP retirados pasaron a ser decorado. Una con algo
        /// dentro no se toca: lo suyo se avisa como suelto.
        /// </summary>
        private static void RemoveRetiredFolders(Scene scene)
        {
            HashSet<string> declared = new HashSet<string>(Folders.Select(f => f.Name));
            foreach (SceneFolder folder in scene.GetRootGameObjects()
                         .SelectMany(r => r.GetComponentsInChildren<SceneFolder>(true)).ToList())
                if (!declared.Contains(folder.name) && folder.transform.childCount == 0)
                    Undo.DestroyObjectImmediate(folder.gameObject);
        }

        private static Transform EnsureFolder(Scene scene, string name, Transform parent)
        {
            IEnumerable<Transform> siblings = parent != null
                ? parent.Cast<Transform>()
                : scene.GetRootGameObjects().Select(r => r.transform);
            Transform existing = siblings.FirstOrDefault(t => t.name == name && t.GetComponent<SceneFolder>() != null);
            if (existing != null)
                return existing;

            GameObject go = new GameObject(name, typeof(SceneFolder));
            SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, "Carpeta del Hierarchy");
            // En el origen, sin giro ni escala: así lo que se mete dentro no se mueve.
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go.transform;
        }
    }
}
