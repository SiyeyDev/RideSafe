using TMPro;
using UnityEngine;

namespace RideSafe.UI.EditorTools
{
    /// <summary>
    /// Builds UI elements inline (no per-element prefabs): buttons, selection cards,
    /// segmented options, auto-sized labels, stacked panels, row templates and progress.
    /// All coordinates are storyboard pixels relative to the parent's top-left.
    /// </summary>
    internal sealed class UIElements
    {
        private readonly UIBuilderKit _kit;

        public UIElements(UIBuilderKit kit)
        {
            _kit = kit;
        }

        public UIBuilderKit Kit => _kit;

        #region Buttons and toggles

        /// <summary>
        /// Pill button with atlas sprite-swap states (Default / Hover mint ring / Pressed /
        /// Disabled). No Selected sprite, so a clicked button does not stay "hovered".
        /// </summary>
        public UnityEngine.UI.Button Button(Transform parent, string name, string label, bool primary,
                                            float x0, float y0, float x1, float y1,
                                            float fontSize = 17f, bool interactable = true)
        {
            string prefix = primary ? "Button_Primary_" : "Button_Secondary_";
            UnityEngine.UI.Image image = _kit.Pill(parent, prefix + "Default", x0, y0, x1, y1, Color.white, name, true);

            UnityEngine.UI.Button button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.transition = UnityEngine.UI.Selectable.Transition.SpriteSwap;
            button.spriteState = SpriteStates(prefix);
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            button.interactable = interactable;

            Color labelColor = primary ? Color.white : UIBuilderKit.WordNavy;
            TMP_Text text = _kit.AddText(UIBuilderKit.Stretch(image.transform, "Label").gameObject, label,
                                         _kit.ButtonText.With(fontSize, labelColor), TextAlignmentOptions.Center);

            SelectableStyle style = image.gameObject.AddComponent<SelectableStyle>();
            UIWire.Ref(style, "_selectable", button);
            UIWire.Ref(style, "_label", text);
            UIWire.Col(style, "_labelColor", labelColor);
            UIWire.Col(style, "_disabledLabelColor", primary ? UIBuilderKit.DisabledPrimaryText : UIBuilderKit.DisabledSecondaryText);
            if (!interactable)
                text.color = primary ? UIBuilderKit.DisabledPrimaryText : UIBuilderKit.DisabledSecondaryText;
            return button;
        }

        private UnityEngine.UI.SpriteState SpriteStates(string prefix)
        {
            return new UnityEngine.UI.SpriteState
            {
                highlightedSprite = _kit.S(prefix + "Hover"),
                pressedSprite = _kit.S(prefix + "Pressed"),
                disabledSprite = _kit.S(prefix + "Disabled")
            };
        }

        /// <summary>
        /// Selection card (language, jurisdiction, vehicle label): Default → Hover (mint) →
        /// Selected (coral, fold, check, "SELECTED"). The check icon is the Toggle graphic.
        /// </summary>
        public UnityEngine.UI.Toggle Card(Transform parent, string name, string title, UnityEngine.UI.ToggleGroup group,
                                          float x0, float y0, float x1, float y1)
        {
            const float multiplier = 2.8f;
            UnityEngine.UI.Image background = _kit.Slice(parent, "Card_Default", x0, y0, x1, y1, multiplier, Color.white, name, true);
            Transform card = background.transform;
            float pad = UIBuilderKit.PadFor(multiplier);
            float w = x1 - x0;

            UnityEngine.UI.Toggle toggle = card.gameObject.AddComponent<UnityEngine.UI.Toggle>();
            toggle.targetGraphic = background;
            toggle.transition = UnityEngine.UI.Selectable.Transition.SpriteSwap;
            toggle.spriteState = new UnityEngine.UI.SpriteState
            {
                highlightedSprite = _kit.S("Card_Hover"),
                pressedSprite = _kit.S("Card_Hover"),
                disabledSprite = _kit.S("Card_Disabled")
            };
            toggle.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            toggle.group = group;
            toggle.isOn = false;

            UnityEngine.UI.Image check = _kit.Icon(card, "Icon_Selected_CoralCircle", pad + w - 30f, pad + 30f, 24f, null, "Check");
            toggle.graphic = check;

            _kit.TL(card, title, _kit.H2.With(26f, UIBuilderKit.WordNavy), pad + 27f, pad + 99f, w - 40f, "Title");
            TMP_Text caption = _kit.TL(card, string.Empty, _kit.Caps, pad + 27f, pad + 127f, w - 40f, "StateCaption");
            caption.gameObject.SetActive(false);
            _kit.Text(card, string.Empty, _kit.Small.With(14f), pad + 28f, pad + 142f, pad + w - 34f, pad + 188f,
                      TextAlignmentOptions.TopLeft, 0f, "Description");

            SelectableStyle style = card.gameObject.AddComponent<SelectableStyle>();
            UIWire.Ref(style, "_selectable", toggle);
            UIWire.Ref(style, "_background", background);
            UIWire.Ref(style, "_offSprite", _kit.S("Card_Default"));
            UIWire.Ref(style, "_onSprite", _kit.S("Card_Selected"));
            UIWire.Ref(style, "_caption", caption);
            UIWire.Str(style, "_onCaption", "Selected");
            return toggle;
        }

