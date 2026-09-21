using TMPro;
using UnityEngine;

namespace RideSafe.UI.EditorTools
{
    /// <summary>Module 2 — mounting, familiarization and debrief (storyboard frames 14–18).</summary>
    internal sealed partial class UIModules
    {
        private void BuildModule02()
        {
            BeginModule("PF_Module02_UI");
            Passthrough(Panel("Passthrough", true));
            Mounting(Panel("Mounting", true));
            Familiarization(Panel("Familiarization"));
            Debrief(Panel("Debrief", true));
            EndModule("PF_Module02_UI");
        }

        // 14 — the room returns; no interaction.
        private void Passthrough(Transform panel)
        {
            DarkInstruction(panel, "Remain in your configured riding position.",
                            "Your room is visible again. The handlebar stays centered.",
                            123f, 41f, 879f, 156f, 83f, 124f, 32f);
            _ui.DarkLabel(panel, "CenteredLabel", "Centered", 501f, 343f, true);

            Transform badge = K.Slice(panel, "Debrief_Label", 45f, 466f, 222f, 510f, 3.2f, Color.white, "PassthroughBadge").transform;
            float pad = UIBuilderKit.PadFor(3.2f);
            K.Dot(badge, 26f + pad, 22f + pad, 5f, UIBuilderKit.Mint, "StatusDot");
            K.TL(badge, "Passthrough on", K.LabelOnDark.With(15f), 43f + pad, 22f + pad, 127f, "Text");

            _ui.Subtitle(panel, "Subtitles on · “Remain in your configured riding position.”", 501f, 502f);
        }

        // 15 — step card, mount outlines, mounted-brake continuation (deliberately no Button).
        private void Mounting(Transform panel)
        {
            DarkInstruction(panel, "Center the handlebar.",
                            "Bring it square to your riding position and stay seated or standing as configured.",
                            147f, 19f, 855f, 119f, 53f, 89f, 28f);

            Transform card = K.Slice(panel, "Card_Default", 305f, 130f, 695f, 222f, 2.8f, Color.white, "StepCard").transform;
            Vector2 o = Origin(305f, 130f, 2.8f);
            TMP_Text counter = K.TL(card, "Step 2 of 4", K.Caps, 332f + o.x, 155f + o.y, 100f, "Counter");
            ProgressView segments = _ui.Segments(card, 428f + o.x, 56f, 6f, 4, 2, 155f + o.y);
            K.Icon(card, "Debrief_Step_Active", 346f + o.x, 189f + o.y, 24f, null, "ActiveIcon");
            TMP_Text title = K.TL(card, "Center handlebar", K.Body.With(22f), 375f + o.x, 189f + o.y, 300f, "Title");
            Card(card.gameObject, counter, title, null, segments);

            MountOutline(panel, "LeftMount", 167f, 302f, 226f, "Left mount", 226f);
            MountOutline(panel, "RightMount", 699f, 834f, 775f, "Right mount", 770f);

            Transform prompt = K.Slice(panel, "Debrief_Label", 623f, 437f, 957f, 492f, 2.2f, Color.white, "HoldPrompt").transform;
            Vector2 p = Origin(623f, 437f, 2.2f);
            K.Dot(prompt, 665f + p.x, 465f + p.y, 11f, Color.white, "RingOuter");
            K.Dot(prompt, 665f + p.x, 465f + p.y, 9f, UIBuilderKit.Hex("#0A2A55"), "RingInner");
            TMP_Text hold = K.TL(prompt, "Hold both brakes to continue", K.LabelOnDark.With(17f), 689f + p.x, 461f + p.y, 260f, "Title");
            ProgressView holdProgress = _ui.ProgressBar(prompt, "HoldProgress", 689f + p.x, 930f + p.x, 480f + p.y, 3f, 0f,
                                                        new Color(1f, 1f, 1f, 0.2f));
            Card(prompt.gameObject, null, hold, null, holdProgress);

            _ui.DarkLabel(panel, "MountedInputNote", "Mounted input only — no free-hand button", 671f, 495f, false, K.LabelOnDark.With(13f));
            _ui.Subtitle(panel, "Subtitles on · “Center the handlebar.”", 501f, 505f);
        }

