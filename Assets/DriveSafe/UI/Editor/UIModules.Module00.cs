using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace RideSafe.UI.EditorTools
{
    /// <summary>Module 0 — pre-module configuration (storyboard frames 01–05).</summary>
    internal sealed partial class UIModules
    {
        private void BuildModule00()
        {
            BeginModule("PF_Module00_UI");
            Welcome(Panel("Welcome", true));
            Choice(Panel("Language"), "Choose your language", "You can change this later.", 77f, 118f,
                new[] { ("en", "English", "", 183f, 236f, 487f, 398f),
                        ("es", "Español", "", 514f, 236f, 819f, 398f) });
            Choice(Panel("Jurisdiction"), "Which rules apply to your riding?",
                "This sets the rule pack the whole session is judged against.", 75f, 113f,
                new[] { ("florida", "Florida", "United States · state e-bike and scooter rule pack", 186f, 206f, 487f, 418f),
                        ("bogota", "Bogotá", "Colombia · city cycling and micromobility rule pack", 516f, 206f, 814f, 418f) });
            Vehicle(Panel("Vehicle"));
            Comfort(Panel("Comfort"));
            EndModule("PF_Module00_UI");
        }

        // 01 — logo plate, tagline, Begin.
        private void Welcome(Transform panel)
        {
            Transform plate = K.Slice(panel, "Panel_Light_Base", 282f, 42f, 720f, 330f, 3f, Color.white, "LogoPlate").transform;
            if (K.Logo != null)
                K.Picture(plate, "Logo", K.Logo, 73f, 95f, 375f, 203f);
            K.TC(panel, "Better decisions in motion.", K.Sub.With(20f, UIBuilderKit.OnDark), 501f, 375f, 600f, "Tagline");
            _ui.Button(panel, "BeginButton", "Begin", true, 414f, 418f, 590f, 486f, 22f);
        }

        // 02 / 03 — two cards in a ToggleGroup; Continue waits for a choice.
        private void Choice(Transform panel, string title, string subtitle, float titleY, float subtitleY,
                            (string id, string label, string description, float x0, float y0, float x1, float y1)[] options)
        {
            Header(panel, title, subtitle, titleY, subtitleY);
            Transform group = UIBuilderKit.Group(panel, "Options");
            UnityEngine.UI.ToggleGroup toggleGroup = group.gameObject.AddComponent<UnityEngine.UI.ToggleGroup>();
            toggleGroup.allowSwitchOff = true;

            List<ChoicePanel.Option> choices = new List<ChoicePanel.Option>();
            foreach (var option in options)
            {
                UnityEngine.UI.Toggle card = _ui.Card(group, "Option_" + option.id, option.label, toggleGroup,
                                                      option.x0, option.y0, option.x1, option.y1);
                float h = option.y1 - option.y0;
                if (!string.IsNullOrEmpty(option.description))
                {
                    UIElements.PlaceInCard(card, "Title", 28f, h - 104f, 280f, h - 72f);
                    UIElements.PlaceInCard(card, "StateCaption", 28f, 22f, 200f, 40f);
                    UIElements.PlaceInCard(card, "Description", 28f, h - 70f, 270f, h - 22f);
                    UIBuilderKit.SetText(card.gameObject, "Description", option.description);
                    UIBuilderKit.Find<TMP_Text>(card.gameObject, "Title").fontSize = 22f;
                }
                else
                {
                    UIElements.PlaceInCard(card, "Title", 27f, h - 80f, 270f, h - 46f);
                    UIElements.PlaceInCard(card, "StateCaption", 27f, h - 44f, 270f, h - 26f);
                }
                choices.Add(new ChoicePanel.Option { Toggle = card, Id = option.id, DisplayName = option.label });
            }

            UnityEngine.UI.Button back = _ui.Button(panel, "BackButton", "Back", false, 121f, 453f, 223f, 504f);
            UnityEngine.UI.Button next = _ui.Button(panel, "ContinueButton", "Continue", true, 738f, 455f, 881f, 504f, 17f, false);

            ChoicePanel choice = panel.gameObject.AddComponent<ChoicePanel>();
            UIWire.Ref(choice, "_group", toggleGroup);
            UIWire.Options(choice, "_options", choices);
            UIWire.Ref(choice, "_continueButton", next);
            UIWire.Ref(choice, "_backButton", back);
        }

        // 04 — vehicle name labels are the selectable targets; explicit confirmation bar.
        private void Vehicle(Transform panel)
        {
            Header(panel, "Choose your vehicle", "Look at each vehicle before you choose.", 62f, 100f);
            Transform group = UIBuilderKit.Group(panel, "VehicleLabels");
            UnityEngine.UI.ToggleGroup toggleGroup = group.gameObject.AddComponent<UnityEngine.UI.ToggleGroup>();
            toggleGroup.allowSwitchOff = true;

            List<ChoicePanel.Option> choices = new List<ChoicePanel.Option>
            {
                VehicleLabel(group, toggleGroup, "ebike", "E-bike", "Pedal assist · seated", 180f, 183f, 354f, 257f),
                VehicleLabel(group, toggleGroup, "escooter", "Standing e-scooter", "Standing · throttle", 640f, 185f, 886f, 251f)
            };

            Transform bar = K.Pill(panel, "Status_Pill_Default", 196f, 440f, 806f, 514f, Color.white, "ConfirmationBar").transform;
            float pad = UIBuilderKit.PadFor(UIBuilderKit.PillMultiplier("Status_Pill_Default", 74f));
            TMP_Text sentence = K.TL(bar, "E-bike selected —", K.Body.With(16f, UIBuilderKit.Muted), 19f + pad, 37f + pad, 160f, "SelectedText");
            K.TL(bar, "confirm to continue?", K.LabelBold.With(16f), 174f + pad, 37f + pad, 175f, "Prompt");
            UnityEngine.UI.Button change = _ui.Button(bar, "ChangeButton", "Change", false, 350f + pad, 15f + pad, 460f + pad, 60f + pad, 15f);
            UnityEngine.UI.Button confirm = _ui.Button(bar, "ConfirmButton", "Confirm", true, 476f + pad, 15f + pad, 590f + pad, 60f + pad, 15f);
            bar.gameObject.SetActive(false);

            ChoicePanel choice = panel.gameObject.AddComponent<ChoicePanel>();
            UIWire.Ref(choice, "_group", toggleGroup);
            UIWire.Options(choice, "_options", choices);
            UIWire.Ref(choice, "_continueButton", confirm);
            UIWire.Ref(choice, "_confirmationBar", bar.gameObject);
            UIWire.Ref(choice, "_confirmationText", sentence);
            UIWire.Ref(choice, "_changeButton", change);
        }

        private ChoicePanel.Option VehicleLabel(Transform parent, UnityEngine.UI.ToggleGroup group, string id, string title,
                                                string subtitle, float x0, float y0, float x1, float y1)
        {
            UnityEngine.UI.Toggle card = _ui.Card(parent, "Option_" + id, title, group, x0, y0, x1, y1);
            float w = x1 - x0;
            float h = y1 - y0;
            UIElements.PlaceInCard(card, "Title", 8f, 8f, w - 8f, 40f);
            UIElements.PlaceInCard(card, "Description", 8f, 40f, w - 8f, h - 6f);
            UIElements.PlaceInCard(card, "StateCaption", 8f, h + 4f, w - 8f, h + 20f);

            TMP_Text titleText = UIBuilderKit.Find<TMP_Text>(card.gameObject, "Title");
            titleText.fontSize = 22f;
            titleText.alignment = TextAlignmentOptions.Center;
            TMP_Text description = UIBuilderKit.Find<TMP_Text>(card.gameObject, "Description");
            description.text = subtitle;
            description.fontSize = 13f;
            description.alignment = TextAlignmentOptions.Top;
            UIBuilderKit.Find<TMP_Text>(card.gameObject, "StateCaption").alignment = TextAlignmentOptions.Center;
            // A small label has no room for the corner check; the coral card state carries selection.
            UIBuilderKit.Find<UnityEngine.UI.Image>(card.gameObject, "Check").rectTransform.localScale = Vector3.one * 0.75f;

            return new ChoicePanel.Option { Toggle = card, Id = id, DisplayName = title };
        }

        // 05 — posture, subtitles, text size, dominant hand.
        private void Comfort(Transform panel)
        {
            Header(panel, "Set up your comfort", "Nothing here affects your results.", 64f, 103f);
            Transform box = K.Slice(panel, "Panel_Modal_Light", 113f, 148f, 889f, 431f, 2.4f, Color.white, "SettingsPanel").transform;
            Vector2 o = Origin(113f, 148f, 2.4f);

            K.Line(box, 141f + o.x, 861f + o.x, 223f + o.y);
            K.Line(box, 141f + o.x, 861f + o.x, 289f + o.y);
            K.Line(box, 141f + o.x, 861f + o.x, 355f + o.y);

            SettingLabel(box, o, 180f, "Riding posture", "Sets eye height and handlebar reach");
            UnityEngine.UI.ToggleGroup posture = UIBuilderKit.Group(box, "PostureGroup").gameObject.AddComponent<UnityEngine.UI.ToggleGroup>();
            _ui.Segmented(posture.transform, "SeatedOption", "Seated", posture, true, 624f + o.x, 168f + o.y, 740f + o.x, 210f + o.y);
            UnityEngine.UI.Toggle standing = _ui.Segmented(posture.transform, "StandingOption", "Standing", posture, false,
                                                           750f + o.x, 168f + o.y, 861f + o.x, 210f + o.y);

            SettingLabel(box, o, 246f, "Subtitles", "Every spoken line, placed below the instruction panel");
            UnityEngine.UI.Toggle subtitles = Switch(box, 795f + o.x, 241f + o.y, 861f + o.x, 273f + o.y, 785f + o.x, 257f + o.y);

            SettingLabel(box, o, 312f, "Text size", "Scales all world-space type together");
            UnityEngine.UI.Slider textSize = Slider(box, 578f + o.x, 317f + o.y, 797f + o.x, 328f + o.y);
            TMP_Text textSizeValue = K.TL(box, "110%", K.LabelBold.With(15f), 809f + o.x, 323f + o.y, 60f, "TextSizeValue");

            SettingLabel(box, o, 379f, "Dominant hand", "Mirrors mounted controls and the ray hand");
            UnityEngine.UI.ToggleGroup hands = UIBuilderKit.Group(box, "HandGroup").gameObject.AddComponent<UnityEngine.UI.ToggleGroup>();
            UnityEngine.UI.Toggle left = _ui.Segmented(hands.transform, "LeftOption", "Left", hands, false, 675f + o.x, 369f + o.y, 748f + o.x, 411f + o.y);
            _ui.Segmented(hands.transform, "RightOption", "Right", hands, true, 758f + o.x, 369f + o.y, 861f + o.x, 411f + o.y);

            _ui.Button(panel, "BackButton", "Back", false, 121f, 463f, 223f, 514f);
            UnityEngine.UI.Button start = _ui.Button(panel, "StartButton", "Start Module 1", true, 696f, 465f, 881f, 514f);

            ComfortSettingsPanel comfort = panel.gameObject.AddComponent<ComfortSettingsPanel>();
            UIWire.Ref(comfort, "_standing", standing);
            UIWire.Ref(comfort, "_subtitles", subtitles);
            UIWire.Ref(comfort, "_textSize", textSize);
            UIWire.Ref(comfort, "_textSizeValue", textSizeValue);
            UIWire.Ref(comfort, "_leftHand", left);
            UIWire.Ref(comfort, "_startButton", start);
        }

        private void SettingLabel(Transform box, Vector2 o, float y, string title, string detail)
        {
            string id = title.Replace(" ", string.Empty);
            K.TL(box, title, K.LabelBold.With(17f, UIBuilderKit.BeamNavy), 141f + o.x, y + o.y, 400f, "Label_" + id);
            K.TL(box, detail, K.Small, 141f + o.x, y + 22f + o.y, 420f, "Detail_" + id);
        }

        /// <summary>On/off switch: coral track when on, grey when off, knob slides.</summary>
        private UnityEngine.UI.Toggle Switch(Transform parent, float x0, float y0, float x1, float y1, float stateX, float stateY)
        {
            UnityEngine.UI.Image track = K.Pill(parent, "Button_Primary_Default", x0, y0, x1, y1, Color.white, "SubtitlesSwitch", true);
            float h = y1 - y0;
            float pad = UIBuilderKit.PadFor(track.pixelsPerUnitMultiplier);
            RectTransform knob = K.Dot(track.transform, pad + (x1 - x0) - h * 0.5f, pad + h * 0.5f, h * 0.5f - 3f, Color.white, "Knob").rectTransform;

            UnityEngine.UI.Toggle toggle = track.gameObject.AddComponent<UnityEngine.UI.Toggle>();
            toggle.targetGraphic = track;
            toggle.transition = UnityEngine.UI.Selectable.Transition.None;
            toggle.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            toggle.isOn = true;

            TMP_Text state = K.TR(parent, "On", K.LabelBold.With(14f), stateX, stateY, 40f, "SubtitlesState");

            SelectableStyle style = track.gameObject.AddComponent<SelectableStyle>();
            UIWire.Ref(style, "_selectable", toggle);
            UIWire.Ref(style, "_background", track);
            UIWire.Ref(style, "_offSprite", K.S("Button_Secondary_Disabled"));
            UIWire.Ref(style, "_onSprite", K.S("Button_Primary_Default"));
            UIWire.Ref(style, "_knob", knob);
            UIWire.Vec2(style, "_knobOn", knob.anchoredPosition);
            UIWire.Vec2(style, "_knobOff", knob.anchoredPosition - new Vector2((x1 - x0) - h, 0f));
            UIWire.Ref(style, "_stateText", state);
            return toggle;
        }

        /// <summary>Text-size slider, 80–140 %, whole numbers, default 110 %.</summary>
        private UnityEngine.UI.Slider Slider(Transform parent, float x0, float y0, float x1, float y1)
        {
            RectTransform root = UIBuilderKit.Rect(parent, "TextSizeSlider", x0, y0 - 8f, x1, y1 + 8f);
            float barH = y1 - y0;
            K.Pill(root, "Status_Pill_Default", 0f, 8f, x1 - x0, 8f + barH, UIBuilderKit.Hex("#D6ECE6"), "Background", true);

            RectTransform fill = UIBuilderKit.Stretch(UIBuilderKit.Stretch(root, "Fill Area", 0f, 8f, 0f, 8f), "Fill");
            float fillMultiplier = UIBuilderKit.PillMultiplier("Button_Primary_Default", barH);
            float fillPad = UIBuilderKit.PadFor(fillMultiplier);
            fill.offsetMin = new Vector2(-fillPad, -fillPad);
            fill.offsetMax = new Vector2(fillPad, fillPad);
            K.AddSliced(fill.gameObject, "Button_Primary_Default", fillMultiplier, Color.white);

            RectTransform handle = (RectTransform)UIBuilderKit.NewUi("Handle", UIBuilderKit.Stretch(root, "Handle Slide Area")).transform;
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = new Vector2(0f, 1f);
            handle.sizeDelta = new Vector2(24f, 0f);
            UnityEngine.UI.Image hit = handle.gameObject.AddComponent<UnityEngine.UI.Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            K.Dot(handle, 12f, 13.5f, 11f, Color.white, "Knob");
            K.Icon(handle, "Decision_Point_Coral", 12f, 13.5f, 24f, null, "Ring");

            UnityEngine.UI.Slider slider = root.gameObject.AddComponent<UnityEngine.UI.Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = hit;
            slider.minValue = 80f;
            slider.maxValue = 140f;
            slider.wholeNumbers = true;
            slider.value = 110f;
            slider.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            return slider;
        }
    }
}
