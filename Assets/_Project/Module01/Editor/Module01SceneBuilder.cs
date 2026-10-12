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
    /// Es idempotente: vuelve a correrse sobre la escena resultante sin duplicar nada.
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

        /// <summary>(nombre del objeto en la escena, zona, id del elemento del catálogo).</summary>
        private static readonly (string Obj, string Zone, string Item)[] Entities =
        {
            ("casco 1",            "head",     "helmet_ok"),
            ("df_g_helmet_01",     "head",     "helmet_damaged"),
            ("gafas",              "head",     "glasses"),
            ("botas",              "clothing", "shoes_ok"),
            ("chaleco refectivo",  "clothing", "reflective"),
            ("guaya candado",      "load",     "cargo_secured"),
            ("MobilePhone_01",     "load",     "cargo_handheld")
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

            TagEntities();
            FillMissingEntities();
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

        /// <summary>Pone la entidad de tarea sobre cada objeto de EPP ya colocado por arte.</summary>
        private static void TagEntities()
        {
            foreach ((string objName, string zone, string item) in Entities)
            {
                GameObject go = Find(objName);
                if (go == null)
                {
                    Debug.LogWarning($"[Module01] No encuentro '{objName}' en la escena; {zone}.{item} queda sin entidad.");
                    continue;
                }

                if (go.GetComponent<Collider>() == null)
                {
                    BoxCollider box = go.AddComponent<BoxCollider>();
                    FitCollider(go, box);
                }

                WorldObjectTaskEntity entity = go.GetComponent<WorldObjectTaskEntity>();
                bool wasActive = go.activeSelf;
                go.SetActive(false);
                if (entity == null)
                    entity = go.AddComponent<WorldObjectTaskEntity>();
                entity.Configure(new EntityId($"module01.{zone}.{item}"), item);
                go.SetActive(wasActive);
            }
        }

        /// <summary>
        /// Toda zona debe tener una entidad por elemento, o nunca se cierra y el módulo
        /// se queda esperando. Lo que el equipo de arte no colocó se crea aquí: el
        /// modelo del catálogo si lo tiene, y si no una primitiva gris.
        /// </summary>
        private static void FillMissingEntities()
        {
            Module01CatalogSO catalog = AssetDatabase.LoadAssetAtPath<Module01CatalogSO>(
                "Assets/_Project/Module01/Data/SO_Module01Catalog.asset");
            VehicleProfileSO vehicle = AssetDatabase.LoadAssetAtPath<VehicleProfileSO>(
                "Assets/_Project/Module01/Data/SO_VehicleProfile_Ebike.asset");

            HashSet<string> present = new HashSet<string>();
            foreach (WorldObjectTaskEntity e in Object.FindObjectsByType<WorldObjectTaskEntity>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                present.Add(e.Id.Value);

            GameObject spare = Find("Module01_Spawned") ?? new GameObject("Module01_Spawned");
            GameObject bike = Find("ebike");
            int created = 0;

            List<(ZoneSO Zone, bool OnVehicle)> all = new List<(ZoneSO, bool)>();
            if (catalog != null)
                foreach (ZoneSO z in catalog.Zones) if (z != null) all.Add((z, false));
            if (vehicle != null)
                foreach (ZoneSO z in vehicle.InspectionZones) if (z != null) all.Add((z, true));

            foreach ((ZoneSO zone, bool onVehicle) in all)
            {
                for (int i = 0; i < zone.Items.Count; i++)
                {
                    SafetyItemSO item = zone.Items[i];
                    if (item == null)
                        continue;

                    string id = $"module01.{zone.ZoneId}.{item.ItemId}";
                    if (present.Contains(id))
                        continue;

                    GameObject go;
                    if (item.DisplayPrefab != null)
                    {
                        go = (GameObject)PrefabUtility.InstantiatePrefab(item.DisplayPrefab);
                        go.name = item.ItemId;
                    }
                    else
                    {
                        go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        go.name = item.ItemId;
                        go.transform.localScale = Vector3.one * 0.15f;
                    }

                    if (onVehicle && bike != null)
                    {
                        // Marcador sobre el vehiculo real, repartido a lo largo de el.
                        go.transform.SetParent(bike.transform, false);
                        go.transform.localPosition = new Vector3(0f, 0.4f + i * 0.25f, -0.4f + i * 0.4f);
                    }
                    else
                    {
                        go.transform.SetParent(spare.transform, false);
                        go.transform.position = new Vector3(i * 0.6f, 1.0f, 0f);
                    }

                    if (go.GetComponent<Collider>() == null)
                    {
                        BoxCollider box = go.AddComponent<BoxCollider>();
                        FitCollider(go, box);
                    }

                    bool wasActive = go.activeSelf;
                    go.SetActive(false);
                    WorldObjectTaskEntity entity = go.GetComponent<WorldObjectTaskEntity>()
                                                   ?? go.AddComponent<WorldObjectTaskEntity>();
                    entity.Configure(new EntityId(id), item.ItemId);
                    go.SetActive(wasActive);

                    present.Add(id);
                    created++;
                }
            }

            Debug.Log($"[Module01] Entidades creadas para completar las zonas: {created}.");
        }

        /// <summary>Ajusta el collider al volumen visible, que los FBX no traen uno.</summary>
        private static void FitCollider(GameObject go, BoxCollider box)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return;

            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers)
                b.Encapsulate(r.bounds);

            box.center = go.transform.InverseTransformPoint(b.center);
            box.size = b.size;
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
            foreach ((string objName, string z, string _) in Entities)
                if (z == zone)
                    return Find(objName);
            return Find("ebike");
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
            VehicleProfileSO vehicle = AssetDatabase.LoadAssetAtPath<VehicleProfileSO>(
                "Assets/_Project/Module01/Data/SO_VehicleProfile_Ebike.asset");

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
            Set(director, "_vehicle", vehicle);
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
