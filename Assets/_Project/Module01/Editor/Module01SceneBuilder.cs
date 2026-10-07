using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using RideSafe.TaskSequence;
using RideSafe.UI;
using EntityId = RideSafe.TaskSequence.EntityId;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Monta la escena jugable del módulo 1 sobre la escena de trabajo, trayendo el
    /// entorno nuevo del equipo de arte.
    /// <para>
    /// Se conserva <c>RideSafe_Garaje</c> como base porque ahí vive toda la lógica ya
    /// verificada —módulo 0, tutorial, narración, subtítulos, modos de demo— y se
    /// trasplanta el mundo, que son objetos sin estado. Hacerlo al revés obligaría a
    /// reconstruir ese cableado.
    /// </para>
    /// <para>
    /// <b>OJO: no edita la escena de salida, la regenera.</b> Parte siempre de
    /// <c>RideSafe_Garaje</c> y escribe encima de <c>RideSafe_Garaje_v5</c>, así que
    /// borra todo lo que se le haya añadido a v5 después —entre otras cosas la
    /// presentación y el <c>Module01Experience</c> que pone
    /// <c>Module01PresentationBuilder</c>—. No duplica nada, pero tampoco conserva nada:
    /// después de correrlo hay que volver a montar la presentación.
    /// </para>
    /// <para>
    /// Para cambios acotados sobre la escena viva, usar el comando que corresponda
    /// (p. ej. <see cref="Module01VehicleWiring"/>), no esto.
    /// </para>
    /// </summary>
    public static class Module01SceneBuilder
    {
        private const string LogicScene = "Assets/_Project/Scene/RideSafe_Garaje.unity";
        private const string ArtScene = "Assets/_Project/Models/CITY/scenas/Garage.unity";
        private const string OutputScene = "Assets/_Project/Scene/RideSafe_Garaje_v5.unity";

        /// <summary>Raíces del mundo que se traen de la escena de arte.</summary>
        private static readonly string[] WorldRoots =
        {
            "enviroment", "House04", "luces", "FX", "Box Volume", "Directional Light",
            "ebike", "ebike (1)", "scooter", "candado", "plataform", "plataform (1)",
            "casco 1", "guaya candado", "luz grontal", "luz roja 1",
            "chaleco refectivo", "gafas", "MobilePhone_01"
        };

        private static readonly string[] ZoneIds =
        {
            "head", "clothing", "load", "cockpit", "wheels", "visibility"
        };

        [MenuItem("RideSafe/Módulo 1/Montar escena jugable con el garaje nuevo")]
        public static void Build()
        {
            Scene logic = EditorSceneManager.OpenScene(LogicScene, OpenSceneMode.Single);
            Scene art = EditorSceneManager.OpenScene(ArtScene, OpenSceneMode.Additive);

            RemoveOldWorld(logic);
            MoveWorld(art, logic);
            EditorSceneManager.CloseScene(art, true);

            Module01SceneEntities.Apply();
            BuildAnchors();
            BuildOrchestrator();
            EnsurePhysicsRaycaster();

            EditorSceneManager.MarkSceneDirty(logic);
            EditorSceneManager.SaveScene(logic, OutputScene);
            Debug.Log("[Module01] Escena montada en " + OutputScene);
        }

        /// <summary>
        /// El entorno viejo se va entero: lo sustituye el del paquete. Tambien se
        /// limpian la carcasa de prueba y la luz direccional, que llegan duplicadas
        /// desde la escena de arte.
        /// </summary>
        private static void RemoveOldWorld(Scene logic)
        {
            string[] drop = { "enviroment", "luces", "Garage_v5", "Directional Light" };
            foreach (GameObject root in logic.GetRootGameObjects())
            {
                foreach (string name in drop)
                {
                    if (root.name == name)
                    {
                        Object.DestroyImmediate(root);
                        break;
                    }
                }
            }
        }

        private static void MoveWorld(Scene art, Scene logic)
        {
            HashSet<string> wanted = new HashSet<string>(WorldRoots);
            List<GameObject> moving = new List<GameObject>();

            foreach (GameObject root in art.GetRootGameObjects())
            {
                if (wanted.Contains(root.name))
                    moving.Add(root);
            }

            foreach (GameObject go in moving)
                SceneManager.MoveGameObjectToScene(go, logic);

            Debug.Log($"[Module01] Trasplantadas {moving.Count} raices del mundo nuevo.");
        }

        /// <summary>Un ancla por zona, en la posición del primer objeto que contiene.</summary>
        private static void BuildAnchors()
        {
            GameObject root = Find("Module01_Anchors") ?? new GameObject("Module01_Anchors");

            foreach (string zone in ZoneIds)
            {
                string name = "anchor_" + zone;
                Transform existing = root.transform.Find(name);
                if (existing != null)
                    continue;

                GameObject anchor = new GameObject(name);
                anchor.transform.SetParent(root.transform, false);

                GameObject reference = FirstObjectOfZone(zone);
                if (reference != null)
                {
                    // Dos metros atrás del objeto, a la altura de los ojos.
                    Vector3 p = reference.transform.position;
                    anchor.transform.position = new Vector3(p.x, 1.6f, p.z - 2f);
                    anchor.transform.LookAt(new Vector3(p.x, 1.6f, p.z));
                }
            }
        }

        private static GameObject FirstObjectOfZone(string zone)
        {
            string objName = Module01SceneEntities.FirstObjectNameOfZone(zone);
            return (objName == null ? null : Find(objName)) ?? Find("ebike");
        }

        /// <summary>El objeto que gobierna el módulo, con sus componentes cableados.</summary>
        private static void BuildOrchestrator()
        {
            GameObject root = Find("Module01") ?? new GameObject("Module01");

            Module01Binding binding = root.GetComponent<Module01Binding>() ?? root.AddComponent<Module01Binding>();
            ItemInspector inspector = root.GetComponent<ItemInspector>() ?? root.AddComponent<ItemInspector>();
            ZoneRunner runner = root.GetComponent<ZoneRunner>() ?? root.AddComponent<ZoneRunner>();
            GuidedTour tour = root.GetComponent<GuidedTour>() ?? root.AddComponent<GuidedTour>();
            ReportPresenter report = root.GetComponent<ReportPresenter>() ?? root.AddComponent<ReportPresenter>();
            Module01Director director = root.GetComponent<Module01Director>() ?? root.AddComponent<Module01Director>();

            Module01CatalogSO catalog = AssetDatabase.LoadAssetAtPath<Module01CatalogSO>(
                "Assets/_Project/Module01/Data/SO_Module01Catalog.asset");

            ModuleUI moduleUi = null;
            GameObject ui = Find("PF_Module01_UI");
            if (ui != null)
                moduleUi = ui.GetComponentInChildren<ModuleUI>(true);

            Set(binding, "_module", moduleUi);
            Set(runner, "_inspector", inspector);
            Set(runner, "_binding", binding);
            Set(report, "_catalog", catalog);
            Set(report, "_binding", binding);
            Set(director, "_catalog", catalog);
            Module01VehicleWiring.Apply(director);
            Set(director, "_tour", tour);
            Set(director, "_runner", runner);
            Set(director, "_report", report);

            GameObject anchors = Find("Module01_Anchors");
            if (anchors != null)
            {
                // El rig que se mueve es el del modo PC si esta, si no el XRPlayer.
                GameObject rig = Find("DesktopMode") ?? Find("XRPlayer");
                if (rig != null)
                    Set(tour, "_rig", rig.transform);
            }
        }

        /// <summary>Sin esto los objetos 3D nunca reciben eventos de puntero con el mouse.</summary>
        private static void EnsurePhysicsRaycaster()
        {
            foreach (Camera cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (cam.GetComponent<PhysicsRaycaster>() == null)
                    cam.gameObject.AddComponent<PhysicsRaycaster>();
            }

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }

        private static void Set(Object target, string field, Object value)
        {
            if (target == null)
                return;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogWarning($"[Module01] {target.GetType().Name} no tiene el campo {field}.");
                return;
            }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static GameObject Find(string name)
        {
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (go.name == name)
                    return go;
            return null;
        }
    }
}