        /// <summary>Repositions a card child relative to the card's VISIBLE top-left.</summary>
        public static void PlaceInCard(Component card, string child, float dx0, float dy0, float dx1, float dy1)
        {
            float pad = UIBuilderKit.PadFor(2.8f);
            UIBuilderKit.Place((RectTransform)card.transform.Find(child), dx0 + pad, dy0 + pad, dx1 + pad, dy1 + pad);
        }

        /// <summary>Segmented choice: OFF looks secondary, ON looks primary, hover follows the look.</summary>
        public UnityEngine.UI.Toggle Segmented(Transform parent, string name, string label, UnityEngine.UI.ToggleGroup group,
                                               bool isOn, float x0, float y0, float x1, float y1)
        {
            string current = isOn ? "Button_Primary_Default" : "Button_Secondary_Default";
            UnityEngine.UI.Image background = _kit.Pill(parent, current, x0, y0, x1, y1, Color.white, name, true);

            UnityEngine.UI.Toggle toggle = background.gameObject.AddComponent<UnityEngine.UI.Toggle>();
            toggle.targetGraphic = background;
            toggle.transition = UnityEngine.UI.Selectable.Transition.SpriteSwap;
            toggle.spriteState = SpriteStates(isOn ? "Button_Primary_" : "Button_Secondary_");
            toggle.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            toggle.group = group;
            toggle.isOn = isOn;

            TMP_Text text = _kit.AddText(UIBuilderKit.Stretch(background.transform, "Label").gameObject, label,
                                         _kit.ButtonText.With(15f, isOn ? Color.white : UIBuilderKit.WordNavy),
                                         TextAlignmentOptions.Center);

            SelectableStyle style = background.gameObject.AddComponent<SelectableStyle>();
            UIWire.Ref(style, "_selectable", toggle);
            UIWire.Ref(style, "_label", text);
            UIWire.Col(style, "_labelColor", UIBuilderKit.WordNavy);
            UIWire.Col(style, "_onLabelColor", Color.white);
            UIWire.Ref(style, "_background", background);
            UIWire.Ref(style, "_offSprite", _kit.S("Button_Secondary_Default"));
            UIWire.Ref(style, "_onSprite", _kit.S("Button_Primary_Default"));
            UIWire.Bool(style, "_swapSpriteState", true);
            UIWire.SpriteStateField(style, "_offSpriteState", SpriteStates("Button_Secondary_"));
            UIWire.SpriteStateField(style, "_onSpriteState", SpriteStates("Button_Primary_"));
            return toggle;
        }

        #endregion

        #region Labels and panels

        /// <summary>
        /// Label whose background grows with its text. The background sits on a child that
        /// layout ignores, so the size comes from the text alone.
        /// </summary>
        public GameObject AutoLabel(Transform parent, string name, string text, TextStyle style, string sprite,
                                    float multiplier, float padX, float padY, float x, float y0, bool centered,
                                    bool withView = false)
        {
            GameObject root = UIBuilderKit.NewUi(name, parent);
            _kit.AddBackground(root, sprite, multiplier, Color.white);
            int pad = Mathf.RoundToInt(UIBuilderKit.PadFor(multiplier));

            UnityEngine.UI.HorizontalLayoutGroup layout = root.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.padding = new RectOffset(Mathf.RoundToInt(padX) + pad, Mathf.RoundToInt(padX) + pad,
                                            Mathf.RoundToInt(padY) + pad, Mathf.RoundToInt(padY) + pad);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            UnityEngine.UI.ContentSizeFitter fitter = root.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

            TMP_Text label = _kit.AddText(UIBuilderKit.NewUi("Text", root.transform), text, style, TextAlignmentOptions.Center);
            label.textWrappingMode = TextWrappingModes.NoWrap;

            RectTransform rect = (RectTransform)root.transform;
            if (centered)
            {
                UIBuilderKit.PlaceTopCenter(rect, x, y0 - pad);
            }
            else
            {
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(x - pad, -(y0 - pad));
            }

            if (withView)
            {
                CardView view = root.AddComponent<CardView>();
                UIWire.Ref(view, "_title", label);
            }
            return root;
        }

