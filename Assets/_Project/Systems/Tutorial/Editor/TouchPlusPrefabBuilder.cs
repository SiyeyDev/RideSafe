using System.Collections.Generic;
using System.Text;
using RideSafe.Tutorial.Presentation;
using UnityEditor;
using UnityEngine;

namespace RideSafe.Tutorial.Editor
{
    /// <summary>
    /// Builds PF_TouchPlus_Left / PF_TouchPlus_Right from the Meta Touch Plus FBX files, whose
    /// buttons are separate meshes. Idempotent: rebuilding keeps the prefab GUIDs, so scenes
    /// that use them stay linked. The FBX files and their materials are never modified; each
    /// pressable part gets a "Glow" shell (its own mesh, the shared unlit highlight material).
    /// <para>
    /// Press directions are derived from the geometry (each part travels toward the body of
    /// the controller) and can be tuned per button in the prefab afterwards.
    /// </para>
    /// </summary>
    public static class TouchPlusPrefabBuilder
    {
        public const string OutputFolder = "Assets/_Project/Tutorial/Controllers";
        public const string LeftPrefabPath = OutputFolder + "/PF_TouchPlus_Left.prefab";
        public const string RightPrefabPath = OutputFolder + "/PF_TouchPlus_Right.prefab";

        private const string ModelFolder = "Assets/_Project/Models/OculusControls";
        private const string LeftModel = ModelFolder + "/L_Meta_Controller.fbx";
        private const string RightModel = ModelFolder + "/R_Meta_Controller.fbx";
        private const string GlowMaterialPath = "Assets/_Project/Tutorial/M_TutorialHighlight.mat";
        private const float GlowScale = 1.15f;

        /// <summary>(mesh suffix, control, travel in metres). Back = index trigger, Side = grip.</summary>
        private static readonly (string Suffix, ControllerControl Control, float Distance)[] CommonParts =
        {
            ("Back_Button_Mesh", ControllerControl.Trigger, 0.006f),
            ("Side_Button_Mesh", ControllerControl.Grip, 0.004f),
            ("Joystick_Mesh", ControllerControl.Thumbstick, 0.002f)
        };

        private static readonly (string Suffix, ControllerControl Control, float Distance)[] LeftParts =
        {
            ("X_Button_Mesh", ControllerControl.PrimaryButton, 0.002f),
            ("Y_Button_Mesh", ControllerControl.SecondaryButton, 0.002f),
            ("Meta_Button_Mesh", ControllerControl.Menu, 0.0015f)
        };

        private static readonly (string Suffix, ControllerControl Control, float Distance)[] RightParts =
        {
            ("A_Button_Mesh", ControllerControl.PrimaryButton, 0.002f),
            ("B_Button_Mesh", ControllerControl.SecondaryButton, 0.002f),
            ("Meta_Button_Mesh", ControllerControl.System, 0.0015f)
        };

        [MenuItem("RideSafe/Tutorial/Crear prefabs Touch Plus")]
        public static void Build()
        {
            EnsureFolder(OutputFolder);
            StringBuilder report = new StringBuilder("[Tutorial] Touch Plus prefabs:\n");
            BuildOne(LeftModel, LeftPrefabPath, XRNodeRole.LeftHand, "L_", LeftParts, report);
            BuildOne(RightModel, RightPrefabPath, XRNodeRole.RightHand, "R_", RightParts, report);
            AssetDatabase.SaveAssets();
            Debug.Log(report.ToString());
        }

        /// <summary>Rewrites only the view poses of the existing prefabs, keeping everything else (and every reference into them).</summary>
        [MenuItem("RideSafe/Tutorial/Recalcular posturas Touch Plus (en sitio)")]
        public static void RewritePoses()
        {
            StringBuilder report = new StringBuilder("[Tutorial] Touch Plus poses:\n");
            foreach (var (path, prefix) in new[] { (LeftPrefabPath, "L_"), (RightPrefabPath, "R_") })
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    ControllerModelVisual visual = root.GetComponent<ControllerModelVisual>();
                    visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    Transform body = FindDeep(root.transform, prefix + "Controller_Mesh");
                    Vector3 bodyCenter = body.GetComponent<Renderer>().bounds.center;
                    List<ControllerButtonVisual> buttons = new List<ControllerButtonVisual>(root.GetComponentsInChildren<ControllerButtonVisual>(true));
                    SerializedObject serialized = new SerializedObject(visual);
                    WritePoses(serialized, root.transform, bodyCenter, buttons, report);
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            Debug.Log(report.ToString());
        }

