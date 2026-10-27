using System.Collections.Generic;
using System.Linq;
using System.Text;
using RideSafe.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RideSafe.Module00.Editor
{
    /// <summary>
    /// Gives module 0 in RideSafe_Garaje_v5 its own vehicle showroom: display copies of the
    /// e-bike and the e-scooter, each on a copy of the particle platform, shown under their
    /// cards while the Vehicle screen is open and turned by VehiclePreviewController when
    /// chosen. Module 1's real vehicles and platforms (inside the garage) are only read, never
    /// moved. Rebuilt from scratch on each run, so it is idempotent.
    /// </summary>
    public static class Module00ShowroomSetup
    {
        public const string ScenePath = "Assets/_Project/Scene/RideSafe_Garaje_v5.unity";
        public const string RootName = "Module00 Vehicle Showroom";
        // The learner starts at (0, 1.45, 10.3) and the closed garage door is at z = 6.62: 3.7 m.
        // Display pieces at 0.6 scale, 2.25 m away, fit side by side under their cards and turn
        // with 1.4 m of air before the door (0.4 m when the start was z = 9.3; PO, 2026-10-20).
        private const float Distance = 2.25f;
        public static readonly Vector3 LearnerStart = new Vector3(0f, 1.45f, 10.3f);

        /// <summary>
        /// Puts the PC learner (DesktopMode/Desktop Camera) at <see cref="LearnerStart"/>. Module 0's
        /// screens and showroom are placed from the learner, so they move with it.
        /// </summary>
        [MenuItem("RideSafe/Módulo 0/Separar de la puerta el inicio del aprendiz (Garaje_v5)")]
        public static void MoveLearnerStart()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            GameObject desktop = FindRoot(scene, "DesktopMode");
            Transform camera = desktop != null ? desktop.transform.Find("Desktop Camera") : null;
            if (camera == null)
                throw new System.InvalidOperationException("DesktopMode/Desktop Camera missing in " + ScenePath);
            camera.position = LearnerStart;
            EditorUtility.SetDirty(camera);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Module00] Learner start moved to " + LearnerStart);
        }
        private const float DisplayScale = 0.6f;

        /// <summary>
        /// Altura a la que se apoyan los vehículos de exhibición. <b>No es el suelo del garaje.</b>
        /// <para>
        /// Con 0 se cortaban por abajo, y no era cosa de la escena sino del encuadre: desde los
        /// ojos del aprendiz (y = 1,45) y a 2,25 m con 60° de campo, la vista cubre de
        /// <b>y = 0,15 a y = 2,75</b>, así que los 15 cm de abajo quedan fuera de cámara. Medido
        /// el 2026-10-16, con la bici ocupando 0,67 m de alto y el patinete 0,81.
        /// </para>
        /// <para>
        /// A 0,40 los dos caben enteros con 25 cm de margen por abajo, y por arriba se quedan
        /// lejos de las tarjetas, que empiezan en y = 1,48. Sube el conjunto: el vehículo y los
        /// nueve sistemas de partículas de la plataforma, que simulan en espacio local y
        /// cuelgan del mismo ancla.
        /// </para>
        /// </summary>
        private const float GroundHeight = 0.40f;

        private static readonly (string Id, string Model, string Label)[] Vehicles =
        {
            ("ebike", "ebike", "Option_ebike"),
            ("escooter", "E_Scooter", "Option_escooter")
        };

        [MenuItem("RideSafe/Módulo 0/Montar vitrina de vehículos (Garaje_v5)")]
        public static void Build()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                StringBuilder report = new StringBuilder("[Module00] Vehicle showroom in RideSafe_Garaje_v5:\n");
                ModuleUI module = FindModule(scene);
                GameObject platformSource = FindByName(scene, "plataform", null);

                GameObject old = FindRoot(scene, RootName);
                if (old != null)
                    Object.DestroyImmediate(old);
                GameObject root = new GameObject(RootName);
                SceneManager.MoveGameObjectToScene(root, scene);
                GameObject stage = new GameObject("Stage");
                stage.transform.SetParent(root.transform, false);

                VehiclePreviewController preview = root.AddComponent<VehiclePreviewController>();
                SerializedObject serializedPreview = new SerializedObject(preview);
                SerializedProperty entries = serializedPreview.FindProperty("_vehicles");
                entries.arraySize = Vehicles.Length;
                List<(Transform, string)> slots = new List<(Transform, string)>();

                for (int i = 0; i < Vehicles.Length; i++)
                {
                    Transform slot = new GameObject("Slot_" + Vehicles[i].Id).transform;
                    slot.SetParent(stage.transform, false);
                    GameObject model = DisplayCopy(FindByName(scene, Vehicles[i].Model, root.transform), slot);
                    GameObject platform = Object.Instantiate(platformSource, slot);
                    platform.name = "Platform";
                    platform.transform.localPosition = Vector3.zero;
                    platform.transform.localRotation = Quaternion.identity;
                    // Its particles scale their shape with the hierarchy (scalingMode Shape):
                    // the ring made for a full-size vehicle shrinks to the display piece.
                    platform.transform.localScale = Vector3.one * DisplayScale;

                    SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                    entry.FindPropertyRelative("_id").stringValue = Vehicles[i].Id;
                    entry.FindPropertyRelative("_model").objectReferenceValue = model.transform;
                    entry.FindPropertyRelative("_platform").objectReferenceValue = platform;
                    slots.Add((slot, Vehicles[i].Label));
                    report.AppendLine("  " + Vehicles[i].Id + ": copy of " + Vehicles[i].Model + " on a copy of " + platformSource.name);
                }
                serializedPreview.ApplyModifiedPropertiesWithoutUndo();

                VehiclePanelPreviewBinding binding = root.AddComponent<VehiclePanelPreviewBinding>();
                SerializedObject serializedBinding = new SerializedObject(binding);
                serializedBinding.FindProperty("_module").objectReferenceValue = module;
                serializedBinding.FindProperty("_preview").objectReferenceValue = preview;
                serializedBinding.ApplyModifiedPropertiesWithoutUndo();

                VehicleShowroom showroom = root.AddComponent<VehicleShowroom>();
                showroom.Configure(null, stage, Distance, GroundHeight, slots.ToArray());
                SerializedObject serializedShowroom = new SerializedObject(showroom);
                serializedShowroom.FindProperty("_module").objectReferenceValue = module;
                serializedShowroom.ApplyModifiedPropertiesWithoutUndo();
                stage.SetActive(false);

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new System.InvalidOperationException("Could not save " + ScenePath);
                Debug.Log(report.ToString());
            }
            finally
            {
                if (openedHere)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>
        /// Remakes only the vehicle copies whose meshes no longer match their vehicle, where they
        /// stand. The copies are not prefab instances, so a new FBX does not reach them: with the
        /// e-bike of 2026-10-21 the copy kept <c>manubrio</c> and <c>rueda_trasera</c>, which no
        /// longer exist. <see cref="Build"/> would also remake the platforms, labels and the rest.
        /// </summary>
        [MenuItem("RideSafe/Módulo 0/Rehacer las copias de la vitrina desde los vehículos (Garaje_v5)")]
        public static void RefreshDisplayCopies()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.isLoaded)
                throw new System.InvalidOperationException("Open " + ScenePath + " first.");
            GameObject root = FindRoot(scene, RootName);
            if (root == null)
                throw new System.InvalidOperationException(ScenePath + " has no showroom; run Build.");

            VehiclePreviewController preview = root.GetComponent<VehiclePreviewController>();
            SerializedObject serializedPreview = new SerializedObject(preview);
            SerializedProperty entries = serializedPreview.FindProperty("_vehicles");
            StringBuilder report = new StringBuilder("[Module00] Showroom copies:\n");
            foreach ((string id, string modelName, string _) in Vehicles)
            {
                Transform slot = root.transform.Find("Stage/Slot_" + id);
                Transform old = ShownModel(entries, id);
                GameObject source = FindByName(scene, modelName, root.transform);
                if (old != null && SameMeshes(source.transform, old))
                {
                    report.AppendLine("  " + id + ": up to date");
                    continue;
                }

                GameObject copy = DisplayCopy(source, slot);
                if (old != null)
                {
                    // Where the old one stood: VehicleShowroom places the slot, not the copy.
                    // Rotation and scale stay as DisplayCopy derives them from the vehicle: a new
                    // model may lie along another axis (E_Scooter is long in Z) or stand at
                    // another size (×1.4), and the old copy's values would undo both.
                    copy.transform.localPosition = old.localPosition;
                    copy.transform.SetSiblingIndex(old.GetSiblingIndex());
                    copy.SetActive(old.gameObject.activeSelf);
                    Object.DestroyImmediate(old.gameObject);
                }
                for (int i = 0; i < entries.arraySize; i++)
                {
                    SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                    if (entry.FindPropertyRelative("_id").stringValue == id)
                        entry.FindPropertyRelative("_model").objectReferenceValue = copy.transform;
                }
                report.AppendLine("  " + id + ": remade from " + modelName);
            }
            serializedPreview.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new System.InvalidOperationException("Could not save " + ScenePath);
            Debug.Log(report.ToString());
        }

        /// <summary>
        /// The copy the showroom shows for that vehicle, by the reference it keeps. Not by name:
        /// when a vehicle's model changes the old copy keeps the old name, and a lookup by the new
        /// one would leave it standing in the slot.
        /// </summary>
        private static Transform ShownModel(SerializedProperty entries, string id)
        {
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("_id").stringValue == id)
                    return entry.FindPropertyRelative("_model").objectReferenceValue as Transform;
            }
            return null;
        }

        private static bool SameMeshes(Transform a, Transform b)
        {
            List<string> Names(Transform t)
            {
                List<string> names = new List<string>();
                foreach (MeshFilter filter in t.GetComponentsInChildren<MeshFilter>(true))
                    names.Add(filter.sharedMesh != null ? filter.sharedMesh.name : "<missing>");
                names.Sort();
                return names;
            }
            List<string> left = Names(a), right = Names(b);
            return !right.Contains("<missing>") && left.Count == right.Count && !left.Where((n, i) => n != right[i]).Any();
        }

        /// <summary>
        /// A copy that only looks like the vehicle: no colliders, no module behaviours, no prefab
        /// link. Turned side-on to the learner (the slot faces away from the learner).
        /// </summary>
        private static GameObject DisplayCopy(GameObject source, Transform slot)
        {
            GameObject copy = Object.Instantiate(source, slot);
            copy.name = source.name + " (display)";
            copy.SetActive(true);
            // Behaviours first: module scripts such as WorldObjectTaskEntity require their collider.
            foreach (MonoBehaviour behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true))
                Object.DestroyImmediate(behaviour);
            foreach (Collider collider in copy.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
            copy.transform.localPosition = Vector3.zero;
            copy.transform.localRotation = Quaternion.identity;
            // 0.6 of the vehicle as the artist sized it, not 0.6 of the FBX: the 2026-10-26
            // scooter stands in the garage at ×1.4, and a fixed 0.6 shrank it to half the bike.
            copy.transform.localScale = source.transform.localScale * DisplayScale;

            Bounds bounds = new Bounds(copy.transform.position, Vector3.zero);
            foreach (Renderer renderer in copy.GetComponentsInChildren<Renderer>(true))
                bounds.Encapsulate(renderer.bounds);
            Vector3 size = slot.InverseTransformVector(bounds.size);
            if (Mathf.Abs(size.z) > Mathf.Abs(size.x))
                copy.transform.localRotation = Quaternion.Euler(0f, 90f, 0f); // long axis across the view
            return copy;
        }

        private static ModuleUI FindModule(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (ModuleUI module in root.GetComponentsInChildren<ModuleUI>(true))
                {
                    if (module.gameObject.name == "PF_Module00_UI")
                        return module;
                }
            }
            throw new System.InvalidOperationException(scene.name + " has no PF_Module00_UI.");
        }

        private static GameObject FindByName(Scene scene, string name, Transform exclude)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == name && (exclude == null || !t.IsChildOf(exclude)))
                        return t.gameObject;
                }
            }
            throw new System.InvalidOperationException(scene.name + " has no '" + name + "'.");
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            // De primer nivel, también dentro de las carpetas del Hierarchy.
            foreach (GameObject root in SceneFolder.TopLevelObjects(scene))
            {
                if (root.name == name)
                    return root;
            }
            return null;
        }
    }
}