        public GameObject DarkLabel(Transform parent, string name, string text, float x, float y0, bool centered,
                                    TextStyle style = null)
            => AutoLabel(parent, name, text, style ?? _kit.LabelOnDark, "Debrief_Label", 4f, 10f, 5f, x, y0, centered);

        /// <summary>Subtitle strip with a CardView so gameplay can Show/Hide the spoken line.</summary>
        public GameObject Subtitle(Transform parent, string text, float centerX, float y0)
            => AutoLabel(parent, "Subtitle", text, _kit.Subtitle, "Panel_Subtitle", 2.2f, 14f, 6f, centerX, y0, true, true);

        /// <summary>Vertical panel that grows downward with its content.</summary>
        public GameObject StackPanel(Transform parent, string name, string sprite, float multiplier,
                                     float x0, float y0, float width, RectOffset inner, float spacing)
        {
            float pad = UIBuilderKit.PadFor(multiplier);
            RectTransform rect = UIBuilderKit.Rect(parent, name, x0 - pad, y0 - pad, x0 + width + pad, y0 + 100f);
            _kit.AddBackground(rect.gameObject, sprite, multiplier, Color.white);

            UnityEngine.UI.VerticalLayoutGroup layout = rect.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            int p = Mathf.RoundToInt(pad);
            layout.padding = new RectOffset(inner.left + p, inner.right + p, inner.top + p, inner.bottom + p);
            layout.spacing = spacing;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            rect.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit =
                UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            return rect.gameObject;
        }

        public TMP_Text StackText(Transform parent, string name, string text, TextStyle style)
            => _kit.AddText(UIBuilderKit.NewUi(name, parent), text, style, TextAlignmentOptions.TopLeft);

        public void StackDivider(Transform parent)
        {
            GameObject divider = UIBuilderKit.NewUi("Divider", parent);
            _kit.AddSliced(divider, "Status_Pill_Default", 100f, UIBuilderKit.Divider);
            UnityEngine.UI.LayoutElement layout = divider.AddComponent<UnityEngine.UI.LayoutElement>();
            layout.minHeight = layout.preferredHeight = 1.5f;
        }

        public static RectTransform StackList(Transform parent, string name, float spacing)
        {
            GameObject list = UIBuilderKit.NewUi(name, parent);
            UnityEngine.UI.VerticalLayoutGroup layout = list.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return (RectTransform)list.transform;
        }

        #endregion

        #region Row templates

        /// <summary>
        /// Checklist row blueprint: coral check, label, tag. The whole row is the Button, so
        /// pointing highlights it and the trigger removes the item.
        /// </summary>
        public RectTransform ChecklistRowTemplate(Transform container, string sampleLabel)
        {
            GameObject row = UIBuilderKit.NewUi("RowTemplate", container);
            UnityEngine.UI.LayoutElement layout = row.AddComponent<UnityEngine.UI.LayoutElement>();
            layout.minHeight = layout.preferredHeight = 40f;

            UnityEngine.UI.Image highlight = _kit.AddSliced(row, "Checklist_Row_Selected", 3f, Color.white, true);
            UnityEngine.UI.Button button = row.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = highlight;
            button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            button.colors = new UnityEngine.UI.ColorBlock
            {
                normalColor = new Color(1f, 1f, 1f, 0f),
                highlightedColor = Color.white,
                pressedColor = new Color(0.9f, 0.93f, 0.96f, 1f),
                selectedColor = new Color(1f, 1f, 1f, 0f),
                disabledColor = new Color(1f, 1f, 1f, 0f),
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };

            RectTransform check = _kit.Icon(row.transform, "Icon_Selected_CoralCircle", 0f, 0f, 26f, null, "Check").rectTransform;
            check.anchorMin = check.anchorMax = check.pivot = new Vector2(0f, 0.5f);
            check.anchoredPosition = new Vector2(3f, 0f);

            _kit.AddText(UIBuilderKit.Stretch(row.transform, "Label", 44f, 0f, 90f, 0f).gameObject, sampleLabel, _kit.Row,
                         TextAlignmentOptions.Left);
            _kit.AddText(UIBuilderKit.Stretch(row.transform, "Tag", 240f, 0f, 8f, 0f).gameObject, "Added", _kit.Caps.With(11f),
                         TextAlignmentOptions.Right);
            return (RectTransform)row.transform;
        }

