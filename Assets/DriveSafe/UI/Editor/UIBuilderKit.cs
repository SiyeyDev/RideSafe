using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace RideSafe.UI.EditorTools
{
    /// <summary>Text style: font asset, size, colour, tracking, caps.</summary>
    internal sealed class TextStyle
    {
        public readonly TMP_FontAsset Font;
        public readonly float Size;
        public readonly Color Color;
        public readonly float Spacing;
        public readonly bool Upper;

        public TextStyle(TMP_FontAsset font, float size, Color color, float spacing = 0f, bool upper = false)
        {
            Font = font;
            Size = size;
            Color = color;
            Spacing = spacing;
            Upper = upper;
        }

        public TextStyle With(float? size = null, Color? color = null) =>
            new TextStyle(Font, size ?? Size, color ?? Color, Spacing, Upper);
    }

    /// <summary>
    /// Construction kit shared by every prefab builder: atlas sprite lookup, Florida Dusk
    /// styles, and RectTransform/Image/TMP primitives in storyboard coordinates.
    /// <para>
    /// Coordinates are storyboard pixels of a 1002 x 556 frame with a top-left origin.
    /// Every atlas cell has 16 px of transparent padding around its art, so sliced helpers
    /// inflate the rect by 16/multiplier to keep the VISIBLE art on the given coordinates.
    /// </para>
    /// </summary>
    internal sealed class UIBuilderKit
    {
        public const float FrameW = 1002f;
        public const float FrameH = 556f;
        public const float WorldScale = 0.002f;
        private const float k_Pad = 16f;

        public static readonly Color BeamNavy = Hex("#002554");
        public static readonly Color WordNavy = Hex("#07335F");
        public static readonly Color Coral = Hex("#FF6042");
        public static readonly Color Mint = Hex("#A9E2D4");
        public static readonly Color Muted = Hex("#4A6480");
        public static readonly Color ClearWhite = Hex("#F8FAFC");
        public static readonly Color OnDark = Hex("#D5DEE8");
        public static readonly Color Track = Hex("#D5DEE8");
        public static readonly Color Divider = Hex("#07335F", 0.16f);
        public static readonly Color DisabledPrimaryText = Hex("#6E8196");
        public static readonly Color DisabledSecondaryText = Hex("#E8ECF1");

        private readonly Dictionary<string, Sprite> _sprites;
        public readonly Sprite Logo;
        public readonly List<string> MissingSprites = new List<string>();

        public readonly TextStyle H1, H1OnDark, Sub, SubOnDark, H2, Body, BodyOnDark, Small, Caps, CapsOnDark,
                                  ButtonText, LabelBold, LabelOnDark, Subtitle, Row;

        public UIBuilderKit(Dictionary<string, Sprite> sprites, Sprite logo,
                            TMP_FontAsset manropeBold, TMP_FontAsset manropeSemiBold,
                            TMP_FontAsset interRegular, TMP_FontAsset interMedium, TMP_FontAsset interSemiBold)
        {
            _sprites = sprites;
            Logo = logo;

            H1 = new TextStyle(manropeBold, 34f, BeamNavy);
            H1OnDark = new TextStyle(manropeBold, 30f, Color.white);
            Sub = new TextStyle(interRegular, 18f, Muted);
            SubOnDark = new TextStyle(interRegular, 17f, OnDark);
            H2 = new TextStyle(manropeSemiBold, 21f, BeamNavy);
            Body = new TextStyle(interRegular, 16f, WordNavy);
            BodyOnDark = new TextStyle(interMedium, 16f, Color.white);
            Small = new TextStyle(interRegular, 13f, Muted);
            Caps = new TextStyle(interSemiBold, 12f, Muted, 12f, true);
            CapsOnDark = new TextStyle(interSemiBold, 12f, Color.white, 12f, true);
            ButtonText = new TextStyle(interSemiBold, 17f, Color.white);
            LabelBold = new TextStyle(interSemiBold, 15f, WordNavy);
            LabelOnDark = new TextStyle(interSemiBold, 14f, Color.white);
            Subtitle = new TextStyle(interSemiBold, 14f, Color.white);
            Row = new TextStyle(interMedium, 17f, WordNavy);
        }

        #region Sprites

        public Sprite S(string name)
        {
            Sprite sprite;
            if (_sprites.TryGetValue(name, out sprite))
                return sprite;
            if (!MissingSprites.Contains(name))
                MissingSprites.Add(name);
            return null;
        }

        /// <summary>Pixels-per-unit multiplier that keeps pill caps semicircular at a visible height.</summary>
        public static float PillMultiplier(string sprite, float visibleHeight)
        {
            float artHeight = sprite.StartsWith("Button_") ? 224f : 160f;
            return artHeight / Mathf.Max(1f, visibleHeight);
        }

        public static float PadFor(float multiplier) => k_Pad / multiplier;

        private static float GlyphRatio(string sprite)
        {
            if (sprite.StartsWith("Status_"))
                return 256f / 176f;
            switch (sprite)
            {
                case "Icon_Selected_CoralCircle":
                case "Decision_Point_Coral":
                    return 128f / 96f;
                case "Debrief_Step_Active":
                    return 128f / 48f;
                default:
                    return 128f / 80f;
            }
        }

        #endregion

        #region RectTransforms

        public static GameObject NewUi(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            if (parent != null)
                go.transform.SetParent(parent, false);
            go.layer = LayerMask.NameToLayer("UI");
            return go;
        }

        /// <summary>Top-left anchored rect at storyboard coordinates, relative to the parent's top-left.</summary>
        public static RectTransform Rect(Transform parent, string name, float x0, float y0, float x1, float y1)
        {
            RectTransform rect = (RectTransform)NewUi(name, parent).transform;
            Place(rect, x0, y0, x1, y1);
            return rect;
        }

        public static void Place(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x0, -y0);
            rect.sizeDelta = new Vector2(x1 - x0, y1 - y0);
        }

        /// <summary>Anchored top-left of the parent but pivoted at its own top-centre (for auto-width labels).</summary>
        public static void PlaceTopCenter(RectTransform rect, float centerX, float y0)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(centerX, -y0);
        }

        public static RectTransform Stretch(Transform parent, string name, float left = 0f, float top = 0f,
                                            float right = 0f, float bottom = 0f)
        {
            RectTransform rect = (RectTransform)NewUi(name, parent).transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        /// <summary>Full-frame grouping node so children keep absolute storyboard coordinates.</summary>
        public static Transform Group(Transform parent, string name) =>
            Rect(parent, name, 0f, 0f, FrameW, FrameH);

        #endregion

        #region Images

        /// <summary>9-sliced atlas sprite with its VISIBLE art on (x0,y0)-(x1,y1).</summary>
        public UnityEngine.UI.Image Slice(Transform parent, string sprite, float x0, float y0, float x1, float y1,
                                          float multiplier, Color color, string name = null, bool raycast = false)
        {
            float pad = PadFor(multiplier);
            RectTransform rect = Rect(parent, name ?? sprite, x0 - pad, y0 - pad, x1 + pad, y1 + pad);
            return AddSliced(rect.gameObject, sprite, multiplier, color, raycast);
        }

        public UnityEngine.UI.Image Pill(Transform parent, string sprite, float x0, float y0, float x1, float y1,
                                         Color color, string name = null, bool raycast = false)
        {
            return Slice(parent, sprite, x0, y0, x1, y1, PillMultiplier(sprite, y1 - y0), color, name, raycast);
        }

        /// <summary>Adds a sliced atlas Image to an existing object (for layout-driven panels).</summary>
        public UnityEngine.UI.Image AddSliced(GameObject go, string sprite, float multiplier, Color color, bool raycast = false)
        {
            UnityEngine.UI.Image image = go.GetComponent<UnityEngine.UI.Image>();
            if (image == null)
                image = go.AddComponent<UnityEngine.UI.Image>();
            image.sprite = S(sprite);
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = multiplier;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        /// <summary>
        /// Sliced background on a stretched child that layout ignores. Put on the layout root
        /// itself, a sliced Image reports its border sum as preferred size (e.g. 112 x 112) with
        /// the same priority as the layout group, and uGUI keeps the larger value, so every
        /// auto-sized label would grow to the sprite's border size.
        /// </summary>
        public UnityEngine.UI.Image AddBackground(GameObject go, string sprite, float multiplier, Color color)
        {
            RectTransform background = Stretch(go.transform, "Background");
            background.SetAsFirstSibling();
            background.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
            return AddSliced(background.gameObject, sprite, multiplier, color);
        }

        /// <summary>Non-sliced glyph sized so the VISIBLE glyph has diameter <paramref name="d"/>.</summary>
        public UnityEngine.UI.Image Icon(Transform parent, string sprite, float cx, float cy, float d, Color? color = null,
                                         string name = null)
        {
            float size = d * GlyphRatio(sprite);
            RectTransform rect = Rect(parent, name ?? sprite, cx - size * 0.5f, cy - size * 0.5f,
                                      cx + size * 0.5f, cy + size * 0.5f);
            UnityEngine.UI.Image image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = S(sprite);
            image.preserveAspect = true;
            image.color = color ?? Color.white;
            image.raycastTarget = false;
            return image;
        }

        public UnityEngine.UI.Image Picture(Transform parent, string name, Sprite sprite, float x0, float y0, float x1, float y1)
        {
            RectTransform rect = Rect(parent, name, x0, y0, x1, y1);
            UnityEngine.UI.Image image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Round marker: coral uses the atlas coral dot, anything else tints a pill.</summary>
        public UnityEngine.UI.Image Dot(Transform parent, float cx, float cy, float radius, Color color, string name = "Dot")
        {
            if (color == Coral)
                return Icon(parent, "Debrief_Step_Active", cx, cy, radius * 2f, null, name);
            return Pill(parent, "Status_Pill_Default", cx - radius, cy - radius, cx + radius, cy + radius, color, name);
        }

        public UnityEngine.UI.Image Line(Transform parent, float x0, float x1, float y, Color? color = null)
        {
            return Pill(parent, "Status_Pill_Default", x0, y - 0.75f, x1, y + 0.75f, color ?? Divider, "Divider");
        }

        #endregion

        #region Text

        public TextMeshProUGUI Text(Transform parent, string text, TextStyle style, float x0, float y0, float x1, float y1,
                                    TextAlignmentOptions alignment, float lineSpacing = 0f, string name = null)
        {
            RectTransform rect = Rect(parent, name ?? NameFor(text), x0, y0, x1, y1);
            return AddText(rect.gameObject, text, style, alignment, lineSpacing);
        }

        public TextMeshProUGUI TC(Transform parent, string text, TextStyle style, float cx, float cy, float width, string name = null)
            => Text(parent, text, style, cx - width * 0.5f, cy - style.Size, cx + width * 0.5f, cy + style.Size,
                    TextAlignmentOptions.Center, 0f, name);

        public TextMeshProUGUI TL(Transform parent, string text, TextStyle style, float x, float cy, float width, string name = null)
            => Text(parent, text, style, x, cy - style.Size, x + width, cy + style.Size, TextAlignmentOptions.Left, 0f, name);

        public TextMeshProUGUI TR(Transform parent, string text, TextStyle style, float x, float cy, float width, string name = null)
            => Text(parent, text, style, x - width, cy - style.Size, x, cy + style.Size, TextAlignmentOptions.Right, 0f, name);

        public TextMeshProUGUI AddText(GameObject go, string text, TextStyle style, TextAlignmentOptions alignment,
                                       float lineSpacing = 0f)
        {
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
                tmp = go.AddComponent<TextMeshProUGUI>();
            ApplyStyle(tmp, style);
            // Caps come from the font style, not the string, so runtime text stays in caps too.
            tmp.text = text;
            tmp.alignment = alignment;
            tmp.lineSpacing = lineSpacing;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            tmp.margin = Vector4.zero;
            return tmp;
        }

        public static void ApplyStyle(TMP_Text tmp, TextStyle style)
        {
            tmp.font = style.Font;
            tmp.fontSize = style.Size;
            tmp.color = style.Color;
            tmp.characterSpacing = style.Spacing;
            tmp.fontStyle = style.Upper ? FontStyles.UpperCase : FontStyles.Normal;
        }

        private static string NameFor(string text)
        {
            string clean = text.Replace("\n", " ");
            return "Text · " + (clean.Length > 28 ? clean.Substring(0, 28) + "…" : clean);
        }

        #endregion

        #region Layout

        /// <summary>
        /// Recomputes every layout group / content size fitter under <paramref name="root"/>,
        /// innermost first. Needed before saving a prefab: uGUI only settles layout during a
        /// canvas update, and LayoutRebuilder skips any subtree whose root has no layout
        /// controller, so auto-sized labels would otherwise be saved at their default size.
        /// </summary>
        public static void RebuildLayouts(GameObject root)
        {
            List<RectTransform> targets = new List<RectTransform>();
            foreach (UnityEngine.UI.ILayoutController controller in root.GetComponentsInChildren<UnityEngine.UI.ILayoutController>(true))
            {
                RectTransform rect = ((Component)controller).transform as RectTransform;
                if (rect != null && !targets.Contains(rect))
                    targets.Add(rect);
            }
            targets.Sort((a, b) => Depth(b).CompareTo(Depth(a)));

            for (int pass = 0; pass < 2; pass++)
            {
                foreach (RectTransform rect in targets)
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            }
        }

        private static int Depth(Transform transform)
        {
            int depth = 0;
            while (transform.parent != null)
            {
                transform = transform.parent;
                depth++;
            }
            return depth;
        }

        #endregion

        #region Prefab instances

        public static GameObject Instance(GameObject prefab, Transform parent, string name)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            return instance;
        }

        public static T Find<T>(GameObject root, string path) where T : Component
        {
            Transform child = root.transform.Find(path);
            if (child == null)
                throw new System.InvalidOperationException("'" + root.name + "' has no child '" + path + "'.");
            T component = child.GetComponent<T>();
            if (component == null)
                throw new System.InvalidOperationException("'" + root.name + "/" + path + "' has no " + typeof(T).Name + ".");
            return component;
        }

        public static void SetText(GameObject root, string path, string text)
        {
            TMP_Text tmp = Find<TMP_Text>(root, path);
            tmp.text = text;
        }

        #endregion

        public static Color Hex(string hex)
        {
            Color color;
            return ColorUtility.TryParseHtmlString(hex, out color) ? color : Color.magenta;
        }

        public static Color Hex(string hex, float alpha)
        {
            Color color = Hex(hex);
            color.a = alpha;
            return color;
        }
    }
}
