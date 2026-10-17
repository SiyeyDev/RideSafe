using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace RideSafe.UI.EditorTools
{
    /// <summary>
    /// Composition pass over the module prefabs, 2026-10-16 review: every screen's title,
    /// options and buttons inside ONE box with the same margin (they floated over the garage
    /// door and its painted logo), «Mi Elección» as one box instead of three, SELECTED inside
    /// its card, labels that never wrap out of their pill, and the video controls as atlas icons.
    /// <para>
    /// The builder calls it while building, and the menu applies it to the existing prefabs
    /// in place: regenerating them gives new internal ids and breaks the scene's references.
    /// Idempotent: it sets absolute values and reuses what it created.
    /// </para>
    /// </summary>
    internal static class UIComposition
    {
        public const string Module00Path = UIModules.Folder + "/PF_Module00_UI.prefab";
        public const string Module01Path = UIModules.Folder + "/PF_Module01_UI.prefab";
        public const string SubtitlesPath = UIModules.Folder + "/PF_Subtitles.prefab";
        /// <summary>Degrees below the gaze for the subtitle strip: at 14 it touched the bottom of the module 1 welcome panel.</summary>
        public const float SubtitlePitch = 17f;
        public const string FrameName = "Frame";
        public const string FrameSprite = "Panel_Modal_Light";
        public const float FrameMultiplier = 2.4f;

        /// <summary>Margin between a box's visible edge and what it holds, the same on every screen.</summary>
        public static readonly Vector4 Margin = new Vector4(28f, 24f, 28f, 24f);

        [MenuItem("RideSafe/UI/Componer paneles en sitio (títulos dentro de la caja)")]
        public static void ApplyInPlace()
        {
            UIBuilderKit kit = RideSafeUIPrefabBuilder.CreateKit();
            Edit(Module00Path, root => ComposeModule00(root, kit));
            Edit(Module01Path, root => ComposeModule01(root, kit));
            Edit(SubtitlesPath, LowerSubtitles);
            if (kit.MissingSprites.Count > 0)
                throw new System.InvalidOperationException("Sprites not in the atlases: " + string.Join(", ", kit.MissingSprites));
            Debug.Log("[RideSafe.UI] Panels composed in place: " + Module00Path + ", " + Module01Path);
        }

        private static void Edit(string path, System.Action<GameObject> compose)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                compose(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        #region Module 0

        public static void ComposeModule00(GameObject root, UIBuilderKit kit)
        {
            WithPanelsOn(root, () =>
            {
                // Welcome: Begin between the door's mark and its wordmark, not over the letters.
                SetVisibleY(Find(root, "Welcome/BeginButton"), 367f, 68f);

                // Language and rules: the question and its cards in the box, close together; Back and
                // Continue stand free under it, aligned to its edges (PO, 2026-10-16).
                Transform language = Find(root, "Language");
                Choice(language, kit, 110f, false);
                Transform rules = Find(root, "Jurisdiction");
                Choice(rules, kit, 130f, true);

                // Vehicle stays as it was designed (PO, 2026-10-16): no box, the confirmation at the
                // bottom and the name cards over their vehicles. Undoes the first pass of this review.
                Transform vehicle = Find(root, "Vehicle");
                RemoveFrame(vehicle);
                Header(vehicle, 960f);
                SetVisibleY(Find(vehicle, "ConfirmationBar"), 440f, 74f);
                SetVisibleY(Find(vehicle, "VehicleLabels/Option_ebike"), 183f, 74f);
                SetVisibleY(Find(vehicle, "VehicleLabels/Option_escooter"), 185f, 66f);
                foreach (Transform card in Find(vehicle, "VehicleLabels"))
                {
                    FitCardToContent fit = card.GetComponent<FitCardToContent>();
                    if (fit == null)
                        continue;
                    fit.HoldCaptionInside(false);
                    EditorUtility.SetDirty(fit);
                }

                Transform comfort = Find(root, "Comfort");
                Header(comfort, 776f);
                RectTransform settings = Find(comfort, "SettingsPanel");
                // One box: the settings sit straight on the screen's frame, not in a box in a box.
                settings.GetComponent<UnityEngine.UI.Image>().enabled = false;
                // «Izquierda» did not fit its pill; it grows leftward, «Derecha» keeps its place.
                SetVisibleWidthKeepRight(Find(comfort, "SettingsPanel/HandGroup/LeftOption"), 105f);
                Enclose box = Frame(comfort, kit, new[] { Find(comfort, "Header/Title"), Find(comfort, "Header/Body"), settings }, Margin);
                ButtonsUnder(box, Find(comfort, "BackButton"), Find(comfort, "StartButton"));

                LabelsNeverWrap(root);
            });
        }

        #endregion

        #region Module 1

        public static void ComposeModule01(GameObject root, UIBuilderKit kit)
        {
            WithPanelsOn(root, () =>
            {
                // Orientation: the title keeps the body's margin; «Repetir instrucciones» fits on one line.
                RectTransform box = Find(root, "Orientation/OrientationPanel");
                float pad = PadOf(box);
                float centre = pad + (box.sizeDelta.x - 2f * pad) * 0.5f;
                SetX(Find(box, "Title"), centre - 270.5f, 541f);
                SetVisibleX(Find(box, "RepeatAudioButton"), centre - 174.5f, 230f);
                SetVisibleX(Find(box, "StartButton"), centre - 174.5f + 245f, 104f);

                // «Mi Elección» read like «Revisa tus elecciones» (PO, 2026-10-16): title, the list in
                // its own box, the note under the list like the lock notice there, and the button free
                // under the panel.
                Transform comparison = Find(root, "Comparison");
                // The dark bar was there to read over the garage ceiling; inside the light box its
                // translucent sprite turned grey. The title reads like every other box's: navy on light.
                RectTransform bar = Find(comparison, "Header/TitleBar");
                bar.GetComponent<UnityEngine.UI.Image>().enabled = false;
                TMP_Text title = Find(bar, "Title").GetComponent<TMP_Text>();
                title.color = UIBuilderKit.BeamNavy;
                EditorUtility.SetDirty(title);
                RectTransform list = Find(comparison, "LeftColumn");
                RectTransform note = Find(comparison, "Header/Body");
                Background(list).enabled = true;
                UnityEngine.UI.Image notice = Background(note);
                kit.AddSliced(notice.gameObject, "Checklist_Row_Selected", 2.4f, Color.white);
                notice.enabled = true;
                TMP_Text noteText = Find(note, "Text").GetComponent<TMP_Text>();
                noteText.color = UIBuilderKit.WordNavy;
                EditorUtility.SetDirty(noteText);
                // Order: bar, list, note. ComparisonView keeps the note under the list as it grows
                // (Module01Binding), and the frame follows both.
                UIBuilderKit.RebuildLayouts(list.gameObject);
                list.anchoredPosition = new Vector2(list.anchoredPosition.x, -(-bar.anchoredPosition.y + bar.sizeDelta.y + ColumnComposition.Gap));
                note.anchoredPosition = new Vector2(note.anchoredPosition.x, list.anchoredPosition.y - list.rect.height - ColumnComposition.Gap);
                Enclose choiceBox = Frame(comparison, kit, new[] { bar, list, note }, new Vector4(20f, 20f, 20f, 22f));
                // «Ver explicación» stands free, centred under the box.
                FollowBelow follow = Find(comparison, "ExplainButton").GetComponent<FollowBelow>();
                if (follow == null)
                    follow = Find(comparison, "ExplainButton").gameObject.AddComponent<FollowBelow>();
                follow.Configure((RectTransform)choiceBox.transform, ButtonsGap);
                follow.Tick();
                EditorUtility.SetDirty(follow);

                // The «Reproduciendo» tag is not needed (PO, 2026-10-16): off, and the view no longer drives it.
                RectTransform playing = Find(root, "Explanation/VideoCard/PlayingTag");
                playing.gameObject.SetActive(false);
                ExplanationView explanation = Find(root, "Explanation").GetComponent<ExplanationView>();
                UIWire.Ref(explanation, "_playingTag", null);
                EditorUtility.SetDirty(explanation);

                // Video controls: «Pausar / seguir» wrapped in its pill; the atlas has the icons.
                RectTransform card = Find(root, "Explanation/VideoCard");
                RectTransform pause = Find(card, "PauseButton");
                float left = pause.anchoredPosition.x + PadOf(pause);
                IconButton(pause, kit, "Icon_Pause", left, 52f);
                IconButton(Find(card, "ReplayButton"), kit, "Icon_Replay", left + 62f, 52f);

                LabelsNeverWrap(root);
            });
        }

        #endregion

        /// <summary>PF_Subtitles is hand-made (no builder): only its follower's pitch changes.</summary>
        private static void LowerSubtitles(GameObject root)
        {
            GazeFollower follower = root.GetComponentInChildren<GazeFollower>(true);
            SerializedObject serialized = new SerializedObject(follower);
            serialized.FindProperty("_pitchOffsetDegrees").floatValue = SubtitlePitch;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        #region Pieces

        /// <summary>The panel's box: a sliced light frame behind everything, kept around the pieces at runtime.</summary>
        public static Enclose Frame(Transform panel, UIBuilderKit kit, RectTransform[] pieces, Vector4 margin)
        {
            Transform existing = panel.Find(FrameName);
            GameObject frame = existing != null ? existing.gameObject : UIBuilderKit.NewUi(FrameName, panel);
            kit.AddSliced(frame, FrameSprite, FrameMultiplier, Color.white);
            Enclose enclose = frame.GetComponent<Enclose>();
            if (enclose == null)
                enclose = frame.AddComponent<Enclose>();
            enclose.Configure(pieces, margin, UIBuilderKit.PadFor(FrameMultiplier));
            UIBuilderKit.RebuildLayouts(panel.gameObject);
            enclose.Refresh();
            EditorUtility.SetDirty(enclose);
            return enclose;
        }

        /// <summary>Where the option cards start: just under the subtitle, not 100 px below it.</summary>
        private const float CardsTop = 156f;
        /// <summary>Between a box's bottom edge and the buttons that stand free under it.</summary>
        private const float ButtonsGap = 16f;

        /// <summary>
        /// A choice screen: question and cards in one box, each card's content centred (the name
        /// sat in the bottom-left corner of an empty card), and Back / Continue free under the box.
        /// </summary>
        private static void Choice(Transform panel, UIBuilderKit kit, float cardHeight, bool described)
        {
            Header(panel, 760f);
            var pieces = new List<RectTransform> { Find(panel, "Header/Title"), Find(panel, "Header/Body") };
            foreach (Transform option in Find(panel, "Options"))
            {
                RectTransform card = (RectTransform)option;
                float pad = PadOf(card);
                float w = card.sizeDelta.x - 2f * pad;
                SetVisibleY(card, CardsTop, cardHeight);
                if (described)
                {
                    InCard(card, "StateCaption", 60f, 12f, w - 60f, 26f);
                    InCard(card, "Title", 24f, 32f, w - 24f, 62f);
                    InCard(card, "Description", 24f, 66f, w - 24f, cardHeight - 14f);
                }
                else
                {
                    InCard(card, "Title", 24f, 30f, w - 24f, 64f);
                    InCard(card, "StateCaption", 24f, 70f, w - 24f, 86f);
                }
                foreach (string text in new[] { "Title", "StateCaption", "Description" })
                {
                    TMP_Text tmp = Find(card, text).GetComponent<TMP_Text>();
                    tmp.alignment = text == "Description" ? TextAlignmentOptions.Top : TextAlignmentOptions.Center;
                    EditorUtility.SetDirty(tmp);
                }
                pieces.Add(card);
            }
            Enclose box = Frame(panel, kit, pieces.ToArray(), Margin);
            ButtonsUnder(box, Find(panel, "BackButton"), Find(panel, "ContinueButton"));
        }

        /// <summary>Back on the box's left edge, the forward action on its right edge, free under it.</summary>
        private static void ButtonsUnder(Enclose box, RectTransform left, RectTransform right)
        {
            RectTransform frame = (RectTransform)box.transform;
            float pad = PadOf(frame);
            float boxLeft = frame.anchoredPosition.x + pad;
            float boxRight = frame.anchoredPosition.x + frame.sizeDelta.x - pad;
            float top = -frame.anchoredPosition.y + frame.sizeDelta.y - pad + ButtonsGap;
            foreach (RectTransform button in new[] { left, right })
            {
                float buttonPad = PadOf(button);
                float width = button.sizeDelta.x - 2f * buttonPad;
                SetVisibleX(button, button == left ? boxLeft : boxRight - width, width);
                SetVisibleY(button, top, button.sizeDelta.y - 2f * buttonPad);
            }
        }

        private static void RemoveFrame(Transform panel)
        {
            Transform frame = panel.Find(FrameName);
            if (frame != null)
                Object.DestroyImmediate(frame.gameObject);
        }

        /// <summary>A card child relative to the card's VISIBLE top-left (like UIElements.PlaceInCard).</summary>
        private static void InCard(RectTransform card, string child, float x0, float y0, float x1, float y1)
        {
            float pad = PadOf(card);
            UIBuilderKit.Place(Find(card, child), x0 + pad, y0 + pad, x1 + pad, y1 + pad);
        }

        /// <summary>A centred title and subtitle as wide as the box below them, not the whole frame.</summary>
        private static void Header(Transform panel, float width)
        {
            foreach (string name in new[] { "Header/Title", "Header/Body" })
                SetX(Find(panel, name), UIBuilderKit.FrameW * 0.5f - width * 0.5f, width);
        }

        /// <summary>Pill labels shrink a little before they wrap: a second line spilled out of the pill.</summary>
        private static void LabelsNeverWrap(GameObject root)
        {
            foreach (UnityEngine.UI.Selectable selectable in root.GetComponentsInChildren<UnityEngine.UI.Selectable>(true))
            {
                // Pills only: checklist rows are Buttons too, with their own label layout.
                UnityEngine.UI.Image pill = selectable.GetComponent<UnityEngine.UI.Image>();
                if (pill == null || pill.sprite == null || !pill.sprite.name.StartsWith("Button_"))
                    continue;
                Transform label = selectable.transform.Find("Label");
                TMP_Text text = label != null ? label.GetComponent<TMP_Text>() : null;
                if (text == null)
                    continue;
                if (!text.enableAutoSizing)
                {
                    text.fontSizeMax = text.fontSize;
                    text.fontSizeMin = Mathf.Min(12f, text.fontSize);
                    text.enableAutoSizing = true;
                }
                text.textWrappingMode = TextWrappingModes.NoWrap;
                // Room inside the pill's caps; the label is stretched over the whole pill.
                float side = ((RectTransform)selectable.transform).sizeDelta.y * 0.2f;
                text.margin = new Vector4(side, 0f, side, 0f);
                EditorUtility.SetDirty(text);
            }
        }

        private static void IconButton(RectTransform button, UIBuilderKit kit, string sprite, float visibleX, float width)
        {
            SetVisibleX(button, visibleX, width);
            button.Find("Label").gameObject.SetActive(false);
            Transform existing = button.Find("Icon");
            GameObject icon = existing != null ? existing.gameObject : UIBuilderKit.NewUi("Icon", button);
            RectTransform rect = (RectTransform)icon.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(30f, 30f);
            UnityEngine.UI.Image image = icon.GetComponent<UnityEngine.UI.Image>();
            if (image == null)
                image = icon.AddComponent<UnityEngine.UI.Image>();
            image.sprite = kit.S(sprite);
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
        }

        private static UnityEngine.UI.Image Background(Transform host) =>
            host.Find("Background").GetComponent<UnityEngine.UI.Image>();

        #endregion

        #region Rect helpers (kit rects: top-left anchor and pivot, inflated by the atlas padding)

        private static float PadOf(RectTransform rect)
        {
            UnityEngine.UI.Image image = rect.GetComponent<UnityEngine.UI.Image>();
            return image != null && image.type == UnityEngine.UI.Image.Type.Sliced && image.pixelsPerUnitMultiplier > 0f
                ? UIBuilderKit.PadFor(image.pixelsPerUnitMultiplier)
                : 0f;
        }

        private static void SetX(RectTransform rect, float x, float width)
        {
            rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
            rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
        }

        private static void SetVisibleX(RectTransform rect, float visibleX, float visibleWidth)
        {
            float pad = PadOf(rect);
            SetX(rect, visibleX - pad, visibleWidth + 2f * pad);
        }

        private static void SetVisibleY(RectTransform rect, float visibleY, float visibleHeight)
        {
            float pad = PadOf(rect);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -(visibleY - pad));
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, visibleHeight + 2f * pad);
        }

        private static void SetVisibleWidthKeepRight(RectTransform rect, float visibleWidth)
        {
            float pad = PadOf(rect);
            float right = rect.anchoredPosition.x + rect.sizeDelta.x - pad;
            SetVisibleX(rect, right - visibleWidth, visibleWidth);
        }

        #endregion

        private static void WithPanelsOn(GameObject root, System.Action compose)
        {
            List<GameObject> off = new List<GameObject>();
            foreach (Transform panel in root.transform)
            {
                if (panel.gameObject.activeSelf)
                    continue;
                off.Add(panel.gameObject);
                panel.gameObject.SetActive(true);
            }
            try
            {
                compose();
            }
            finally
            {
                foreach (GameObject panel in off)
                    panel.SetActive(false);
            }
        }

        private static RectTransform Find(GameObject root, string path) => Find(root.transform, path);

        private static RectTransform Find(Transform root, string path)
        {
            Transform child = root.Find(path);
            if (child == null)
                throw new System.InvalidOperationException("'" + root.name + "' has no '" + path + "'.");
            return (RectTransform)child;
        }
    }
}