        /// <summary>Status row blueprint: glyph + title + optional detail, stacked by layout.</summary>
        public RectTransform StatusRowTemplate(Transform container)
        {
            GameObject row = UIBuilderKit.NewUi("RowTemplate", container);
            UnityEngine.UI.HorizontalLayoutGroup layout = row.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(0, 0, 3, 3);

            GameObject iconGo = UIBuilderKit.NewUi("Icon", row.transform);
            UnityEngine.UI.Image icon = iconGo.AddComponent<UnityEngine.UI.Image>();
            icon.sprite = _kit.S("Status_Independent");
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            UnityEngine.UI.LayoutElement iconLayout = iconGo.AddComponent<UnityEngine.UI.LayoutElement>();
            iconLayout.minWidth = iconLayout.preferredWidth = iconLayout.minHeight = iconLayout.preferredHeight = 36f;

            GameObject column = UIBuilderKit.NewUi("Text", row.transform);
            UnityEngine.UI.VerticalLayoutGroup stack = column.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            stack.spacing = 1f;
            stack.childControlWidth = stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
            stack.padding = new RectOffset(0, 0, 7, 0);
            column.AddComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1f;

            _kit.AddText(UIBuilderKit.NewUi("Title", column.transform), "Item", _kit.Body, TextAlignmentOptions.TopLeft);
            _kit.AddText(UIBuilderKit.NewUi("Detail", column.transform), "Detail", _kit.Small, TextAlignmentOptions.TopLeft);
            return (RectTransform)row.transform;
        }

        /// <summary>Sprites for the four-state key, in ItemStatus order.</summary>
        public Sprite[] StatusSprites() => new[]
        {
            _kit.S("Status_Independent"), _kit.S("Status_Supported"), _kit.S("Status_Attention"), _kit.S("Status_Retry")
        };

        #endregion

        #region Progress

        /// <summary>Pill track + anchor-driven pill fill, driven by a ProgressView in Bar mode.</summary>
        public ProgressView ProgressBar(Transform parent, string name, float x0, float x1, float cy, float height,
                                        float value, Color track)
        {
            RectTransform root = UIBuilderKit.Rect(parent, name, x0, cy - height * 0.5f, x1, cy + height * 0.5f);
            _kit.Pill(root, "Status_Pill_Default", 0f, 0f, x1 - x0, height, track, "Track");

            RectTransform fill = (RectTransform)UIBuilderKit.NewUi("Fill", root).transform;
            float multiplier = UIBuilderKit.PillMultiplier("Button_Primary_Default", height);
            float pad = UIBuilderKit.PadFor(multiplier);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(value, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.offsetMin = new Vector2(-pad, -pad);
            fill.offsetMax = new Vector2(pad, pad);
            _kit.AddSliced(fill.gameObject, "Button_Primary_Default", multiplier, Color.white);

            ProgressView view = root.gameObject.AddComponent<ProgressView>();
            UIWire.Int(view, "_mode", (int)ProgressView.Mode.Bar);
            UIWire.Ref(view, "_fill", fill);
            UIWire.Float(view, "_value", value);
            return view;
        }

        /// <summary>Row of pill segments ("Step 2 of 4").</summary>
        public ProgressView Segments(Transform parent, float x0, float width, float gap, int count, int done, float cy)
        {
            RectTransform root = UIBuilderKit.Rect(parent, "Segments", x0, cy - 2f, x0 + count * width + (count - 1) * gap, cy + 2f);
            UnityEngine.UI.Image[] marks = new UnityEngine.UI.Image[count];
            for (int i = 0; i < count; i++)
            {
                float a = i * (width + gap);
                bool active = i < done;
                marks[i] = _kit.Pill(root, active ? "Button_Primary_Default" : "Status_Pill_Default", a, 0f, a + width, 4f,
                                     active ? Color.white : UIBuilderKit.Track, "Segment " + (i + 1));
            }

            ProgressView view = root.gameObject.AddComponent<ProgressView>();
            UIWire.Int(view, "_mode", (int)ProgressView.Mode.Segments);
            UIWire.Refs(view, "_marks", marks);
            UIWire.Ref(view, "_activeSprite", _kit.S("Button_Primary_Default"));
            UIWire.Ref(view, "_inactiveSprite", _kit.S("Status_Pill_Default"));
            UIWire.Col(view, "_inactiveColor", UIBuilderKit.Track);
            UIWire.Int(view, "_current", done);
            return view;
        }

        #endregion
    }
}
