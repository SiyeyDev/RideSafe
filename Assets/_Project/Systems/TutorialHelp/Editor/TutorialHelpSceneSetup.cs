using System.Collections.Generic;
using System.Text;
using RideSafe.Tutorial;
using RideSafe.Tutorial.Editor;
using RideSafe.Tutorial.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RideSafe.Help.Editor
{
    /// <summary>
    /// Puts the agnostic tutorial into RideSafe_Garaje_v5: removes the old, inactive "Tutorial"
    /// root (its own copy of the module 0 screens) once nothing outside it references it, and
    /// adds the "TutorialHelp" root. Touches no module object. Idempotent.
    /// </summary>
    public static class TutorialHelpSceneSetup
    {
        public const string ScenePath = "Assets/_Project/Scene/RideSafe_Garaje_v5.unity";
        public const string RootName = "TutorialHelp";
        public const string OldRootName = "Tutorial";
        public const string GesturePath = "Assets/_Project/Systems/TutorialHelp/Data/SO_Gesture_Select.asset";
        public const string HaloMaterialPath = "Assets/_Project/Systems/TutorialHelp/Art/M_TutorialHalo.mat";
        private const string HaloShader = "RideSafe/Tutorial Halo";
        private const string AtlasA = "Assets/_Project/UI/RideSafe_UI_Atlas_A_4096.png";
        // Captions are Inter SemiBold in the approved type scale (storyboard «Mini design system»).
        private const string CaptionFont = "Assets/_Project/UI/Fonts/Inter-SemiBold SDF.asset";
        private const string HintName = "Gesture Hint";
        /// <summary>The hint card before the view's scale: the caption on top, the controller under it.</summary>
        private static readonly Vector2 CardSize = new Vector2(0.2f, 0.21f);
        private const float CaptionY = 0.062f;

        [MenuItem("RideSafe/Tutorial Help/Montar en RideSafe_Garaje_v5")]
        public static void Build()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                StringBuilder report = new StringBuilder("[TutorialHelp] RideSafe_Garaje_v5:\n");
                RemoveOldTutorial(scene, report);
                TutorialGestureSO gesture = EnsureGesture(report);
                Material halo = EnsureHaloMaterial(report);
                BuildRoot(scene, gesture, halo, report);
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

        private static void RemoveOldTutorial(Scene scene, StringBuilder report)
        {
            GameObject old = FindRoot(scene, OldRootName);
            if (old == null)
            {
                report.AppendLine("  old Tutorial: already gone");
                return;
            }
            List<string> users = new List<string>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == old)
                    continue;
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null)
                        continue;
                    SerializedProperty property = new SerializedObject(component).GetIterator();
                    while (property.NextVisible(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference)
                            continue;
                        Object value = property.objectReferenceValue;
                        Transform target = value is GameObject go ? go.transform : value is Component c ? c.transform : null;
                        if (target != null && target.IsChildOf(old.transform))
                            users.Add(component.GetType().Name + " on " + component.name + "." + property.propertyPath);
                    }
                }
            }
            if (users.Count > 0)
                throw new System.InvalidOperationException("The old Tutorial is still referenced: " + string.Join(", ", users));
            Object.DestroyImmediate(old);
            report.AppendLine("  old Tutorial removed (nothing referenced it)");
        }

        private static TutorialGestureSO EnsureGesture(StringBuilder report)
        {
            TutorialGestureSO gesture = AssetDatabase.LoadAssetAtPath<TutorialGestureSO>(GesturePath);
            if (gesture == null)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(GesturePath));
                gesture = ScriptableObject.CreateInstance<TutorialGestureSO>();
                AssetDatabase.CreateAsset(gesture, GesturePath);
            }
            gesture.Configure(ControllerControl.Trigger, TutorialGestureSO.HandChoice.Dominant,
                              HelpTextKeys.SelectInterface, HelpTextKeys.SelectObject);
            EditorUtility.SetDirty(gesture);
            AssetDatabase.SaveAssetIfDirty(gesture);
            report.AppendLine("  gesture: trigger, dominant hand");
            return gesture;
        }

        private static Material EnsureHaloMaterial(StringBuilder report)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(HaloMaterialPath);
            if (material != null)
                return material;
            Shader shader = Shader.Find(HaloShader);
            if (shader == null)
                throw new System.InvalidOperationException("Shader " + HaloShader + " is not imported.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, HaloMaterialPath);
            report.AppendLine("  halo material created");
            return material;
        }

        private static void BuildRoot(Scene scene, TutorialGestureSO gesture, Material halo, StringBuilder report)
        {
            GameObject root = FindRoot(scene, RootName);
            if (root == null)
            {
                root = new GameObject(RootName);
                SceneManager.MoveGameObjectToScene(root, scene);
            }
            root.SetActive(true);

            TargetHighlighter highlighter = GetOrAdd<TargetHighlighter>(root);
            highlighter.Configure(halo);
            EditorUtility.SetDirty(highlighter);

            Transform hint = root.transform.Find(HintName);
            if (hint == null)
            {
                hint = new GameObject(HintName).transform;
                hint.SetParent(root.transform, false);
            }
            GestureHintView view = GetOrAdd<GestureHintView>(hint.gameObject);
            ControllerModelVisual left = EnsureModel(hint, TouchPlusPrefabBuilder.LeftPrefabPath);
            ControllerModelVisual right = EnsureModel(hint, TouchPlusPrefabBuilder.RightPrefabPath);
            EnsureBackdrop(hint);
            TMP_Text caption = EnsureCaption(hint);
            view.Configure(left, right, caption);
            SerializedObject placement = new SerializedObject(view);
            // 2026-10-16 review: at 1.8 the card was 36 x 38 cm at 60 cm, a third of the view, and its
            // bottom at -38° fell off the PC screen. At 1.3 and 65 cm it spans about -7° to -26°: in
            // the comfortable band below the horizon, smaller than the panels it supports.
            placement.FindProperty("_scale").floatValue = 1.3f;
            placement.FindProperty("_distance").floatValue = 0.65f;
            // Centred below the gaze: module 1 keeps its navigation bottom-left and "Mi preparación" on the right.
            placement.FindProperty("_offset").vector2Value = new Vector2(0f, -0.2f);
            placement.FindProperty("_cardSize").vector2Value = CardSize;
            // The controller makes room for the caption without reaching the card's bottom edge.
            placement.FindProperty("_captionRoom").floatValue = 0.026f;
            placement.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
            hint.gameObject.SetActive(false);

            TutorialHelpManager manager = GetOrAdd<TutorialHelpManager>(root);
            SerializedObject serialized = new SerializedObject(manager);
            serialized.FindProperty("_selectGesture").objectReferenceValue = gesture;
            serialized.FindProperty("_hintView").objectReferenceValue = view;
            serialized.FindProperty("_highlighter").objectReferenceValue = highlighter;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            report.AppendLine("  TutorialHelp: manager, highlighter, hint (" + left.name + ", " + right.name + ")");
            EnsureOrientationPause(scene, root.transform, report);
        }

        private const string OrientationPauseName = "Pause: Module 1 Orientation";

        /// <summary>
        /// Module 1's orientation screen ("Comenzar") shows the EPP on the table, selectable but not
        /// yet expected; nothing generic tells it from a station. The pause lives under TutorialHelp
        /// and only watches the module's screen, which stays untouched.
        /// </summary>
        private static void EnsureOrientationPause(Scene scene, Transform root, StringBuilder report)
        {
            GameObject orientation = null;
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                foreach (Transform t in sceneRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Orientation" && t.parent != null && t.parent.name == "PF_Module01_UI")
                        orientation = t.gameObject;
                }
            }
            if (orientation == null)
                throw new System.InvalidOperationException("PF_Module01_UI/Orientation not found in " + scene.name);

            Transform holder = root.Find(OrientationPauseName);
            if (holder == null)
            {
                holder = new GameObject(OrientationPauseName).transform;
                holder.SetParent(root, false);
            }
            TutorialPause pause = GetOrAdd<TutorialPause>(holder.gameObject);
            pause.Watch(orientation);
            EditorUtility.SetDirty(pause);
            report.AppendLine("  pause while PF_Module01_UI/Orientation is open");
        }

        private static ControllerModelVisual EnsureModel(Transform parent, string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new System.IO.FileNotFoundException("Touch Plus prefab missing", prefabPath);
            foreach (Transform child in parent)
            {
                if (PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) == prefab)
                    return child.GetComponent<ControllerModelVisual>();
            }
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.SetActive(false);
            return instance.GetComponent<ControllerModelVisual>();
        }

        /// <summary>Dark rounded card (Atlas A Panel_Dark_Base, 9-slice) so the white controller reads against the white garage.</summary>
        private static void EnsureBackdrop(Transform hint)
        {
            Sprite panel = null;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(AtlasA))
            {
                if (asset is Sprite sprite && sprite.name == "Panel_Dark_Base")
                    panel = sprite;
            }
            if (panel == null)
                throw new System.InvalidOperationException("Atlas A has no Panel_Dark_Base.");
            Transform backdrop = hint.Find("Backdrop");
            if (backdrop == null)
            {
                backdrop = new GameObject("Backdrop").transform;
                backdrop.SetParent(hint, false);
            }
            SpriteRenderer renderer = GetOrAdd<SpriteRenderer>(backdrop.gameObject);
            renderer.sprite = panel;
            renderer.color = new Color(1f, 1f, 1f, 0.9f);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            backdrop.localPosition = new Vector3(0f, 0f, -0.07f);
            backdrop.localRotation = Quaternion.Euler(0f, 180f, 0f);
            float borderUnits = Mathf.Max(panel.border.x, panel.border.y) / panel.pixelsPerUnit;
            float scale = borderUnits > 0f ? 0.02f / borderUnits : 1f;
            renderer.drawMode = SpriteDrawMode.Sliced;
            backdrop.localScale = Vector3.one * scale;
            renderer.size = CardSize / scale;
        }

        internal static TMP_Text EnsureCaption(Transform hint)
        {
            Transform caption = hint.Find("Caption");
            if (caption == null)
            {
                caption = new GameObject("Caption", typeof(TextMeshPro)).transform;
                caption.SetParent(hint, false);
            }
            TextMeshPro text = caption.GetComponent<TextMeshPro>();
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CaptionFont);
            if (font != null)
            {
                text.font = font;
                text.fontSharedMaterial = font.material;
            }
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            // Two lines inside the top of the card, with room at both sides (PO, 2026-10-16): a fixed
            // size in a narrow box, so the sentence wraps instead of shrinking to one thin line (what
            // auto-size did) or rising above the card (what the old 4 cm box did).
            text.enableAutoSizing = false;
            text.fontSize = 0.11f;
            text.overflowMode = TextOverflowModes.Truncate;
            text.rectTransform.sizeDelta = new Vector2(0.13f, 0.06f);
            // A new RectTransform under a plain Transform saves m_AnchoredPosition, not localPosition: set both.
            text.rectTransform.anchoredPosition = new Vector2(0f, CaptionY);
            caption.localPosition = new Vector3(0f, CaptionY, 0f);
            caption.localRotation = Quaternion.Euler(0f, 180f, 0f); // read from the learner's side, like the backdrop
            caption.gameObject.SetActive(false);
            return text;
        }

        private static T GetOrAdd<T>(GameObject owner) where T : Component
        {
            T component = owner.GetComponent<T>();
            return component != null ? component : owner.AddComponent<T>();
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