        private void MountOutline(Transform panel, string name, float x0, float x1, float arrowX, string label, float labelX)
        {
            Transform group = UIBuilderKit.Group(panel, name);
            K.Slice(group, "Focus_Ring_Mint", x0, 281f, x1, 361f, 2.6f, Color.white, "Outline");
            K.TC(group, "↓", K.H2.With(22f, UIBuilderKit.Mint), arrowX, 268f, 30f, "Arrow");
            _ui.DarkLabel(group, "Label", label, labelX, 368f, true, K.LabelOnDark.With(13f));
        }

        // 16 — one instruction at a time; brief completion chip.
        private void Familiarization(Transform panel)
        {
            Header(panel, "Brake smoothly to a complete stop.", "Use both levers together. There is no rush.", 62f, 96f, 32f);

            GameObject chip = UIBuilderKit.NewUi("CompletionChip", panel);
            float multiplier = 160f / 43f;
            int pad = Mathf.RoundToInt(UIBuilderKit.PadFor(multiplier));
            UIBuilderKit.PlaceTopCenter((RectTransform)chip.transform, 501f, 147f - pad);
            K.AddBackground(chip, "Status_Pill_Default", multiplier, Color.white);

            UnityEngine.UI.HorizontalLayoutGroup layout = chip.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20 + pad, 22 + pad, 10 + pad, 10 + pad);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            UnityEngine.UI.ContentSizeFitter fitter = chip.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.horizontalFit = fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

            GameObject icon = UIBuilderKit.NewUi("Icon", chip.transform);
            UnityEngine.UI.Image iconImage = icon.AddComponent<UnityEngine.UI.Image>();
            iconImage.sprite = K.S("Icon_Selected_CoralCircle");
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            UnityEngine.UI.LayoutElement iconLayout = icon.AddComponent<UnityEngine.UI.LayoutElement>();
            iconLayout.preferredWidth = iconLayout.preferredHeight = 29f;

            TMP_Text text = K.AddText(UIBuilderKit.NewUi("Text", chip.transform), "Steering held steady — done", K.LabelBold,
                                      TextAlignmentOptions.Left);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            Card(chip, null, text, null, null);

            _ui.Subtitle(panel, "Subtitles on · “Brake smoothly to a complete stop.”", 501f, 450f);
        }

        // 18 — scene paused, one beat card, one dominant overlay, repair strip.
        private void Debrief(Transform panel)
        {
            _ui.DarkLabel(panel, "ScenePausedBadge", "Scene paused", 37f, 33f, false, K.CapsOnDark.With(12f));

            Transform overlay = UIBuilderKit.Group(panel, "BeatOverlay");
            K.Slice(overlay, "Panel_Light_Base", 151f, 193f, 383f, 394f, 3f, new Color(0.55f, 0.75f, 0.95f, 0.10f), "HiddenArea");
            K.Slice(overlay, "Focus_Ring_Mint", 151f, 193f, 383f, 394f, 7f, new Color(1f, 1f, 1f, 0.55f), "HiddenAreaOutline");
            _ui.DarkLabel(overlay, "HiddenLabel", "Hidden from you", 159f, 167f, false, K.LabelOnDark.With(12f));
            Transform cue = K.Slice(overlay, "Card_Default", 393f, 211f, 525f, 237f, 4f, UIBuilderKit.Mint, "CueLabel").transform;
            float cuePad = UIBuilderKit.PadFor(4f);
            K.TC(cue, "Earliest usable cue", K.LabelBold.With(12f, UIBuilderKit.BeamNavy), 66f + cuePad, 13f + cuePad, 130f, "Text");
            K.Dot(overlay, 386f, 253f, 6f, UIBuilderKit.Mint, "CueMarker");

            Transform card = K.Slice(panel, "Card_Default", 635f, 109f, 965f, 351f, 2.8f, Color.white, "BeatCard").transform;
            Vector2 o = Origin(635f, 109f, 2.8f);
            TMP_Text counter = K.TL(card, "Beat 1 of 4", K.Caps, 658f + o.x, 139f + o.y, 100f, "Counter");
            ProgressView segments = _ui.Segments(card, 750f + o.x, 45f, 4f, 4, 1, 139f + o.y);
            TMP_Text title = K.TL(card, "What happened", K.H2, 658f + o.x, 174f + o.y, 280f, "Title");
            TMP_Text body = K.Text(card, "You crossed the garage mouth while the van still cut your view of it. The wheel was the first thing that could be seen.",
                                   K.Body, 658f + o.x, 198f + o.y, 942f + o.x, 300f + o.y, TextAlignmentOptions.TopLeft, 10f, "Body");
            K.Dot(card, 662f + o.x, 320f + o.y, 4f, UIBuilderKit.Mint, "FooterDot");
            K.TL(card, "Right mounted button · next beat", K.Small, 676f + o.x, 320f + o.y, 260f, "Footer");
            Card(card.gameObject, counter, title, body, segments, "Beat {0} of {1}");

            _ui.Subtitle(panel, "Subtitles on · “You crossed the garage mouth while the van still cut your view of it.”", 501f, 428f);
            RepairStrip(panel);
        }

