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
            foreach (string flag in LocationFlags.Values)
                UIStatusIcons.Configure(IconsFolder + "/" + flag + ".png");
            UIBuilderKit kit = RideSafeUIPrefabBuilder.CreateKit();
            Edit(Module00Path, root => ComposeModule00(root, kit));
            Edit(Module01Path, root => ComposeModule01(root, kit));
            Edit(SubtitlesPath, LowerSubtitles);
            if (kit.MissingSprites.Count > 0)
                throw new System.InvalidOperationException("Sprites not in the atlases: " + string.Join(", ", kit.MissingSprites));
            Debug.Log("[RideSafe.UI] Panels composed in place: " + Module00Path + ", " + Module01Path);
        }

        /// <summary>
        /// The video's «Repetir». The atlas's <c>Icon_Replay</c> has a 9-px ring beside the 15-px pause
        /// bars and a loose arrowhead, and read as something else (PO, 2026-10-26). This one is drawn
        /// in the same navy with a stroke to match: <c>UI/Icons/Icon_Replay_Round.png</c>.
        /// </summary>
        public const string ReplayIcon = "Icon_Replay_Round";
        public const string IconsFolder = "Assets/_Project/UI/Icons";

        [MenuItem("RideSafe/UI/Icono de repetir del video")]
        public static void ApplyReplayIcon()
        {
            UIBuilderKit kit = RideSafeUIPrefabBuilder.CreateKit();
            Edit(Module01Path, root =>
            {
                RectTransform card = Find(root, "Explanation/VideoCard");
                RectTransform replay = Find(card, "ReplayButton");
                RectTransform pause = Find(card, "PauseButton");
                IconButton(replay, kit, ReplayIcon, pause.anchoredPosition.x + PadOf(pause) + 62f, 52f);
            });
            if (kit.MissingSprites.Count > 0)
                throw new System.InvalidOperationException("Sprites not in the atlases: " + string.Join(", ", kit.MissingSprites));
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
                // Welcome: Begin between the door's mark and its wordmark, not over the letters. With
                // the learner 1 m further back (2026-10-20) that gap is 64 high: a 52-high pill fits.
                RectTransform begin = Find(root, "Welcome/BeginButton");
                begin.GetComponent<UnityEngine.UI.Image>().pixelsPerUnitMultiplier = UIBuilderKit.PillMultiplier("Button_Primary_Default", 52f);
                SetVisibleY(begin, 339f, 52f);

                // Language and rules laid out like the vehicle screen (PO, 2026-10-21): see Choice.
                Transform language = Find(root, "Language");
                Choice(language, kit, 110f, false, 480f);
                Transform rules = Find(root, "Jurisdiction");
                // Its question is the longest: the box grows to it, the font shrinks the rest (28–34 pt).
                Choice(rules, kit, 142f, true, 700f);

                // Vehicle stays as it was designed (PO, 2026-10-16): the confirmation at the bottom and
                // the name cards over their vehicles. Only its title gets a box (2026-10-21): navy on the
                // garage siding, «Elige tu vehículo» got lost.
                Transform vehicle = Find(root, "Vehicle");
                Header(vehicle, VehicleHeaderWidth);
                TitleAboveDoor(vehicle);
                Transform firstPass = vehicle.Find(FrameName); // its header box was named "Frame" for a day
                if (firstPass != null)
                    Object.DestroyImmediate(firstPass.gameObject);
                HeaderBox(vehicle, kit);
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
                TitleAboveDoor(comfort);
                RectTransform settings = Find(comfort, "SettingsPanel");
                // The settings sit straight on their box, not in a box in a box.
                settings.GetComponent<UnityEngine.UI.Image>().enabled = false;
                // «Izquierda» did not fit its pill; it grows leftward, «Derecha» keeps its place.
                SetVisibleWidthKeepRight(Find(comfort, "SettingsPanel/HandGroup/LeftOption"), 105f);
                // The settings stay on the door where they were (top 170) now that the title went up.
                HeaderBox(comfort, kit);
                settings.anchoredPosition = new Vector2(settings.anchoredPosition.x, -ComfortSettingsTop);
                Enclose box = ContentBox(comfort, kit, new[] { settings });
                ButtonsUnder(box, Find(comfort, "BackButton"), Find(comfort, "StartButton"), NavButtonWidth, 220f);
                // «Iniciar Módulo 1» keeps its words: it starts the module, it is not a step back or forth.
                NavIcon(Find(comfort, "BackButton"), kit, "Icon_Back");

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
                // The company mark at the top left of every panel (2026-10-21), here where the body text
                // starts and on the «MÓDULO 1 · PREPARACIÓN» line above the title (PO). The title keeps
                // clear of it on one line: «Prepárate antes de rodar» wrapped onto the body.
                TMP_Text welcome = Find(box, "Title").GetComponent<TMP_Text>();
                welcome.textWrappingMode = TextWrappingModes.NoWrap;
                welcome.fontSizeMax = 34f;
                welcome.fontSizeMin = 28f;
                welcome.enableAutoSizing = true;
                EditorUtility.SetDirty(welcome);
                UIBrand.Place(kit, Find(box, "ModuleTag").GetComponent<TMP_Text>(), Find(box, "Body"), 0f, false, welcome);
                // «Revisa tus elecciones»: title, subtitle and the two buttons centred in the box, like
                // every other panel (PO, 2026-10-29: «todo unificado»). The mark stays at the left of the
                // content and the centred title gives up the same on the right (UIBrand.KeepClear).
                RectTransform review = Find(root, "ReviewChoices/Modal");
                foreach (string text in new[] { "Title", "Subtitle" })
                {
                    CenterIn(review, Find(review, text));
                    Align(Find(review, text), HorizontalAlignmentOptions.Center);
                }
                CenterIn(review, Find(review, "KeepChoosingButton"), Find(review, "ConfirmButton"));
                // «Al confirmar se evaluará esta sección» goes (PO, 2026-10-29): the buttons move up
                // into its place and the box shrinks by the same, so no hole is left. Once only.
                RectTransform lockNotice = Find(review, "LockNotice");
                if (lockNotice.gameObject.activeSelf)
                {
                    RectTransform keep = Find(review, "KeepChoosingButton"), confirm = Find(review, "ConfirmButton");
                    float lift = lockNotice.anchoredPosition.y - keep.anchoredPosition.y;
                    foreach (RectTransform button in new[] { keep, confirm })
                    {
                        button.anchoredPosition += new Vector2(0f, lift);
                        EditorUtility.SetDirty(button);
                    }
                    review.sizeDelta -= new Vector2(0f, lift);
                    EditorUtility.SetDirty(review);
                    lockNotice.gameObject.SetActive(false);
                    Find(review, "LockNoticeText").gameObject.SetActive(false);
                }
                UIBrand.Place(kit, Find(review, "Title").GetComponent<TMP_Text>(), Find(review, "Subtitle"), 0f, false);
                // The sidebars («Mi preparación») share the view with the station sign, which carries
                // the mark: one logo in view, by the title. Their title and subtitle are centred too.
                foreach (string side in new[] { "Preparation/ChecklistPanel", "Diagnostic/ChecklistPanel" })
                {
                    UIBrand.Remove(Find(root, side + "/Title").GetComponent<TMP_Text>());
                    Align(Find(root, side + "/Title"), HorizontalAlignmentOptions.Center);
                    Align(Find(root, side + "/Subtitle"), HorizontalAlignmentOptions.Center);
                }

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
                // Nothing taken in a section: a line in the list box says so (ComparisonView toggles it).
                Transform existingEmpty = list.Find("EmptyState");
                GameObject empty = existingEmpty != null ? existingEmpty.gameObject : UIBuilderKit.NewUi("EmptyState", list);
                kit.AddText(empty, "No elegiste ningún elemento en esta sección.", kit.Body.With(16f, UIBuilderKit.Muted),
                            TextAlignmentOptions.Center);
                UnityEngine.UI.LayoutElement emptyLayout = empty.GetComponent<UnityEngine.UI.LayoutElement>();
                if (emptyLayout == null)
                    emptyLayout = empty.AddComponent<UnityEngine.UI.LayoutElement>();
                emptyLayout.minHeight = emptyLayout.preferredHeight = 44f;
                empty.transform.SetAsLastSibling();
                empty.SetActive(false);
                UIWire.Ref(comparison.GetComponent<ComparisonView>(), "_emptyState", empty);
                UIBuilderKit.RebuildLayouts(list.gameObject);
                list.anchoredPosition = new Vector2(list.anchoredPosition.x, -(-bar.anchoredPosition.y + bar.sizeDelta.y + ColumnComposition.Gap));
                note.anchoredPosition = new Vector2(note.anchoredPosition.x, list.anchoredPosition.y - list.rect.height - ColumnComposition.Gap);
                Enclose choiceBox = Frame(comparison, kit, new[] { bar, list, note }, new Vector4(20f, 20f, 20f, 22f));
                // The whole logo sat here at 54 px, its «RideSafe» unreadable: the mark replaces it.
                Transform wordmark = bar.Find("Logo");
                if (wordmark != null)
                    Object.DestroyImmediate(wordmark.gameObject);
                UIBrand.Place(kit, title, (RectTransform)choiceBox.transform, 20f, false);
                // «Ver explicación» stands free, centred under the box.
                FollowBelow follow = Find(comparison, "ExplainButton").GetComponent<FollowBelow>();
                if (follow == null)
                    follow = Find(comparison, "ExplainButton").gameObject.AddComponent<FollowBelow>();
                follow.Configure((RectTransform)choiceBox.transform, ButtonsGap);
                follow.Tick();
                EditorUtility.SetDirty(follow);

                // Traffic-light status icons in the report and the explanation (PO, 2026-10-20).
                foreach (Object view in new Object[] { comparison.GetComponent<ComparisonView>(),
                                                        Find(root, "Explanation").GetComponent<ExplanationView>() })
                {
                    Semaphore(view, kit);
                    // A bit larger than the 28 they were built at (PO, 2026-10-29).
                    foreach (Transform icon in ((Component)view).GetComponentsInChildren<Transform>(true))
                        if (icon.name == "Icon" && icon.parent.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>() != null
                            && icon.TryGetComponent(out UnityEngine.UI.LayoutElement size))
                        {
                            size.minWidth = size.preferredWidth = size.minHeight = size.preferredHeight = UIElements.StatusIconSize;
                            EditorUtility.SetDirty(size);
                        }
                }

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
                IconButton(Find(card, "ReplayButton"), kit, ReplayIcon, left + 62f, 52f);

                // The review and «Mi Elección» scroll past a cap instead of growing off the view (PO, 2026-10-26).
                UIScrollList.ApplyToModule01(root, kit);

                LabelsNeverWrap(root);
            });
        }

        #endregion

        /// <summary>
        /// White status glyphs tinted by status, in ItemStatus order (Selected, CoreOmitted,
        /// ConditionDependent, NotSuited, AtRisk, NoEffect): green for correct, yellow for
        /// conditional or no effect, red for an omission, a hazard or damaged gear.
        /// </summary>
        /// <remarks>
        /// One shape per reading since 2026-10-29 (PO): check = correct, X = incorrect (an omission, a
        /// hazard or damaged gear), dash = not needed. The ringed check for «not needed» read as a
        /// second «correct», and the red ring with a bar as a «!».
        /// </remarks>
        public static readonly (string Glyph, Color Tint)[] StatusSemaphore =
        {
            ("Status_Independent", UIBuilderKit.SemaphoreGreen),
            ("Status_Wrong", UIBuilderKit.SemaphoreRed),
            ("Status_NotNeeded", UIBuilderKit.SemaphoreYellow),
            ("Status_Wrong", UIBuilderKit.SemaphoreRed),
            ("Status_Wrong", UIBuilderKit.SemaphoreRed),
            ("Status_NotNeeded", UIBuilderKit.SemaphoreYellow)
        };

        private static void Semaphore(Object view, UIBuilderKit kit)
        {
            SerializedObject serialized = new SerializedObject(view);
            SerializedProperty sprites = serialized.FindProperty("_glyphs.Sprites");
            SerializedProperty colors = serialized.FindProperty("_glyphs.Colors");
            sprites.arraySize = colors.arraySize = StatusSemaphore.Length;
            for (int i = 0; i < StatusSemaphore.Length; i++)
            {
                sprites.GetArrayElementAtIndex(i).objectReferenceValue = kit.S(UIStatusIcons.MonoName(StatusSemaphore[i].Glyph));
                colors.GetArrayElementAtIndex(i).colorValue = StatusSemaphore[i].Tint;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

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
        public static Enclose Frame(Transform panel, UIBuilderKit kit, RectTransform[] pieces, Vector4 margin, string name = FrameName,
                                    string sprite = FrameSprite)
        {
            Transform existing = panel.Find(name);
            GameObject frame = existing != null ? existing.gameObject : UIBuilderKit.NewUi(name, panel);
            kit.AddSliced(frame, sprite, FrameMultiplier, Color.white);
            Enclose enclose = frame.GetComponent<Enclose>();
            if (enclose == null)
                enclose = frame.AddComponent<Enclose>();
            enclose.Configure(pieces, margin, UIBuilderKit.PadFor(FrameMultiplier));
            UIBuilderKit.RebuildLayouts(panel.gameObject);
            enclose.Refresh();
            EditorUtility.SetDirty(enclose);
            return enclose;
        }

        /// <summary>«Elige tu vehículo» and its subtitle: as wide as they need, the vehicles stay in view.</summary>
        private const float VehicleHeaderWidth = 480f;
        public const string HeaderFrameName = "HeaderFrame";
        /// <summary>The title's box hugs its text: the title's rect already leaves room above the letters.</summary>
        private static readonly Vector4 HeaderMargin = new Vector4(28f, 4f, 28f, 12f);
        /// <summary>Between the title's box and the box of options under it.</summary>
        private const float BoxGap = 16f;
        /// <summary>
        /// The title's box on the module 0 screens: the blue of the module 1 station sign, white title and
        /// white mark (PO, 2026-10-21). A pale tint on the light box read as no title box at all.
        /// </summary>
        public const string TitleBoxSprite = "Panel_Modal_Dark";
        /// <summary>Between a box's bottom edge and the buttons that stand free under it.</summary>
        private const float ButtonsGap = 16f;

        /// <summary>
        /// The title and its subtitle in a blue box of their own, with the company mark (PO, 2026-10-21: the
        /// module 0 questions apart from their options, like «Elige tu vehículo»). Returns where the
        /// box's visible edge ends, top-down, for what goes under it.
        /// </summary>
        private static float HeaderBox(Transform panel, UIBuilderKit kit)
        {
            RectTransform title = Find(panel, "Header/Title");
            RectTransform body = Find(panel, "Header/Body");
            // The box wraps what shows: the module 0 subtitles are off since 2026-10-29.
            RectTransform[] pieces = body.gameObject.activeSelf ? new[] { title, body } : new[] { title };
            Enclose box = Frame(panel, kit, pieces, HeaderMargin, HeaderFrameName, TitleBoxSprite);
            // Light on the blue, like the sign's text.
            TMP_Text titleText = title.GetComponent<TMP_Text>();
            titleText.color = Color.white;
            EditorUtility.SetDirty(titleText);
            TMP_Text bodyText = body.GetComponent<TMP_Text>();
            bodyText.color = UIBuilderKit.OnDark;
            EditorUtility.SetDirty(bodyText);
            UIBrand.Place(kit, titleText, (RectTransform)box.transform, HeaderMargin.x, true);
            // The box as wide as the title and its mark, not a fixed width with the rest empty
            // (PO, 2026-10-29). The width the builder gave the title is the most it may take.
            HugText hug = title.GetComponent<HugText>();
            if (hug == null)
                hug = title.gameObject.AddComponent<HugText>();
            float widest = title.sizeDelta.x - titleText.margin.x - titleText.margin.z;
            hug.Configure(titleText, null, widest, 0f, Vector2.zero, false);
            EditorUtility.SetDirty(hug);
            RectTransform frame = (RectTransform)box.transform;
            return -frame.anchoredPosition.y + frame.sizeDelta.y - PadOf(frame);
        }

        /// <summary>The box of options under the title's box, exactly as wide as it.</summary>
        private static Enclose ContentBox(Transform panel, UIBuilderKit kit, RectTransform[] pieces)
        {
            (float titleLeft, float titleRight) = SpanX(new[] { Find(panel, "Header/Title") }, panel);
            (float left, float right) = SpanX(pieces, panel);
            Vector4 margin = new Vector4(left - titleLeft + HeaderMargin.x, Margin.y, titleRight - right + HeaderMargin.z, Margin.w);
            return Frame(panel, kit, pieces, margin);
        }

        /// <summary>Leftmost and rightmost edge of the rects, in <paramref name="space"/>.</summary>
        private static (float left, float right) SpanX(RectTransform[] rects, Transform space)
        {
            Vector3[] corners = new Vector3[4];
            float left = float.MaxValue, right = float.MinValue;
            foreach (RectTransform rect in rects)
            {
                rect.GetWorldCorners(corners);
                foreach (Vector3 corner in corners)
                {
                    float x = space.InverseTransformPoint(corner).x;
                    left = Mathf.Min(left, x);
                    right = Mathf.Max(right, x);
                }
            }
            return (left, right);
        }

        /// <summary>
        /// Where the options stand, as the vehicle screen's name cards do (PO, 2026-10-21: «the vehicle
        /// screen has the right distribution»): the two cards either side of the mark painted on the door
        /// (it spans about 370–666), from 183 down. Crammed under the title they sat on the mark's bars.
        /// Centred at 185 and 845, a little outside the vehicle cards' 200 / 830: these cards are wider
        /// (256) and at 200 / 830 they came within 18 of the mark.
        /// </summary>
        private static readonly Vector2 OptionCentres = new Vector2(185f, 845f);
        private const float OptionsTop = 183f;

        /// <summary>
        /// A choice screen laid out like the vehicle screen: the question in a blue box only as wide as
        /// it needs, above the garage door like every module 0 title (TitleAboveDoor); the two cards
        /// either side of the door's mark, each card's content centred (the name sat in the bottom-left
        /// corner of an empty card); Back under the left card and Continue under the right one, in the
        /// band between the door's mark and its wordmark.
        /// </summary>
        private static void Choice(Transform panel, UIBuilderKit kit, float cardHeight, bool flagged, float headerWidth)
        {
            Header(panel, headerWidth);
            TitleAboveDoor(panel);
            // One line beside the mark: «¿Qué normas aplican a tus recorridos?» shrinks a little
            // instead of wrapping onto the subtitle.
            TMP_Text question = Find(panel, "Header/Title").GetComponent<TMP_Text>();
            question.textWrappingMode = TextWrappingModes.NoWrap;
            question.fontSizeMax = 34f;
            question.fontSizeMin = 28f;
            question.enableAutoSizing = true;
            EditorUtility.SetDirty(question);
            HeaderBox(panel, kit);

            Transform options = Find(panel, "Options");
            Require(options.childCount == 2, panel.name + " has " + options.childCount + " options; the layout is for two.");
            float[] lefts = new float[2], rights = new float[2];
            for (int i = 0; i < 2; i++)
            {
                RectTransform card = (RectTransform)options.GetChild(i);
                float pad = PadOf(card);
                float w = card.sizeDelta.x - 2f * pad;
                float centre = i == 0 ? OptionCentres.x : OptionCentres.y;
                SetVisibleX(card, centre - w * 0.5f, w);
                SetVisibleY(card, OptionsTop, cardHeight);
                lefts[i] = centre - w * 0.5f;
                rights[i] = centre + w * 0.5f;
                if (flagged)
                {
                    // The region's name over its flag: the rule-pack sentence read as fine print (PO, 2026-10-29).
                    InCard(card, "StateCaption", 60f, 12f, w - 60f, 26f);
                    InCard(card, "Title", 24f, 28f, w - 24f, 58f);
                    Find(card, "Description").gameObject.SetActive(false);
                    Flag(card, kit, LocationFlags[card.name], w * 0.5f, 64f);
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
            }
            // No box behind the options (PO): the one of an earlier pass goes.
            Transform optionsBox = panel.Find(FrameName);
            if (optionsBox != null)
                Object.DestroyImmediate(optionsBox.gameObject);
            ButtonsUnder(lefts[0], rights[1], OptionsTop + cardHeight + ButtonsGap, Find(panel, "BackButton"), Find(panel, "ContinueButton"),
                         NavButtonWidth, NavButtonWidth);
            NavIcon(Find(panel, "BackButton"), kit, "Icon_Back");
            NavIcon(Find(panel, "ContinueButton"), kit, "Icon_Continue");
        }

        /// <summary>
        /// The flag of each location card (PO, 2026-10-29): the United States, and South America as one
        /// collage for Latin America. Drawn from flagcdn.com's public-domain flags (Wikimedia) into
        /// circular badges, 256 px, in <c>UI/Icons</c>.
        /// </summary>
        private static readonly Dictionary<string, string> LocationFlags = new Dictionary<string, string>
        {
            { "Option_florida", "Flag_USA" },
            { "Option_bogota", "Flag_LatAm" },
        };

        /// <summary>A location card's flag badge, <see cref="FlagSize"/> across, centred under its name.</summary>
        private const float FlagSize = 64f;

        private static void Flag(RectTransform card, UIBuilderKit kit, string sprite, float centreX, float top)
        {
            Transform existing = card.Find("Flag");
            GameObject flag = existing != null ? existing.gameObject : UIBuilderKit.NewUi("Flag", card);
            InCard(card, "Flag", centreX - FlagSize * 0.5f, top, centreX + FlagSize * 0.5f, top + FlagSize);
            UnityEngine.UI.Image image = flag.GetComponent<UnityEngine.UI.Image>();
            if (image == null)
                image = flag.AddComponent<UnityEngine.UI.Image>();
            image.sprite = kit.S(sprite);
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
            EditorUtility.SetDirty(image);
        }

        /// <summary>
        /// The question's top on every module 0 screen: its blue box above the garage door, on the
        /// siding (PO, 2026-10-29), not over the door's top. Measured on the prefab: with the title
        /// alone the box's visible bottom lands at 48, and the door's white frame starts at about 72.
        /// </summary>
        private const float TitleTop = -32f;

        /// <summary>Where the comfort settings stood before the title moved up: they stay on the door.</summary>
        private const float ComfortSettingsTop = 170f;

        /// <summary>
        /// Back and Continue as icons, not words (PO, 2026-10-29). A chevron is universal for a step
        /// back or forth; the pill stays 88 x 56 so the target is as easy to hit with the ray as before.
        /// </summary>
        private const float NavButtonWidth = 88f;

        /// <summary>The title at <see cref="TitleTop"/> and the subtitle off: «no son relevantes» (PO, 2026-10-29).</summary>
        private static void TitleAboveDoor(Transform panel)
        {
            RectTransform title = Find(panel, "Header/Title");
            title.anchoredPosition = new Vector2(title.anchoredPosition.x, -TitleTop);
            EditorUtility.SetDirty(title);
            Find(panel, "Header/Body").gameObject.SetActive(false);
        }

        /// <summary>
        /// A free Back / Continue pill showing a white chevron instead of its label. The label stays
        /// (hidden) for <c>SelectableStyle</c>, which now paints the chevron with the label's colour:
        /// navy on the light Back, white on the coral Continue, grey while it is disabled.
        /// </summary>
        private static void NavIcon(RectTransform button, UIBuilderKit kit, string source)
        {
            TMP_Text label = button.Find("Label").GetComponent<TMP_Text>();
            label.gameObject.SetActive(false);
            Transform existing = button.Find("Icon");
            GameObject icon = existing != null ? existing.gameObject : UIBuilderKit.NewUi("Icon", button);
            RectTransform rect = (RectTransform)icon.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(30f, 30f);
            UnityEngine.UI.Image image = icon.GetComponent<UnityEngine.UI.Image>();
            if (image == null)
                image = icon.AddComponent<UnityEngine.UI.Image>();
            image.sprite = kit.S(UIStatusIcons.NavigationName(source));
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = label.color;
            SelectableStyle style = button.GetComponentInChildren<SelectableStyle>(true);
            if (style != null)
            {
                UIWire.Ref(style, "_icon", image);
                EditorUtility.SetDirty(style);
            }
            EditorUtility.SetDirty(image);
            EditorUtility.SetDirty(label);
        }


        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new System.InvalidOperationException(message);
        }

        /// <summary>Back on the box's left edge, the forward action on its right edge, free under it.</summary>
        /// <summary>Height and type of the free Back / forward buttons: at 51 and 17 they looked lost beside the box (PO, 2026-10-20).</summary>
        private const float FreeButtonHeight = 56f;
        private const float FreeButtonFont = 19f;

        private static void ButtonsUnder(Enclose box, RectTransform left, RectTransform right, float leftWidth = 150f, float rightWidth = 190f)
        {
            RectTransform frame = (RectTransform)box.transform;
            float pad = PadOf(frame);
            ButtonsUnder(frame.anchoredPosition.x + pad, frame.anchoredPosition.x + frame.sizeDelta.x - pad,
                         -frame.anchoredPosition.y + frame.sizeDelta.y - pad + ButtonsGap, left, right, leftWidth, rightWidth);
        }

        /// <summary>The same, between two visible edges and from a top, top-down.</summary>
        private static void ButtonsUnder(float boxLeft, float boxRight, float top, RectTransform left, RectTransform right,
                                         float leftWidth = 150f, float rightWidth = 190f)
        {
            foreach (RectTransform button in new[] { left, right })
            {
                UnityEngine.UI.Image pill = button.GetComponent<UnityEngine.UI.Image>();
                pill.pixelsPerUnitMultiplier = UIBuilderKit.PillMultiplier(pill.sprite != null ? pill.sprite.name : "Button_", FreeButtonHeight);
                float width = button == left ? leftWidth : rightWidth;
                SetVisibleX(button, button == left ? boxLeft : boxRight - width, width);
                SetVisibleY(button, top, FreeButtonHeight);
                TMP_Text label = button.Find("Label").GetComponent<TMP_Text>();
                label.fontSize = label.fontSizeMax = FreeButtonFont;
                EditorUtility.SetDirty(label);
                EditorUtility.SetDirty(pill);
            }
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
                float side = ((RectTransform)selectable.transform).sizeDelta.y * 0.1f;
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

        /// <summary>
        /// Slides <paramref name="pieces"/> sideways, together, so the group sits centred in
        /// <paramref name="panel"/> (their direct parent). Only x moves. Idempotent.
        /// </summary>
        private static void CenterIn(RectTransform panel, params RectTransform[] pieces)
        {
            float min = float.MaxValue, max = float.MinValue;
            Vector3[] corners = new Vector3[4];
            foreach (RectTransform piece in pieces)
            {
                piece.GetWorldCorners(corners);
                min = Mathf.Min(min, panel.InverseTransformPoint(corners[0]).x);
                max = Mathf.Max(max, panel.InverseTransformPoint(corners[2]).x);
            }
            float shift = panel.rect.center.x - (min + max) * 0.5f;
            if (Mathf.Abs(shift) < 0.01f)
                return;
            foreach (RectTransform piece in pieces)
            {
                piece.anchoredPosition += new Vector2(shift, 0f);
                EditorUtility.SetDirty(piece);
            }
        }

        private static void Align(RectTransform text, HorizontalAlignmentOptions alignment)
        {
            TMP_Text tmp = text.GetComponent<TMP_Text>();
            tmp.horizontalAlignment = alignment;
            EditorUtility.SetDirty(tmp);
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