        private static void BuildOne(string modelPath, string prefabPath, XRNodeRole hand, string prefix,
            (string Suffix, ControllerControl Control, float Distance)[] sideParts,
            StringBuilder report)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
                throw new System.IO.FileNotFoundException("Touch Plus model not found", modelPath);

            GameObject root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(prefabPath));
            try
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                instance.name = "Model";

                Transform body = FindDeep(instance.transform, prefix + "Controller_Mesh");
                Vector3 bodyCenter = body != null && body.GetComponent<Renderer>() != null
                    ? body.GetComponent<Renderer>().bounds.center
                    : instance.transform.position;
                Bounds all = TotalBounds(instance);
                report.AppendLine("  " + root.name + ": size " + all.size.ToString("F3") + " m");

                List<ControllerButtonVisual> buttons = new List<ControllerButtonVisual>();
                foreach (var part in Concat(CommonParts, sideParts))
                {
                    Transform mesh = FindDeep(instance.transform, prefix + part.Suffix);
                    if (mesh == null)
                    {
                        report.AppendLine("  MISSING mesh " + prefix + part.Suffix);
                        continue;
                    }
                    buttons.Add(AddButton(mesh, part.Control, part.Distance, bodyCenter));
                    report.AppendLine("  " + part.Control + " -> " + mesh.name);
                }

