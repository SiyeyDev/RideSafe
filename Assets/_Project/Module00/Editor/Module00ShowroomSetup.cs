using System.Collections.Generic;
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
        // The learner starts at (0, 1.45, 9.3) and the closed garage door is at z = 6.62: 2.7 m.
        // Display pieces at 0.6 scale, 2.25 m away, fit side by side under their cards and turn
        // without crossing the door.
        private const float Distance = 2.25f;
        private const float DisplayScale = 0.6f;
        private const float GroundHeight = 0f;

        private static readonly (string Id, string Model, string Label)[] Vehicles =
        {
            ("ebike", "ebike", "Option_ebike"),
            ("escooter", "scooter", "Option_escooter")
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
            copy.transform.localScale = Vector3.one * DisplayScale;

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
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                    return root;
            }
            return null;
        }
    }
}
