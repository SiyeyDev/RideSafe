using System.Collections.Generic;
using System.Reflection;
using System.Text;
using RideSafe.Demo;
using RideSafe.TaskSequence;
using RideSafe.Tutorial;
using RideSafe.Tutorial.AutoHand;
using RideSafe.Tutorial.Editor;
using RideSafe.Tutorial.Presentation;
using RideSafe.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RideSafe.Module00.Editor
{
    /// <summary>
    /// Wires the 3D Touch Plus hint, the Point/Select sequence and the vehicle preview into
    /// RideSafe_Garaje_Test only. Production scenes are never opened. Idempotent: running it
    /// again finds what it created and re-applies the same references.
    /// <para>
    /// The test scene is opened additively and closed again, so whatever scene the user has
    /// open (and its unsaved changes) is left alone.
    /// </para>
    /// </summary>
    public static class Module00TestSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scene/RideSafe_Garaje_Test.unity";
        private const string GateSequencePath = "Assets/_Project/Tutorial/SO_Tutorial_InteractionGate.asset";
        private const string PointSelectSequencePath = "Assets/_Project/Tutorial/SO_Tutorial_PointSelect.asset";
        private const string OnboardingSequencePath = "Assets/_Project/Tutorial/SO_Module00_Onboarding.asset";
        private const string OnboardingTestSequencePath = "Assets/_Project/Tutorial/SO_Module00_Onboarding_Test.asset";
        private const string ExampleRigName = "XRPlayer (AutoHand example, unused)";
        private const string AtlasA = "Assets/_Project/UI/RideSafe_UI_Atlas_A_4096.png";
        private const string AtlasB = "Assets/_Project/UI/RideSafe_UI_Atlas_B_4096.png";
        private const string FocusRingName = "Tutorial Focus Ring";

        /// <summary>PF_Module00_UI panels the onboarding test sequence guides step by step.</summary>
        public static readonly string[] GuidedPanels =
        {
            Module00TutorialBinding.LanguagePanel, Module00TutorialBinding.LocationPanel, Module00TutorialBinding.VehiclePanel
        };

        // RideSafe UI system colours (UIBuilderKit): H1/H2 text and the focus/hover mint.
        private static readonly Color BeamNavy = new Color32(0x00, 0x25, 0x54, 0xFF);
        private static readonly Color Mint = new Color32(0xA9, 0xE2, 0xD4, 0xFF);

        private const string Hint3DName = "Tutorial Controller Hint 3D";
        private const string PreviewName = "Module00 Vehicle Preview";

        private static readonly (string Id, string Model)[] Vehicles =
        {
            ("ebike", "ebike"),
            ("escooter", "scooter")
        };

        [MenuItem("RideSafe/Módulo 0/Montar escena de prueba (Garaje_Test)")]
        public static void Build()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                StringBuilder report = new StringBuilder("[Module00] RideSafe_Garaje_Test wiring:\n");
                GameObject tutorial = RequireRoot(scene, "Tutorial");

                ControllerHint3DView hint3D = EnsureHint3D(scene, tutorial.transform, report);
                WirePresenter(tutorial, hint3D, report);
                UseTestSequences(tutorial, report);
                EnsureVehiclePreview(scene, tutorial, report);
                ComposeGuidedScreens(tutorial, report);
                EnsureDesktopSimulator(scene, report);
                ApplyDesktopMode(scene, tutorial, report);
                DisableLegacyModule00UI(scene, report);

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

        private static ControllerHint3DView EnsureHint3D(Scene scene, Transform tutorial, StringBuilder report)
        {
            ControllerHint3DView view = tutorial.GetComponentInChildren<ControllerHint3DView>(true);
            if (view == null)
            {
                GameObject root = CreateIn(scene, Hint3DName, tutorial);
                view = root.AddComponent<ControllerHint3DView>();
                report.AppendLine("  created " + Hint3DName);
            }

            ControllerModelVisual left = EnsureModel(view.transform, TouchPlusPrefabBuilder.LeftPrefabPath);
            ControllerModelVisual right = EnsureModel(view.transform, TouchPlusPrefabBuilder.RightPrefabPath);

            SerializedObject serialized = new SerializedObject(view);
            serialized.FindProperty("_left").objectReferenceValue = left;
            serialized.FindProperty("_right").objectReferenceValue = right;
            // Didactic size and a comfortable spot below and beside the line of sight.
            serialized.FindProperty("_distance").floatValue = 0.6f;
            serialized.FindProperty("_offset").vector2Value = new Vector2(0.2f, -0.27f);
            serialized.FindProperty("_maxYawFromGaze").floatValue = 15f;
            serialized.FindProperty("_scale").floatValue = 2.2f;
            serialized.FindProperty("_maxDemonstrations").intValue = 3;
            // Focus / "use this" semantics of the UI system; coral stays for selection and decisions.
            serialized.FindProperty("_highlightColor").colorValue = Mint;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EnsureBackdrop(view.transform, report);
            view.gameObject.SetActive(false);
            report.AppendLine("  3D hint: left=" + left.name + " right=" + right.name);
            return view;
        }

        /// <summary>
        /// A dark rounded card behind the controller (Atlas A Panel_Dark_Base), so a white
        /// controller stays readable against the white garage. 9-slice, drawn Sliced. Part of
        /// the hint: shown with it.
        /// </summary>
        private static void EnsureBackdrop(Transform view, StringBuilder report)
        {
            Sprite panel = AtlasSprite(AtlasA, "Panel_Dark_Base");
            if (panel == null)
            {
                report.AppendLine("  (no Panel_Dark_Base sprite: hint without backdrop)");
                return;
            }

            Transform backdrop = view.Find("Backdrop");
            if (backdrop == null)
            {
                backdrop = new GameObject("Backdrop").transform;
                backdrop.SetParent(view, false);
            }
            SpriteRenderer renderer = GetOrAdd<SpriteRenderer>(backdrop);
            renderer.sprite = panel;
            renderer.color = new Color(1f, 1f, 1f, 0.9f);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // Behind the model (the view faces the learner along +Z), 17 x 15 cm before the view's
            // scale, as before; now Sliced with a uniform scale so the corners stay round.
            backdrop.localPosition = new Vector3(0f, 0f, -0.07f);
            backdrop.localRotation = Quaternion.Euler(0f, 180f, 0f);
            SetSliced(renderer, backdrop, new Vector2(0.17f, 0.15f), 0.02f);
        }

        private static ControllerModelVisual EnsureModel(Transform parent, string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new System.IO.FileNotFoundException("Build the Touch Plus prefabs first (RideSafe/Tutorial).", prefabPath);

            foreach (Transform child in parent)
            {
                if (PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) == prefab)
                    return child.GetComponent<ControllerModelVisual>();
            }
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.SetActive(false);
            return instance.GetComponent<ControllerModelVisual>();
        }

        private static void WirePresenter(GameObject tutorial, ControllerHint3DView hint3D, StringBuilder report)
        {
            TutorialPresenter presenter = Require<TutorialPresenter>(tutorial);
            ControllerHintView hint2D = Require<ControllerHintView>(tutorial);
            AutoHandTutorialInputService input = Require<AutoHandTutorialInputService>(tutorial);

            SerializedObject serialized = new SerializedObject(presenter);
            serialized.FindProperty("_input").objectReferenceValue = input;
            serialized.FindProperty("_controllerHint").objectReferenceValue = hint3D;
            serialized.FindProperty("_fallbackControllerHint").objectReferenceValue = hint2D;
            serialized.FindProperty("_instructionAboveTarget").boolValue = true;
            serialized.FindProperty("_ringReactsToFocus").boolValue = true;
            serialized.FindProperty("_instructionReferenceDistance").floatValue = 1.1f;
            serialized.FindProperty("_instructionGap").floatValue = 0.16f;
            serialized.FindProperty("_instructionBelowUnderEyeBy").floatValue = 0.2f;
            serialized.FindProperty("_boundsFromVisibleGraphics").boolValue = true;
            ConfigureProgressiveHelp(serialized.FindProperty("_help"));
            Object label = serialized.FindProperty("_instructionLabel").objectReferenceValue;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            ApplyBackplate(label as TMP_Text, report);
            EnsureFocusRing(tutorial.scene, tutorial, presenter, report);

            // The label was authored inside Onboarding, which stays hidden during Point/Select.
            Transform labelTransform = label is Component component ? component.transform : null;
            if (labelTransform != null && labelTransform.parent != tutorial.transform)
            {
                labelTransform.SetParent(tutorial.transform, true);
                report.AppendLine("  instruction label moved under Tutorial (was inside a hidden parent)");
            }
            report.AppendLine("  presenter: hint=3D, fallback=2D, input=" + input.name);
        }

        /// <summary>
        /// Test data only: the silent gate becomes Point -> Select with instructions, and the
        /// onboarding uses its Test copy (bilingual Language, instruction on every step). The
        /// production assets are not touched.
        /// </summary>
        private static void UseTestSequences(GameObject tutorial, StringBuilder report)
        {
            Swap(tutorial, GateSequencePath, PointSelectSequencePath, report);
            Swap(tutorial, OnboardingSequencePath, OnboardingTestSequencePath, report);
            KeepHighlightOnPointSelect(report);
            DropOnboardingInstructions(report);
            PutLocationFirst(tutorial, report);
        }

        /// <summary>
        /// Approved manual order (01A location, 01B language, 01C vehicle): the binding opens
        /// Location first and the Test sequence follows it. The pointing step moves to the
        /// first screen's options; every other step keeps its entity, validator and hint.
        /// </summary>
        public static readonly string[] OnboardingOrder =
        {
            "location.point", "location.choose", "location.continue",
            "language.choose", "language.continue", "vehicle.choose", "vehicle.confirm"
        };

        private static void PutLocationFirst(GameObject tutorial, StringBuilder report)
        {
            SerializedObject binding = new SerializedObject(Require<Module00TutorialBinding>(tutorial));
            binding.FindProperty("_locationFirst").boolValue = true;
            binding.ApplyModifiedPropertiesWithoutUndo();

            TaskSequenceSO sequence = AssetDatabase.LoadAssetAtPath<TaskSequenceSO>(OnboardingTestSequencePath);
            FieldInfo stepsField = typeof(TaskSequenceSO).GetField("_steps", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo idField = typeof(TaskStepData).GetField("_stepId", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo entityField = typeof(TaskStepData).GetField("_entityId", BindingFlags.Instance | BindingFlags.NonPublic);
            List<TaskStepData> steps = (List<TaskStepData>)stepsField.GetValue(sequence);

            bool changed = false;
            foreach (TaskStepData step in steps)
            {
                if (step.StepId != "language.point")
                    continue;
                idField.SetValue(step, "location.point");
                entityField.SetValue(step, new RideSafe.TaskSequence.EntityId("onboarding.location.options"));
                changed = true;
            }

            List<TaskStepData> ordered = new List<TaskStepData>();
            foreach (string id in OnboardingOrder)
            {
                TaskStepData step = steps.Find(candidate => candidate.StepId == id);
                if (step == null)
                    throw new System.InvalidOperationException(sequence.name + " has no step " + id);
                ordered.Add(step);
            }
            if (ordered.Count != steps.Count)
                throw new System.InvalidOperationException(sequence.name + " has steps outside the onboarding order.");

            for (int i = 0; i < steps.Count; i++)
                changed |= steps[i] != ordered[i];
            if (changed)
            {
                steps.Clear();
                steps.AddRange(ordered);
                EditorUtility.SetDirty(sequence);
                AssetDatabase.SaveAssetIfDirty(sequence);
            }
            report.AppendLine("  onboarding order: Location -> Language -> Vehicle (" + string.Join(", ", OnboardingOrder) + ")");
        }

        /// <summary>
        /// Every onboarding step is a normal UI step: the screen (title, cards, button) already
        /// says what to do, and the trigger reminder is the 3D controller, not text. The steps
        /// stay to drive the flow (focus, selection, confirmation, progress); they just show
        /// no instruction. Only the gate, which teaches a new interaction, keeps its text.
        /// </summary>
        private static void DropOnboardingInstructions(StringBuilder report)
        {
            TaskSequenceSO sequence = AssetDatabase.LoadAssetAtPath<TaskSequenceSO>(OnboardingTestSequencePath);
            FieldInfo key = typeof(TaskStepData).GetField("_instructionKey", BindingFlags.Instance | BindingFlags.NonPublic);
            bool changed = false;
            foreach (TaskStepData step in sequence.Steps)
            {
                if (string.IsNullOrEmpty((string)key.GetValue(step)))
                    continue;
                key.SetValue(step, string.Empty);
                changed = true;
            }
            if (changed)
            {
                EditorUtility.SetDirty(sequence);
                AssetDatabase.SaveAssetIfDirty(sequence);
            }
            report.AppendLine("  " + sequence.name + ": no instruction text on any step (the screen carries it)");
        }

        /// <summary>
        /// The Point/Select target is a neutral toggle, which the presenter's Auto rule would
        /// treat as UI with its own states. There the learner is still learning what pointing
        /// means, so both steps ask for the highlight explicitly.
        /// </summary>
        private static void KeepHighlightOnPointSelect(StringBuilder report)
        {
            TaskSequenceSO sequence = AssetDatabase.LoadAssetAtPath<TaskSequenceSO>(PointSelectSequencePath);
            FieldInfo mode = typeof(TaskStepData).GetField("_targetHighlight", BindingFlags.Instance | BindingFlags.NonPublic);
            bool changed = false;
            foreach (TaskStepData step in sequence.Steps)
            {
                if ((TargetHighlightMode)mode.GetValue(step) == TargetHighlightMode.Show)
                    continue;
                mode.SetValue(step, TargetHighlightMode.Show);
                changed = true;
            }
            if (changed)
            {
                EditorUtility.SetDirty(sequence);
                AssetDatabase.SaveAssetIfDirty(sequence);
            }
            report.AppendLine("  " + sequence.name + ": target highlight Show on every step");
        }

        private static void Swap(GameObject tutorial, string productionPath, string testPath, StringBuilder report)
        {
            TaskSequenceSO production = AssetDatabase.LoadAssetAtPath<TaskSequenceSO>(productionPath);
            TaskSequenceSO test = AssetDatabase.LoadAssetAtPath<TaskSequenceSO>(testPath);
            if (test == null)
                throw new System.IO.FileNotFoundException("Test sequence missing", testPath);

            TutorialSession session = Require<TutorialSession>(tutorial);
            SerializedObject serialized = new SerializedObject(session);
            SerializedProperty stages = serialized.FindProperty("_stages");
            for (int i = 0; i < stages.arraySize; i++)
            {
                SerializedProperty sequence = stages.GetArrayElementAtIndex(i).FindPropertyRelative("Sequence");
                if (sequence.objectReferenceValue == production || sequence.objectReferenceValue == test)
                {
                    sequence.objectReferenceValue = test;
                    report.AppendLine("  stage " + i + ": " + test.name);
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Teach (first time): everything at once. Reinforce (second): text and controller at
        /// once. Light: text and where at once, controller after 6 s. Known: text at once, where
        /// after 3 s, controller after 10 s. Point steps: text and where at once, no controller.
        /// </summary>
        private static void ConfigureProgressiveHelp(SerializedProperty help)
        {
            help.FindPropertyRelative("_reinforceBelow").intValue = 2;
            help.FindPropertyRelative("_lightBelow").intValue = 4;
            SetTiming(help, "_locate", 0f, -1f);
            SetTiming(help, "_teach", 0f, 0f);
            SetTiming(help, "_reinforce", 0f, 0f);
            SetTiming(help, "_light", 0f, 6f);
            SetTiming(help, "_known", 3f, 10f);
        }

        private static void SetTiming(SerializedProperty help, string stage, float highlight, float controllerHint)
        {
            SerializedProperty timing = help.FindPropertyRelative(stage);
            timing.FindPropertyRelative("Highlight").floatValue = highlight;
            timing.FindPropertyRelative("ControllerHint").floatValue = controllerHint;
        }

        private static void EnsureVehiclePreview(Scene scene, GameObject tutorial, StringBuilder report)
        {
            GameObject root = FindRoot(scene, PreviewName) ?? CreateIn(scene, PreviewName, null);
            VehiclePreviewController preview = GetOrAdd<VehiclePreviewController>(root);
            VehiclePanelPreviewBinding binding = GetOrAdd<VehiclePanelPreviewBinding>(root);

            List<GameObject> platforms = new List<GameObject>();
            foreach (GameObject candidate in scene.GetRootGameObjects())
            {
                if (candidate.name.StartsWith("plataform"))
                    platforms.Add(candidate);
            }

            SerializedObject serializedPreview = new SerializedObject(preview);
            SerializedProperty vehicles = serializedPreview.FindProperty("_vehicles");
            vehicles.arraySize = Vehicles.Length;
            for (int i = 0; i < Vehicles.Length; i++)
            {
                GameObject model = RequireRoot(scene, Vehicles[i].Model);
                GameObject platform = Nearest(platforms, model.transform.position);
                platforms.Remove(platform);

                SerializedProperty entry = vehicles.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_id").stringValue = Vehicles[i].Id;
                entry.FindPropertyRelative("_model").objectReferenceValue = model.transform;
                entry.FindPropertyRelative("_platform").objectReferenceValue = platform;
                report.AppendLine("  vehicle " + Vehicles[i].Id + ": " + model.name + " on " +
                                  (platform != null ? platform.name : "(no platform)"));
            }
            serializedPreview.ApplyModifiedPropertiesWithoutUndo();

            Transform onboarding = tutorial.transform.Find("Onboarding");
            ModuleUI module = onboarding != null ? onboarding.GetComponentInChildren<ModuleUI>(true) : null;
            if (module == null)
                throw new System.InvalidOperationException("Tutorial/Onboarding has no PF_Module00_UI (ModuleUI).");

            SerializedObject serializedBinding = new SerializedObject(binding);
            serializedBinding.FindProperty("_module").objectReferenceValue = module;
            serializedBinding.FindProperty("_preview").objectReferenceValue = preview;
            serializedBinding.ApplyModifiedPropertiesWithoutUndo();
            report.AppendLine("  preview bound to " + module.name + "/Vehicle");
        }

        /// <summary>
        /// The screen says WHAT to do; the tutorial only shows HOW, when a new interaction is
        /// taught. So the guided onboarding panels keep their own Header, and Language (seen
        /// before a language exists) gets the approved composition as Test-only overrides on
        /// the scene instance, re-applied on every build: bilingual title, no English-only
        /// subtitle, cards with tag + autonym + description in their own language. Texts that
        /// would need new I2 terms (Location/Vehicle copy, "Confirm ..." labels) stay as they
        /// are until promotion.
        /// </summary>
        private static void ComposeGuidedScreens(GameObject tutorial, StringBuilder report)
        {
            ModuleUI module = tutorial.transform.Find("Onboarding").GetComponentInChildren<ModuleUI>(true);
            foreach (string panel in GuidedPanels)
                RequireChild(module.Get<RectTransform>(panel), "Header").gameObject.SetActive(true);

            Transform header = RequireChild(module.Get<RectTransform>(Module00TutorialBinding.LanguagePanel), "Header");
            TMP_Text title = RequireChild(header, "Title").GetComponent<TMP_Text>();
            RemoveLocalize(title.gameObject);
            title.text = LanguageTitle;
            Record(title);
            RequireChild(header, "Body").gameObject.SetActive(false);

            Transform options = RequireChild(module.Get<RectTransform>(Module00TutorialBinding.LanguagePanel), "Options");
            ComposeLanguageCard(RequireChild(options, "Option_en"), "EN", "Captions and narration in English.");
            ComposeLanguageCard(RequireChild(options, "Option_es"), "ES", "Subtítulos y narración en español.");
            report.AppendLine("  guided panels keep their Header; Language: bilingual title, EN/ES cards with description");
            ApplyTypography(module, report);
        }

        public const string TitleFont = "Manrope-Bold SDF";
        public const string NameFont = "Manrope-SemiBold SDF";
        public const string TextFont = "Inter-Regular SDF";
        public const string ActionFont = "Inter-SemiBold SDF";
        /// <summary>Approved H1 range is 34-40 px at 2 m; the top of the builder's 34 so the title dominates.</summary>
        public const float ScreenTitleSize = 38f;
        private const string FontsFolder = "Assets/_Project/UI/Fonts/";

        /// <summary>
        /// The approved RideSafe VR type scale (storyboard "Mini design system", same as
        /// UIBuilderKit): H1 screen title Manrope Bold 700; H2 card name Manrope SemiBold 600;
        /// subtitle, description and secondary text Inter Regular; tags, captions, prompts and
        /// button labels Inter SemiBold 600. Null leaves a text as built.
        /// </summary>
        public static string FontFor(TMP_Text label)
        {
            switch (label.name)
            {
                case "Title":
                    return IsScreenTitle(label) ? TitleFont : NameFont;
                case "Tag":
                case "StateCaption":
                case "Label":
                case "Prompt":
                    return ActionFont;
                case "Body":
                case "Description":
                case "SelectedText":
                    return TextFont;
                default:
                    return null;
            }
        }

        public static bool IsScreenTitle(TMP_Text label) =>
            label.name == "Title" && label.transform.parent != null && label.transform.parent.name == "Header";

        private static void ApplyTypography(ModuleUI module, StringBuilder report)
        {
            int count = 0;
            foreach (string panel in GuidedPanels)
            {
                foreach (TMP_Text label in module.Get<RectTransform>(panel).GetComponentsInChildren<TMP_Text>(true))
                {
                    string fontName = FontFor(label);
                    if (fontName == null)
                        continue;
                    TMP_FontAsset font = RequireFont(fontName);
                    if (label.font != font)
                    {
                        label.font = font;
                        label.fontSharedMaterial = font.material;
                    }
                    if (IsScreenTitle(label))
                        label.fontSize = ScreenTitleSize;
                    Record(label);
                    count++;
                }
            }
            report.AppendLine("  typography: " + count + " texts on the approved scale (Manrope H1/H2, Inter body and actions), titles " +
                              ScreenTitleSize + " pt");
        }

        private static TMP_FontAsset RequireFont(string name)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontsFolder + name + ".asset");
            if (font == null)
                throw new System.IO.FileNotFoundException("Font asset missing", FontsFolder + name + ".asset");
            return font;
        }

        public const string LanguageTitle = "Choose your language / Elige tu idioma";

        /// <summary>
        /// Tag, name and description, as the approved Language card. Children are anchored to
        /// the card's top-left, so each is placed absolutely (frame px from the card's visible
        /// top-left): re-running the build gives the same layout.
        /// </summary>
        private static void ComposeLanguageCard(Transform card, string tag, string description)
        {
            RectTransform title = (RectTransform)RequireChild(card, "Title");
            RectTransform caption = (RectTransform)RequireChild(card, "StateCaption");
            RectTransform text = (RectTransform)RequireChild(card, "Description");
            Transform existing = card.Find("Tag");
            RectTransform tagRect = existing != null ? (RectTransform)existing
                : (RectTransform)Object.Instantiate(caption.gameObject, card).transform;
            tagRect.name = "Tag";
            tagRect.gameObject.SetActive(true);
            tagRect.GetComponent<TMP_Text>().text = tag;
            text.GetComponent<TMP_Text>().text = description;
            title.GetComponent<TMP_Text>().fontSize = 22f;

            PlaceInCard(tagRect, 28f, 22f, 90f, 40f);
            PlaceInCard(caption, 100f, 22f, 240f, 40f);
            PlaceInCard(title, 28f, 52f, 280f, 84f);
            PlaceInCard(text, 28f, 90f, 276f, 140f);
            foreach (Component changed in new Component[] { title, title.GetComponent<TMP_Text>(), caption, text,
                                                           text.GetComponent<TMP_Text>(), tagRect, tagRect.GetComponent<TMP_Text>() })
                Record(changed);
        }

        /// <summary>Card slice padding: UIBuilderKit.PadFor(2.8) = 16 / 2.8 frame px around the visible card.</summary>
        private const float CardPad = 16f / 2.8f;

        /// <summary>Same convention as UIElements.PlaceInCard: frame px from the card's visible top-left.</summary>
        private static void PlaceInCard(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(x1 - x0, y1 - y0);
            rect.anchoredPosition = new Vector2(CardPad + x0, -(CardPad + y0));
        }

        /// <summary>Script edits on a prefab instance only survive the save once recorded as overrides.</summary>
        private static void Record(Component component)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(component))
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }

        /// <summary>Removes I2's Localize from a static text, so a Test-only text is not overwritten at runtime.</summary>
        private static void RemoveLocalize(GameObject text)
        {
            foreach (Component component in text.GetComponents<Component>())
            {
                if (component != null && component.GetType().Name == "Localize")
                    Object.DestroyImmediate(component);
            }
        }

        private static Transform RequireChild(Transform parent, string name)
        {
            Transform child = parent != null ? parent.Find(name) : null;
            if (child == null)
                throw new System.InvalidOperationException((parent != null ? parent.name : "?") + " has no " + name + ".");
            return child;
        }

        /// <summary>
        /// The instruction uses RideSafe's own instruction panel (Atlas A Panel_Instruction_Wide,
        /// 9-slice) with H2 BeamNavy text, refitted to the text by TextBackplate.
        /// </summary>
        private static void ApplyBackplate(TMP_Text label, StringBuilder report)
        {
            if (label == null)
                return;
            if (label.font != null && label.fontSharedMaterial != label.font.material)
                label.fontSharedMaterial = label.font.material; // back from the earlier outline test material

            Sprite panel = AtlasSprite(AtlasA, "Panel_Instruction_Wide");
            label.color = BeamNavy;
            Transform plateTransform = label.transform.Find("Backplate");
            if (plateTransform == null)
            {
                plateTransform = new GameObject("Backplate").transform;
                plateTransform.SetParent(label.transform, false);
            }
            SpriteRenderer plate = GetOrAdd<SpriteRenderer>(plateTransform);
            plate.sprite = panel;
            plate.color = Color.white;
            plate.drawMode = SpriteDrawMode.Sliced;
            plate.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            plate.sortingOrder = -1;

            TextBackplate backplate = GetOrAdd<TextBackplate>(label);
            SerializedObject serialized = new SerializedObject(backplate);
            serialized.FindProperty("_plate").objectReferenceValue = plate;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(label);
            report.AppendLine("  instruction label: Panel_Instruction_Wide (Sliced), BeamNavy text");
        }

        /// <summary>
        /// The target mark is RideSafe's Focus_Ring_Mint (Atlas B, 9-slice, also used by
        /// PF_Module02_UI), drawn by a SpriteTargetHighlight; the presenter still decides when.
        /// The old line ring stays in the scene, unused.
        /// </summary>
        private static void EnsureFocusRing(Scene scene, GameObject tutorial, TutorialPresenter presenter, StringBuilder report)
        {
            Sprite ring = AtlasSprite(AtlasB, "Focus_Ring_Mint");
            if (ring == null)
                throw new System.InvalidOperationException("Atlas B has no Focus_Ring_Mint sprite.");

            Transform focus = tutorial.transform.Find(FocusRingName);
            if (focus == null)
                focus = CreateIn(scene, FocusRingName, tutorial.transform).transform;
            SpriteRenderer renderer = GetOrAdd<SpriteRenderer>(focus);
            renderer.sprite = ring;
            renderer.color = Color.white;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            SpriteTargetHighlight highlight = GetOrAdd<SpriteTargetHighlight>(focus);
            focus.gameObject.SetActive(false);

            SerializedObject serialized = new SerializedObject(presenter);
            serialized.FindProperty("_targetHighlight").objectReferenceValue = highlight;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            report.AppendLine("  target highlight: Focus_Ring_Mint (Sliced)");
        }

        /// <summary>A 9-slice sprite drawn Sliced: uniform scale for the corner size, then sized.</summary>
        private static void SetSliced(SpriteRenderer renderer, Transform transform, Vector2 size, float cornerSize)
        {
            Sprite sprite = renderer.sprite;
            float borderUnits = Mathf.Max(sprite.border.x, sprite.border.y) / sprite.pixelsPerUnit;
            float scale = borderUnits > 0f ? cornerSize / borderUnits : 1f;
            renderer.drawMode = SpriteDrawMode.Sliced;
            transform.localScale = Vector3.one * scale;
            renderer.size = size / scale;
        }

        private static Sprite AtlasSprite(string atlas, string name)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(atlas))
            {
                if (asset is Sprite sprite && sprite.name == name)
                    return sprite;
            }
            return null;
        }

        /// <summary>The existing DesktopMode gets the one missing piece: simulated tracked controllers.</summary>
        private static void EnsureDesktopSimulator(Scene scene, StringBuilder report)
        {
            GameObject desktop = RequireRoot(scene, "DesktopMode");
            Camera camera = desktop.GetComponentInChildren<Camera>(true);
            if (camera == null)
                throw new System.InvalidOperationException("DesktopMode needs its Camera.");

            DesktopTutorialInputSimulator simulator = GetOrAdd<DesktopTutorialInputSimulator>(desktop);
            SerializedObject serialized = new SerializedObject(simulator);
            serialized.FindProperty("_desktopCamera").objectReferenceValue = camera;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            report.AppendLine("  DesktopMode: input simulator on " + camera.name);
        }

        /// <summary>
        /// Saves the test scene in the project's own PC mode, exactly what RideSafe/Demo/Modo PC
        /// does (XRPlayer off, DesktopMode on); Modo VR switches it back for the headset. The
        /// unused AutoHand example rig, also named "XRPlayer", is renamed so that menu always
        /// finds the real one.
        /// </summary>
        private static void ApplyDesktopMode(Scene scene, GameObject tutorial, StringBuilder report)
        {
            Transform head = new SerializedObject(Require<TutorialPresenter>(tutorial)).FindProperty("_viewer").objectReferenceValue as Transform;
            GameObject rig = head != null ? head.root.gameObject : null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "XRPlayer" && root != rig)
                {
                    root.name = ExampleRigName;
                    report.AppendLine("  renamed the unused AutoHand example rig to '" + ExampleRigName + "'");
                }
            }

            if (rig != null)
                rig.SetActive(false);
            RequireRoot(scene, "DesktopMode").SetActive(true);
            report.AppendLine("  saved in PC mode (switch with RideSafe/Demo/Modo VR for the headset)");
        }

        /// <summary>
        /// The scene also holds the older standalone PF_Module00_UI (narration/video path). It
        /// shows its own Welcome/Language in front of the learner and competes with the tutorial's
        /// onboarding, so it is turned off in the test scene.
        /// </summary>
        private static void DisableLegacyModule00UI(Scene scene, StringBuilder report)
        {
            GameObject legacy = FindRoot(scene, "PF_Module00_UI");
            if (legacy != null && legacy.activeSelf)
            {
                legacy.SetActive(false);
                report.AppendLine("  legacy root PF_Module00_UI turned off");
            }
        }

        /// <summary>GetComponent or AddComponent. Not "??": a missing Unity component is a fake null that "??" lets through.</summary>
        private static T GetOrAdd<T>(Component owner) where T : Component => GetOrAdd<T>(owner.gameObject);

        private static T GetOrAdd<T>(GameObject owner) where T : Component
        {
            T component = owner.GetComponent<T>();
            return component != null ? component : owner.AddComponent<T>();
        }

        private static GameObject Nearest(List<GameObject> candidates, Vector3 position)
        {
            GameObject best = null;
            float bestDistance = float.MaxValue;
            foreach (GameObject candidate in candidates)
            {
                float distance = Vector3.Distance(candidate.transform.position, position);
                if (distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }
            return best != null && bestDistance < 1f ? best : null;
        }

        private static GameObject CreateIn(Scene scene, string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            if (parent != null)
                go.transform.SetParent(parent, false);
            return go;
        }

        private static T Require<T>(GameObject root) where T : Component
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component == null)
                throw new System.InvalidOperationException(root.name + " has no " + typeof(T).Name);
            return component;
        }

        private static GameObject RequireRoot(Scene scene, string name)
        {
            GameObject root = FindRoot(scene, name);
            if (root == null)
                throw new System.InvalidOperationException(scene.name + " has no root '" + name + "'.");
            return root;
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
