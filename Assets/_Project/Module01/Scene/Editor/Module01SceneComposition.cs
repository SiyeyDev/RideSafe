using System.Linq;
using RideSafe.UI.EditorTools;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Composition of the module 1 scene panels, 2026-10-16 review:
    /// <list type="bullet">
    /// <item>the item's name was a floating title over the cabinet, apart from its question: it is
    /// now the header of the popup, which keeps its top edge so the pedestal stays clear;</item>
    /// <item>the station sign had 2.5 px above and below its text;</item>
    /// <item>the go/no-go box had 82 px of nothing under its buttons;</item>
    /// <item>the final buttons sat 15 px from one edge and 20 from the other: now centred, and free,
    /// with no box behind them.</item>
    /// </list>
    /// Edits the open v5 scene in place (the presentation builder replaces these objects
    /// wholesale and wires much more), and the builder calls it too, so both agree. Idempotent.
    /// </summary>
    public static class Module01SceneComposition
    {
        private const float PopupHeight = 232f; // 216 left the answers 21 units off the bottom edge (PO: too tight)
        private const float MarkerFontSize = 82f;

        [MenuItem("RideSafe/Módulo 1/Componer paneles de la escena (popup, cartel, decisión)")]
        public static void ApplyToOpenScene()
        {
            if (Application.isPlaying)
                throw new System.InvalidOperationException("Exit Play first.");
            UIBuilderKit kit = RideSafeUIPrefabBuilder.CreateKit();
            Compose(kit);
            if (kit.MissingSprites.Count > 0)
                throw new System.InvalidOperationException("Sprites not in the atlases: " + string.Join(", ", kit.MissingSprites));
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Module01] Scene panels composed in " + scene.path);
        }

        internal static void Compose(UIBuilderKit kit)
        {
            SyncPresentationCatalog();
            ComposePopup(kit, Find("ItemInspectorCanvas/Confirmation"), Find("ItemInspectorCanvas/ItemName"));

            RectTransform sign = Find("Module01Controls/StationInstruction");
            SetVisibleSize(sign, new Vector2(640f, 98f));
            RectTransform text = (RectTransform)sign.Find("Instruction");
            text.anchoredPosition = Vector2.zero;
            text.sizeDelta = new Vector2(592f, 80f);
            EditorUtility.SetDirty(text);

            ComposeDecision(kit, Find("Module01Controls/ReadinessDecision"));

            // The vehicle's '!' signs were in TMP's default font (AutoHand's AmmoText): brand font.
            foreach (InspectionMarker marker in Object.FindObjectsByType<InspectionMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                foreach (TMP_Text glyph in marker.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (glyph.font == kit.H1.Font && Mathf.Approximately(glyph.fontSize, MarkerFontSize))
                        continue;
                    glyph.font = kit.H1.Font;
                    // Manrope's '!' at 86 asks 117.5 of height; its box is 112 (AssertTextIsLegible).
                    glyph.fontSize = MarkerFontSize;
                    // A new font brings its own material: the dark outline that keeps the orange
                    // sign readable on the white ceiling has to be set again (Module01VehicleStaging).
                    glyph.outlineColor = new Color32(10, 20, 35, 255);
                    glyph.outlineWidth = .22f;
                    EditorUtility.SetDirty(glyph);
                }
            }

            // The final buttons stand free under «Mi Elección»'s box, with no box of their own (PO, 2026-10-16).
            RectTransform result = Find("Module01Controls/FinalResult");
            SetVisibleSize(result, new Vector2(660f, 84f));
            result.GetComponent<Image>().enabled = false;
            PlaceButton(Child(result, "ContinueModule2Button"), new Vector2(-148f, 0f), new Vector2(300f, 48f));
            PlaceButton(Child(result, "RepeatModuleButton"), new Vector2(158f, 0f), new Vector2(280f, 48f));
            RideSafe.UI.FollowBelow follow = result.GetComponent<RideSafe.UI.FollowBelow>();
            follow.Configure(Find("Comparison/Frame"), 16f);
            EditorUtility.SetDirty(follow);
            // Locked until the grade allows it, but it has to read as a real button that is not
            // available yet: the grey disabled pill vanished against the light garage floor.
            Button next = Child(result, "ContinueModule2Button").GetComponent<Button>();
            SpriteState states = next.spriteState;
            // Solid white pill (the disabled sprites are translucent and vanished too); the muted label
            // is what tells it apart from «Repetir», whose label stays navy.
            states.disabledSprite = kit.S("Button_Secondary_Default");
            next.spriteState = states;
            EditorUtility.SetDirty(next);
            var style = new SerializedObject(next.GetComponent<RideSafe.UI.SelectableStyle>());
            style.FindProperty("_disabledLabelColor").colorValue = UIBuilderKit.Hex("#8496AA");
            style.ApplyModifiedPropertiesWithoutUndo();

            WriteTranslations();
        }

        /// <summary>
        /// The catalog the presentation names items from. It was built before the trousers station
        /// existed, so «Revisa tus elecciones» showed «trousers_fitted» instead of its name.
        /// </summary>
        private static void SyncPresentationCatalog()
        {
            var all = AssetDatabase.LoadAssetAtPath<Module01CatalogSO>("Assets/_Project/Module01/Data/SO_Module01PresentationCatalog.asset");
            var catalog = AssetDatabase.LoadAssetAtPath<Module01CatalogSO>("Assets/_Project/Module01/Data/SO_Module01Catalog.asset");
            var vehicle = AssetDatabase.LoadAssetAtPath<VehicleProfileSO>("Assets/_Project/Module01/Data/SO_VehicleProfile_Ebike.asset");
            all.ConfigureForTests(catalog.Zones.Concat(vehicle.InspectionZones));
            EditorUtility.SetDirty(all);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Texts that hold an I2 key («M1/Yes») showed the key itself until Play translated them:
        /// in the editor the scene read «M1/...» everywhere. They now hold the Spanish text; the key
        /// stays on their Module01Text, which is what translates them at runtime.
        /// </summary>
        private static void WriteTranslations()
        {
            var asset = AssetDatabase.LoadAssetAtPath<I2.Loc.LanguageSourceAsset>("Assets/Resources/I2Languages.asset");
            var data = asset.SourceData;
            int es = data.GetLanguageIndexFromCode("es-CO");
            foreach (Module01Text localized in Object.FindObjectsByType<Module01Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string key = new SerializedObject(localized).FindProperty("_key").stringValue;
                var term = data.GetTermData(key);
                TMP_Text text = localized.GetComponent<TMP_Text>();
                if (term == null || es < 0 || text == null || string.IsNullOrEmpty(term.Languages[es]) || text.text == term.Languages[es])
                    continue;
                text.text = term.Languages[es];
                EditorUtility.SetDirty(text);
            }
        }

        /// <summary>
        /// The go/no-go, upright (PO, 2026-10-16): the question, a short hint, and the three answers
        /// stacked, each with its status glyph. The tints stay: the meeting asked for green, amber, red.
        /// </summary>
        private static void ComposeDecision(UIBuilderKit kit, RectTransform decision)
        {
            SetVisibleSize(decision, new Vector2(480f, 376f));
            RectTransform title = (RectTransform)decision.Find("Title");
            title.anchoredPosition = new Vector2(0f, 134f);
            title.sizeDelta = new Vector2(430f, 56f);
            TMP_Text titleText = title.GetComponent<TMP_Text>();
            titleText.fontSize = 26f;
            titleText.textWrappingMode = TextWrappingModes.Normal;
            EditorUtility.SetDirty(titleText);

            Transform existing = decision.Find("Hint");
            RectTransform hint = existing != null ? (RectTransform)existing : (RectTransform)new GameObject("Hint", typeof(RectTransform)).transform;
            hint.SetParent(decision, false);
            hint.gameObject.layer = decision.gameObject.layer;
            hint.anchorMin = hint.anchorMax = hint.pivot = new Vector2(0.5f, 0.5f);
            hint.anchoredPosition = new Vector2(0f, 89f);
            hint.sizeDelta = new Vector2(430f, 22f);
            kit.AddText(hint.gameObject, "M1/Decision_Hint", kit.Sub.With(16f), TextAlignmentOptions.Center);
            Module01Text localized = hint.GetComponent<Module01Text>();
            if (localized == null)
                localized = hint.gameObject.AddComponent<Module01Text>();
            localized.Configure("M1/Decision_Hint");
            EditorUtility.SetDirty(localized);

            var answers = new[] { ("RoadReadyButton", "Status_Independent", 26f),
                                  ("NeedsServiceButton", "Status_Attention", -52f),
                                  ("DoNotRideButton", "Status_Retry", -130f) };
            foreach (var (name, glyph, y) in answers)
            {
                RectTransform button = Child(decision, name);
                PlaceButton(button, new Vector2(0f, y), new Vector2(420f, 64f), 18f);
                TMP_Text label = button.Find("Label").GetComponent<TMP_Text>();
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.margin = new Vector4(72f, 0f, 16f, 0f);
                EditorUtility.SetDirty(label);
                Icon(kit, button, glyph);
            }
        }

        /// <summary>The status glyph at the start of an answer pill.</summary>
        private static void Icon(UIBuilderKit kit, RectTransform button, string sprite)
        {
            Transform existing = button.Find("Icon");
            RectTransform icon = existing != null ? (RectTransform)existing : (RectTransform)new GameObject("Icon", typeof(RectTransform)).transform;
            icon.SetParent(button, false);
            icon.gameObject.layer = button.gameObject.layer;
            icon.anchorMin = icon.anchorMax = new Vector2(0f, 0.5f);
            icon.pivot = new Vector2(0.5f, 0.5f);
            icon.anchoredPosition = new Vector2(PadOf(button) + 40f, 0f);
            icon.sizeDelta = new Vector2(34f, 34f);
            Image image = icon.GetComponent<Image>();
            if (image == null)
                image = icon.gameObject.AddComponent<Image>();
            image.sprite = kit.S(sprite);
            image.preserveAspect = true;
            image.raycastTarget = false;
            EditorUtility.SetDirty(image);
        }

        /// <summary>Name, question and the two answers in one dark box, growing downward from the same top.</summary>
        private static void ComposePopup(UIBuilderKit kit, RectTransform confirm, RectTransform name)
        {
            float px = 0.002f / confirm.GetComponentInParent<Canvas>().transform.localScale.x;
            float pad = PadOf(confirm);
            float top = confirm.anchoredPosition.y + (confirm.sizeDelta.y - 2f * pad) * 0.5f;
            confirm.sizeDelta = new Vector2(confirm.sizeDelta.x, PopupHeight + 2f * pad);
            confirm.anchoredPosition = new Vector2(confirm.anchoredPosition.x, top - PopupHeight * 0.5f);

            name.SetParent(confirm, false);
            name.SetAsLastSibling();
            name.anchorMin = name.anchorMax = name.pivot = new Vector2(0.5f, 0.5f);
            name.anchoredPosition = new Vector2(0f, 76f);
            name.sizeDelta = new Vector2(580f, 40f);
            TMP_Text title = name.GetComponent<TMP_Text>();
            UIBuilderKit.ApplyStyle(title, kit.H2.With(22f * px, Color.white));
            title.alignment = TextAlignmentOptions.Center;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.enableAutoSizing = true;
            title.fontSizeMin = 15f * px;
            title.fontSizeMax = 22f * px;
            EditorUtility.SetDirty(title);

            RectTransform question = (RectTransform)confirm.Find("Question");
            question.anchoredPosition = new Vector2(0f, 22f);
            question.sizeDelta = new Vector2(580f, 56f);
            TMP_Text questionText = question.GetComponent<TMP_Text>();
            UIBuilderKit.ApplyStyle(questionText, kit.BodyOnDark.With(17f * px));
            EditorUtility.SetDirty(questionText);

            PlaceButton(Child(confirm, "AcceptButton"), new Vector2(-125f, -51.35f), new Vector2(230f, 44f * px), 16f * px);
            PlaceButton(Child(confirm, "DeclineButton"), new Vector2(125f, -51.35f), new Vector2(230f, 44f * px), 16f * px);
        }

        #region Rect helpers (centre-pivoted panels, kit pills placed from their parent's top-left)

        private static float PadOf(RectTransform rect)
        {
            Image image = rect.GetComponent<Image>();
            return image != null && image.type == Image.Type.Sliced && image.pixelsPerUnitMultiplier > 0f
                ? UIBuilderKit.PadFor(image.pixelsPerUnitMultiplier)
                : 0f;
        }

        private static void SetVisibleSize(RectTransform rect, Vector2 visible)
        {
            rect.sizeDelta = visible + Vector2.one * (2f * PadOf(rect));
            EditorUtility.SetDirty(rect);
        }

        /// <summary>A kit pill at a centre position, with round caps at its new height.</summary>
        private static void PlaceButton(RectTransform button, Vector2 centre, Vector2 visible, float fontSize = 0f)
        {
            Image image = button.GetComponent<Image>();
            image.pixelsPerUnitMultiplier = UIBuilderKit.PillMultiplier(image.sprite != null ? image.sprite.name : "Button_", visible.y);
            float pad = UIBuilderKit.PadFor(image.pixelsPerUnitMultiplier);
            Vector2 half = ((RectTransform)button.parent).rect.size * 0.5f;
            float x0 = half.x + centre.x - visible.x * 0.5f - pad;
            float y0 = half.y - centre.y - visible.y * 0.5f - pad;
            button.anchorMin = button.anchorMax = button.pivot = new Vector2(0f, 1f);
            button.anchoredPosition = new Vector2(x0, -y0);
            button.sizeDelta = visible + Vector2.one * (2f * pad);
            if (fontSize > 0f)
            {
                TMP_Text label = button.Find("Label").GetComponent<TMP_Text>();
                label.fontSize = fontSize;
                EditorUtility.SetDirty(label);
            }
            EditorUtility.SetDirty(image);
            EditorUtility.SetDirty(button);
        }

        #endregion

        private static RectTransform Child(Transform parent, string name) =>
            (RectTransform)parent.GetComponentsInChildren<Transform>(true).First(t => t.name == name);

        /// <summary>"Ancestor/Name", inactive included (the HUD panels start hidden); the name may sit deeper.</summary>
        private static RectTransform Find(string path)
        {
            string[] parts = path.Split('/');
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name != parts[1] || !(t is RectTransform rect))
                    continue;
                for (Transform p = t.parent; p != null; p = p.parent)
                {
                    if (p.name == parts[0])
                        return rect;
                }
            }
            throw new System.InvalidOperationException("Scene has no " + path);
        }
    }
}
