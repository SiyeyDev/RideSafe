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
        // 216 left the answers 21 units off the bottom edge (PO: too tight); +12 for the two-line name.
        // 274 (a full 22-pt second line) put «Sí»/«No» against the bottom of the PC view.
        private const float PopupHeight = 244f;
        /// <summary>
        /// The item's name: the sheet's «Elemento» column, up to «Casco agrietado, aplastado, talla
        /// evidentemente incorrecta o de novedad». On one line it shrank to 15 and still touched the edges
        /// (PO, 2026-10-26), so it wraps to two lines inside a 40-unit margin on each side. A one-line name
        /// keeps the 22 heading (40.6 units); two lines fit 64 at about 17, the question's size.
        /// </summary>
        private const float PopupNameWidth = 540f;
        private const float PopupNameHeight = 64f;
        private const float PopupQuestionHeight = 44f;
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
            ComposeFocus();
            ComposePopup(kit, Find("ItemInspectorCanvas/Confirmation"), Find("ItemInspectorCanvas/ItemName"));

            RectTransform sign = Find("Module01Controls/StationInstruction");
            SetVisibleSize(sign, new Vector2(640f, 98f));
            RectTransform text = (RectTransform)sign.Find("Instruction");
            text.anchoredPosition = Vector2.zero;
            text.sizeDelta = new Vector2(592f, 80f);
            EditorUtility.SetDirty(text);
            // The company mark on its first line, the station's name (white: the sign is dark).
            UIBrand.Place(kit, text.GetComponent<TMP_Text>(), text, 0f, true);

            ComposeDecision(kit, Find("Module01Controls/ReadinessDecision"));
            ComposeTheaterVideo();

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

        public const string FocusMaterialPath = "Assets/_Project/UI/Materials/M_FocusDome.mat";
        /// <summary>
        /// Between the farthest point of the popup (1.48 m) and the nearest of any vehicle in any zone
        /// (the e-bike from behind, 1.70 m), measured 2026-10-23; FocusDomeSceneTests keeps it so.
        /// </summary>
        private const float FocusRadius = 1.6f;

        /// <summary>
        /// The dark bubble around the head while a vehicle point is open (PO, 2026-10-23): a Unity
        /// sphere seen from inside, URP Unlit, transparent, at queue 2950 so the popup is drawn after
        /// it. Under the presentation and following its camera, like everything the learner sees.
        /// </summary>
        private static void ComposeFocus()
        {
            Material material = FocusMaterial();
            Transform presentation = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(t => t.name == "Module01_Presentation");
            Transform existing = presentation.Find("FocusDome");
            GameObject dome;
            if (existing != null)
                dome = existing.gameObject;
            else
            {
                dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dome.name = "FocusDome";
                dome.transform.SetParent(presentation, false);
                Object.DestroyImmediate(dome.GetComponent<Collider>());
            }
            dome.layer = 0;
            MeshRenderer renderer = dome.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            GameObjectUtility.SetStaticEditorFlags(dome, 0);
            // Not '??': in the editor a missing component comes back as a fake null.
            RideSafe.UI.FocusDome focus = dome.GetComponent<RideSafe.UI.FocusDome>();
            if (focus == null)
                focus = dome.AddComponent<RideSafe.UI.FocusDome>();
            focus.ConfigureForTests(renderer, presentation.parent, FocusRadius);
            focus.Tick(0f);
            EditorUtility.SetDirty(focus);
            EditorUtility.SetDirty(renderer);

            Module01Experience experience = Object.FindAnyObjectByType<Module01Experience>(FindObjectsInactive.Include);
            var serialized = new SerializedObject(experience);
            serialized.FindProperty("_focus").objectReferenceValue = focus;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>URP Unlit: black, transparent, back faces only (seen from inside), no depth write, queue 2950.</summary>
        private static Material FocusMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(FocusMaterialPath);
            if (material == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/UI/Materials"))
                    AssetDatabase.CreateFolder("Assets/_Project/UI", "Materials");
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, FocusMaterialPath);
            }
            material.shader = Shader.Find("Universal Render Pipeline/Unlit");
            material.SetColor("_BaseColor", Color.black);
            material.SetFloat("_Surface", 1f);   // transparent
            material.SetFloat("_Blend", 0f);     // alpha
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Front); // render face: back
            material.SetFloat("_AlphaClip", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.SetShaderPassEnabled("DepthOnly", false);
            // URP recomputes the queue from the surface and its priority (_QueueOffset, -50..50):
            // transparent at the lowest priority is 2950, still before the UI canvases at 3000.
            material.SetFloat("_QueueOffset", -50f);
            material.renderQueue = 2950;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
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
            // The question reads like «Revisa tus elecciones»: left-aligned beside the company mark,
            // with the hint under it on the same left edge as the answers. Centred, its two lines sat
            // off-balance next to the mark, and in 56 of height the second ran into the hint (PO, 2026-10-21).
            RectTransform title = (RectTransform)decision.Find("Title");
            title.anchoredPosition = new Vector2(0f, 134f);
            title.sizeDelta = new Vector2(420f, 68f);
            TMP_Text titleText = title.GetComponent<TMP_Text>();
            titleText.fontSize = 26f;
            titleText.textWrappingMode = TextWrappingModes.Normal;
            titleText.alignment = TextAlignmentOptions.Left;
            EditorUtility.SetDirty(titleText);

            Transform existing = decision.Find("Hint");
            RectTransform hint = existing != null ? (RectTransform)existing : (RectTransform)new GameObject("Hint", typeof(RectTransform)).transform;
            hint.SetParent(decision, false);
            hint.gameObject.layer = decision.gameObject.layer;
            hint.anchorMin = hint.anchorMax = hint.pivot = new Vector2(0.5f, 0.5f);
            hint.anchoredPosition = new Vector2(0f, 82f);
            hint.sizeDelta = new Vector2(420f, 22f);
            kit.AddText(hint.gameObject, "M1/Decision_Hint", kit.Sub.With(16f), TextAlignmentOptions.Left);
            Module01Text localized = hint.GetComponent<Module01Text>();
            if (localized == null)
                localized = hint.gameObject.AddComponent<Module01Text>();
            localized.Configure("M1/Decision_Hint");
            EditorUtility.SetDirty(localized);

            // The answers keep their own green, amber and red pills (the meeting's colours) with the
            // atlas's status glyphs. The icon-only traffic light is for the report rows; here Cindy
            // asked to keep the coloured pills (2026-10-20).
            var answers = new[] { ("RoadReadyButton", "Status_Independent", (Color)new Color32(0x74, 0xC6, 0x8A, 0xFF), 26f),
                                  ("NeedsServiceButton", "Status_Attention", (Color)new Color32(0xF2, 0xC4, 0x5A, 0xFF), -52f),
                                  ("DoNotRideButton", "Status_Retry", (Color)new Color32(0xE8, 0x7E, 0x72, 0xFF), -130f) };
            foreach (var (name, glyph, tint, y) in answers)
            {
                RectTransform button = Child(decision, name);
                PlaceButton(button, new Vector2(0f, y), new Vector2(420f, 64f), 18f);
                Image pill = button.GetComponent<Image>();
                pill.color = tint;
                EditorUtility.SetDirty(pill);
                TMP_Text label = button.Find("Label").GetComponent<TMP_Text>();
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.margin = new Vector4(72f, 0f, 16f, 0f);
                EditorUtility.SetDirty(label);
                Icon(kit, button, glyph, Color.white);
            }
            // The company mark where the answers start, on the question's first line.
            UIBrand.Place(kit, titleText, Child(decision, "RoadReadyButton"), 0f, false);
        }

        /// <summary>The status glyph at the start of an answer pill, tinted with its traffic-light colour.</summary>
        private static void Icon(UIBuilderKit kit, RectTransform button, string sprite, Color tint)
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
            image.color = tint;
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
            // 20 under the top edge, as before; a one-line name sits centred in the two-line box.
            name.anchoredPosition = new Vector2(0f, PopupHeight * 0.5f - 20f - PopupNameHeight * 0.5f);
            name.sizeDelta = new Vector2(PopupNameWidth, PopupNameHeight);
            TMP_Text title = name.GetComponent<TMP_Text>();
            UIBuilderKit.ApplyStyle(title, kit.H2.With(22f * px, Color.white));
            title.alignment = TextAlignmentOptions.Center;
            title.textWrappingMode = TextWrappingModes.Normal;
            title.overflowMode = TextOverflowModes.Overflow;
            title.enableAutoSizing = true;
            title.fontSizeMin = 16f * px;
            title.fontSizeMax = 22f * px;
            EditorUtility.SetDirty(title);

            RectTransform question = (RectTransform)confirm.Find("Question");
            // 6 under the name; the answers 23.35 under the question, 42.65 off the bottom as before.
            float nameBottom = name.anchoredPosition.y - PopupNameHeight * 0.5f;
            question.anchoredPosition = new Vector2(0f, nameBottom - 6f - PopupQuestionHeight * 0.5f);
            question.sizeDelta = new Vector2(580f, PopupQuestionHeight);
            TMP_Text questionText = question.GetComponent<TMP_Text>();
            UIBuilderKit.ApplyStyle(questionText, kit.BodyOnDark.With(17f * px));
            EditorUtility.SetDirty(questionText);

            float answersY = question.anchoredPosition.y - PopupQuestionHeight * 0.5f - 23.35f - 22f;
            PlaceButton(Child(confirm, "AcceptButton"), new Vector2(-125f, answersY), new Vector2(230f, 44f * px), 16f * px);
            PlaceButton(Child(confirm, "DeclineButton"), new Vector2(125f, answersY), new Vector2(230f, 44f * px), 16f * px);
            // No mark here: the popup opens over the station, whose sign carries it (one logo in view).
            UIBrand.Remove(title);
        }

        #region Rect helpers (centre-pivoted panels, kit pills placed from their parent's top-left)

        private static float PadOf(RectTransform rect)
        {
            Image image = rect.GetComponent<Image>();
            return image != null && image.type == Image.Type.Sliced && image.pixelsPerUnitMultiplier > 0f
                ? UIBuilderKit.PadFor(image.pixelsPerUnitMultiplier)
                : 0f;
        }

        // ---- El video a pantalla completa --------------------------------------------------
        //
        // El cliente dijo que los videos salían pequeños, y medido tenía razón: la superficie
        // ocupaba 1,03 x 0,50 m vista desde 2,03 m, o sea 28,5° de los 91,5° que abarca el ojo
        // —un 31 % del ancho— y además estaba descentrada 88 cm a la derecha, porque el panel
        // repartía mitad video y mitad fichas de texto.
        //
        // En VR no existe «pantalla completa»: lo que hay es el ángulo que ocupa un plano en el
        // mundo. Lo que se hace aquí es darle al video el panel entero y pasar el texto a una
        // banda que se superpone sobre su borde inferior, como el control de un reproductor.
        // El panel mide 1002 x 556, que es casi exactamente 16:9, así que el video lo llena sin
        // bandas negras. Resultado: 1,90 m de ancho y 50,2° de ángulo, centrado.
        //
        // Por qué no más: pasar de ~60° obliga a mover la cabeza para ver los bordes, y el ancho
        // está topado por el propio panel. Para ir más allá habría que acercar el panel o subir
        // la escala del canvas, y las dos cosas afectan a las demás pantallas.

        /// <summary>Margen del video contra el borde del panel, en unidades del kit.</summary>
        private const float VideoInset = 26f;

        /// <summary>Alto de la banda de controles que se superpone al pie del video.</summary>
        private const float ControlBand = 118f;

        /// <summary>Franja al pie del panel, fuera del video, donde vive el subtitulo.</summary>
        private const float SubtitleBand = 58f;

        private static void ComposeTheaterVideo()
        {
            // El panel es el hijo directo de PF_Module01_UI. Hay dos «Explanation» bajo él —el panel y
            // su caja de texto— y Find devuelve el primero que encuentre, sin orden garantizado: el
            // 2026-10-23 cayó la caja y la composición se detuvo al no hallar VideoCard.
            RectTransform panel = (RectTransform)Find("Desktop Camera/PF_Module01_UI").Find("Explanation");
            Vector2 size = panel.rect.size;

            // El video manda su proporcion y se queda con todo menos la franja del subtitulo.
            // Sin esa franja el subtitulo caia encima de los botones de pausa y repetir: el
            // video llegaba hasta el borde del panel y no quedaba sitio fuera de el.
            float alto = size.y - SubtitleBand;
            float ancho = alto * 16f / 9f;
            if (ancho > size.x - 2f * VideoInset)
            {
                ancho = size.x - 2f * VideoInset;
                alto = ancho * 9f / 16f;
            }
            float arriba = (size.y - SubtitleBand - alto) * .5f;

            // Centrado de verdad: anclarlo al margen izquierdo lo dejaba 6,5 cm a un lado, que
            // en VR se nota porque el video es lo unico que se mira.
            float izquierda = (size.x - ancho) * .5f;

            RectTransform card = (RectTransform)panel.Find("VideoCard");
            Place(card, new Vector2(izquierda, -arriba), new Vector2(ancho, alto));

            // Dentro de la tarjeta todo va en coordenadas suyas, con el origen arriba a la izq.
            Place(Child(card, "VideoSurface"), new Vector2(0f, 0f), new Vector2(ancho, alto));
            Place(Child(card, "VideoFrame"), new Vector2(0f, 0f), new Vector2(ancho, alto));

            // La barra de avance, pegada al borde inferior del video.
            RectTransform progreso = Child(card, "VideoProgress");
            Place(progreso, new Vector2(0f, -(alto - ControlBand)), new Vector2(ancho, 4f));

            // Leyenda y botones, dentro de la banda, sobre el propio video.
            float bandaY = -(alto - ControlBand + 14f);
            Place(Child(card, "Caption"), new Vector2(24f, bandaY), new Vector2(ancho - 300f, 32f));

            float botonesY = -(alto - 56f);
            MoveOnly(Child(card, "PauseButton"), new Vector2(24f, botonesY));
            MoveOnly(Child(card, "ReplayButton"), new Vector2(88f, botonesY));
            MoveOnly(Child(card, "ContinueButton"), new Vector2(ancho - 134f, botonesY));
            MoveOnly(Child(card, "PlayingTag"), new Vector2(ancho - 120f, -18f));

            // La ficha del elemento y su explicacion pasan a superponerse arriba a la derecha,
            // que es la esquina con menos informacion en un plano general.
            RectTransform ficha = (RectTransform)panel.Find("ItemCard");
            Place(ficha, new Vector2(izquierda + ancho - 351f - 14f, -arriba - 18f), new Vector2(351f, 185f));

            RectTransform caja = (RectTransform)panel.Find("Explanation");
            Place(caja, new Vector2(izquierda + ancho - 353f - 14f, -arriba - 215f), new Vector2(353f, 122f));

            // El subtitulo baja al pie del panel, fuera del video, para no pisar la banda.
            RectTransform sub = (RectTransform)panel.Find("Subtitle");
            if (sub != null)
                MoveOnly(sub, new Vector2((size.x - sub.rect.width) * .5f,
                                          -(arriba + alto + (SubtitleBand - sub.rect.height) * .5f)));

            Debug.Log($"[Module01] Video a pantalla completa: {ancho:F0}x{alto:F0} unidades "
                      + $"= {ancho * .002f:F2} x {alto * .002f:F2} m, centrado. Antes eran 1,03 x 0,50 "
                      + "descentrados: el angulo pasa de 28,5° a 50,2°.");
        }

        private static RectTransform Child(RectTransform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t == null)
                throw new System.InvalidOperationException($"No encuentro '{name}' bajo '{parent.name}'.");
            return (RectTransform)t;
        }

        /// <summary>Coloca un rect anclado arriba a la izquierda, con su tamaño visible.</summary>
        private static void Place(RectTransform rect, Vector2 topLeft, Vector2 visible)
        {
            float pad = PadOf(rect);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(topLeft.x - pad, topLeft.y + pad);
            rect.sizeDelta = visible + Vector2.one * (2f * pad);
            EditorUtility.SetDirty(rect);
        }

        /// <summary>Lo mueve sin tocarle el tamaño: para los botones, que el kit ya midió.</summary>
        private static void MoveOnly(RectTransform rect, Vector2 topLeft)
        {
            float pad = PadOf(rect);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(topLeft.x - pad, topLeft.y + pad);
            EditorUtility.SetDirty(rect);
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