        private void RepairStrip(Transform panel)
        {
            Transform strip = K.Slice(panel, "Debrief_Label", 209f, 476f, 793f, 518f, 3f, Color.white, "RepairSequence").transform;
            float pad = UIBuilderKit.PadFor(3f);
            float ox = -209f + pad;
            float cy = 21f + pad;

            K.TL(strip, "Repair sequence", K.CapsOnDark.With(11f), 228f + ox, cy, 140f, "Caption");
            K.Pill(strip, "Status_Pill_Default", 363.5f + ox, cy - 10f, 365f + ox, cy + 10f, new Color(1f, 1f, 1f, 0.3f), "Divider");

            string[] stages = { "Aid", "Supported", "Fades", "Confirm", "Transfer" };
            float[] widths = { 30f, 70f, 42f, 56f, 60f };
            UnityEngine.UI.Image[] dots = new UnityEngine.UI.Image[stages.Length];
            TMP_Text[] labels = new TMP_Text[stages.Length];
            Color inactive = UIBuilderKit.Hex("#8C9EB3");

            float x = 377f;
            for (int i = 0; i < stages.Length; i++)
            {
                bool active = i == 0;
                // A near-coral tint keeps every dot the same sliced pill, so any can be recoloured at runtime.
                dots[i] = K.Dot(strip, x + ox, cy, 3.5f, active ? UIBuilderKit.Hex("#FF6143") : inactive, "Dot · " + stages[i]);
                labels[i] = K.TL(strip, stages[i], K.LabelOnDark.With(12.5f, active ? Color.white : UIBuilderKit.Hex("#B8C6D6")),
                                 x + 8f + ox, cy, widths[i] + 10f, "Stage · " + stages[i]);
                x += 8f + widths[i];
                if (i < stages.Length - 1)
                {
                    K.TC(strip, "→", K.Small.With(12f, inactive), x + 8f + ox, cy, 16f, "Arrow");
                    x += 24f;
                }
            }

            ProgressView view = strip.gameObject.AddComponent<ProgressView>();
            UIWire.Int(view, "_mode", (int)ProgressView.Mode.Stages);
            UIWire.Refs(view, "_marks", dots);
            UIWire.Refs(view, "_labels", labels);
            UIWire.Ref(view, "_activeSprite", K.S("Status_Pill_Default"));
            UIWire.Ref(view, "_inactiveSprite", K.S("Status_Pill_Default"));
            UIWire.Col(view, "_activeColor", UIBuilderKit.Coral);
            UIWire.Col(view, "_inactiveColor", inactive);
            UIWire.Int(view, "_current", 1);
        }
    }
}