                ControllerModelVisual visual = root.AddComponent<ControllerModelVisual>();
                SerializedObject serialized = new SerializedObject(visual);
                serialized.FindProperty("_hand").enumValueIndex = (int)hand;
                SerializedProperty list = serialized.FindProperty("_buttons");
                list.arraySize = buttons.Count;
                for (int i = 0; i < buttons.Count; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
                WritePoses(serialized, root.transform, bodyCenter, buttons, report);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static ControllerButtonVisual AddButton(Transform mesh, ControllerControl control, float distance, Vector3 bodyCenter)
        {
            Renderer renderer = mesh.GetComponent<Renderer>();
            Vector3 partCenter = renderer != null ? renderer.bounds.center : mesh.position;
            Vector3 towardBody = bodyCenter - partCenter;
            Vector3 direction = mesh.parent != null ? mesh.parent.InverseTransformDirection(towardBody) : towardBody;
            if (direction.sqrMagnitude < 1e-10f)
                direction = Vector3.down;

            ControllerButtonVisual button = mesh.gameObject.AddComponent<ControllerButtonVisual>();
            SerializedObject serialized = new SerializedObject(button);
            serialized.FindProperty("_control").enumValueIndex = (int)control;
            serialized.FindProperty("_target").objectReferenceValue = mesh;
            serialized.FindProperty("_pressDirection").vector3Value = direction.normalized;
            serialized.FindProperty("_pressDistance").floatValue = distance;
            serialized.FindProperty("_glow").objectReferenceValue = CreateGlow(mesh);
            SerializedProperty renderers = serialized.FindProperty("_renderers");
            renderers.arraySize = renderer != null ? 1 : 0;
            if (renderer != null)
                renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return button;
        }

        /// <summary>
        /// View poses derived from the geometry: each turns its control toward the viewer (+Z
        /// of the hint view) with the controller's tip up on screen, and centres the body on
        /// the view's origin so the model turns in place. Tunable in the prefab afterwards.
        /// </summary>
        private static void WritePoses(SerializedObject serialized, Transform root, Vector3 bodyCenterWorld,
            List<ControllerButtonVisual> buttons, StringBuilder report)
        {
            Vector3 center = root.InverseTransformPoint(bodyCenterWorld);
            Dictionary<ControllerControl, Vector3> direction = new Dictionary<ControllerControl, Vector3>();
            foreach (ControllerButtonVisual button in buttons)
            {
                Renderer renderer = button.GetComponent<Renderer>();
                Vector3 part = root.InverseTransformPoint(renderer != null ? renderer.bounds.center : button.transform.position);
                direction[button.Control] = (part - center).normalized;
            }

            Vector3 top = ((Dir(direction, ControllerControl.PrimaryButton) + Dir(direction, ControllerControl.SecondaryButton)) * 0.5f).normalized;
            Vector3 trigger = Dir(direction, ControllerControl.Trigger);
            Vector3 grip = Dir(direction, ControllerControl.Grip);
            Vector3 front = Vector3.ProjectOnPlane(trigger, top).normalized;
            Vector3 side = Vector3.ProjectOnPlane(grip, top).normalized;

            var poses = new List<(ControllerControl Control, Vector3 View, Vector3 Up)>
            {
                // As the learner holds it: leaning forward, seen from the grip side and a little from
                // behind and above, so the trigger shows in profile under the finger (PO, 2026-10-16).
                // The old pose stood it upright facing the viewer, like a product shot.
                (ControllerControl.Trigger, side * 0.8f + top * 0.35f - front * 0.25f, top * 0.6f + front * 0.6f),
                (ControllerControl.Grip, side + top * 0.35f, front),
                (ControllerControl.PrimaryButton, top + side * 0.25f, front),
                (ControllerControl.SecondaryButton, top + side * 0.25f, front),
                (ControllerControl.Thumbstick, top + side * 0.25f, front),
                (ControllerControl.Menu, top + side * 0.25f, front),
                (ControllerControl.System, top + side * 0.25f, front)
            };

            SerializedProperty list = serialized.FindProperty("_poses");
            list.arraySize = poses.Count;
            for (int i = 0; i < poses.Count; i++)
            {
                Quaternion rotation = ViewRotation(poses[i].View, poses[i].Up);
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("Control").enumValueIndex = (int)poses[i].Control;
                entry.FindPropertyRelative("Euler").vector3Value = rotation.eulerAngles;
                entry.FindPropertyRelative("Offset").vector3Value = -(rotation * center);
            }

            Quaternion fallback = ViewRotation(top + side * 0.5f + front * 0.2f, front);
            SerializedProperty defaultPose = serialized.FindProperty("_defaultPose");
            defaultPose.FindPropertyRelative("Control").enumValueIndex = (int)ControllerControl.None;
            defaultPose.FindPropertyRelative("Euler").vector3Value = fallback.eulerAngles;
            defaultPose.FindPropertyRelative("Offset").vector3Value = -(fallback * center);
            report.AppendLine("  poses: top=" + top.ToString("F2") + " front=" + front.ToString("F2") + " side=" + side.ToString("F2"));
        }

        /// <summary>Rotation that turns <paramref name="view"/> to +Z (toward the viewer) and <paramref name="up"/> to +Y.</summary>
        private static Quaternion ViewRotation(Vector3 view, Vector3 up)
        {
            view = view.normalized;
            if (Mathf.Abs(Vector3.Dot(view, up.normalized)) > 0.98f)
                up = Vector3.Cross(view, Vector3.right);
            return Quaternion.Inverse(Quaternion.LookRotation(view, up));
        }

        private static Vector3 Dir(Dictionary<ControllerControl, Vector3> directions, ControllerControl control)
        {
            Vector3 value;
            return directions.TryGetValue(control, out value) ? value : Vector3.up;
        }

        /// <summary>Child shell that reuses the part's mesh with the shared unlit highlight material.</summary>
        private static Renderer CreateGlow(Transform mesh)
        {
            MeshFilter source = mesh.GetComponent<MeshFilter>();
            Material material = AssetDatabase.LoadAssetAtPath<Material>(GlowMaterialPath);
            if (source == null || source.sharedMesh == null || material == null)
                return null;

            GameObject glow = new GameObject("Glow");
            glow.transform.SetParent(mesh, false);
            // Scale around the mesh's own centre, wherever its pivot is.
            Vector3 center = source.sharedMesh.bounds.center;
            glow.transform.localPosition = center * (1f - GlowScale);
            glow.transform.localScale = Vector3.one * GlowScale;
            glow.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
            MeshRenderer renderer = glow.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = false;
            return renderer;
        }

        private static IEnumerable<(string Suffix, ControllerControl Control, float Distance)> Concat(
            (string Suffix, ControllerControl Control, float Distance)[] a,
            (string Suffix, ControllerControl Control, float Distance)[] b)
        {
            foreach (var item in a) yield return item;
            foreach (var item in b) yield return item;
        }

        private static Bounds TotalBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(go.transform.position, Vector3.zero);
            foreach (Renderer renderer in renderers)
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == name)
                    return candidate;
            }
            return null;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;
            string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }
    }
}
