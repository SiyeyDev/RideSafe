using TMPro;
using UnityEditor;
using UnityEngine;

namespace RideSafe.UI.EditorTools
{
    /// <summary>
    /// The company mark on the panels (Cindy, 2026-10-21): the bars-and-dot symbol, without the
    /// wordmark, at the top left of each box on the line of its title. Navy on the light boxes,
    /// white on the blue ones (the station sign, the module 0 titles). One mark in view, by its title.
    /// <para>
    /// Only the symbol: the full logo at title height was what «Mi Elección» had, 54 px wide with
    /// an unreadable «RideSafe» beside it. The garage door already carries the wordmark large.
    /// </para>
    /// </summary>
    internal static class UIBrand
    {
        public const string Folder = "Assets/_Project/UI/Brand";
        public const string NavyMark = "RideSafe_Mark_Navy";
        public const string WhiteMark = "RideSafe_Mark_White";
        public const string ObjectName = "BrandMark";

        /// <summary>
        /// One size on every panel (PO, 2026-10-21): a mark that grew with each title's font read as a
        /// different logo per screen. 4.5 cm tall (PO), in kit units: the 1002 x 556 frame at 0.002 m.
        /// </summary>
        public const float MarkHeight = 22.5f;
        /// <summary>Between the mark and the title's text.</summary>
        public const float MarkGap = 10f;

        public static readonly string[] Paths = { Folder + "/" + NavyMark + ".png", Folder + "/" + WhiteMark + ".png" };

        /// <summary>Sprites, uncompressed at the source like the other atlas sources: the atlas compresses once.</summary>
        public static void EnsureImported()
        {
            foreach (string path in Paths)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    throw new System.IO.FileNotFoundException("Company mark missing", path);
                if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single &&
                    importer.textureCompression == TextureImporterCompression.Uncompressed && importer.mipmapEnabled)
                    continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        /// <summary>
        /// The mark at the left of the box's content, centred on the title's first line, and the
        /// title kept clear of it: its text starts after the mark, and a centred title gives up the
        /// same on the right so it stays centred in the box. Idempotent: everything is measured
        /// from <paramref name="alignTo"/>, never from what an earlier pass left on the title.
        /// </summary>
        /// <param name="alignTo">The rect whose left edge (plus <paramref name="inset"/>) is where the box's content starts.</param>
        /// <param name="alsoClear">Other texts on the mark's rows, kept clear of it the same way (the title under a tag line).</param>
        public static TitleMark Place(UIBuilderKit kit, TMP_Text title, RectTransform alignTo, float inset, bool onDark,
                                      params TMP_Text[] alsoClear)
        {
            RectTransform titleRect = title.rectTransform;
            Vector3 contentLeft = alignTo.TransformPoint(new Vector3(alignTo.rect.xMin + PadOf(alignTo) + inset, 0f, 0f));
            float offset = titleRect.InverseTransformPoint(contentLeft).x - titleRect.rect.xMin;

            Sprite sprite = kit.S(onDark ? WhiteMark : NavyMark);
            float height = MarkHeight;
            float width = sprite != null ? height * sprite.rect.width / sprite.rect.height : height * 1.82f;
            float gap = MarkGap;

            Transform parent = titleRect.parent;
            Transform existing = parent.Find(ObjectName);
            GameObject go = existing != null ? existing.gameObject : UIBuilderKit.NewUi(ObjectName, parent);
            go.layer = title.gameObject.layer;
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            // Right after its title, the same on every pass.
            int after = titleRect.GetSiblingIndex();
            if (rect.GetSiblingIndex() != after + 1)
                rect.SetSiblingIndex(rect.GetSiblingIndex() < after ? after : after + 1);

            UnityEngine.UI.Image image = go.GetComponent<UnityEngine.UI.Image>();
            if (image == null)
                image = go.AddComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.type = UnityEngine.UI.Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;

            // In a stacked list (the sidebar) the mark is no row of its own.
            if (parent.GetComponent<UnityEngine.UI.LayoutGroup>() != null)
            {
                UnityEngine.UI.LayoutElement element = go.GetComponent<UnityEngine.UI.LayoutElement>();
                if (element == null)
                    element = go.AddComponent<UnityEngine.UI.LayoutElement>();
                element.ignoreLayout = true;
                EditorUtility.SetDirty(element);
            }

            TitleMark mark = go.GetComponent<TitleMark>();
            if (mark == null)
                mark = go.AddComponent<TitleMark>();
            mark.Configure(title, offset);

            KeepClear(title, contentLeft, width + gap);
            foreach (TMP_Text text in alsoClear)
                KeepClear(text, contentLeft, width + gap);
            mark.Refresh();

            EditorUtility.SetDirty(image);
            EditorUtility.SetDirty(mark);
            EditorUtility.SetDirty(rect);
            return mark;
        }

        /// <summary>
        /// The text starts after the mark; a centred one gives up the same on the right and stays centred.
        /// </summary>
        private static void KeepClear(TMP_Text text, Vector3 contentLeft, float markAndGap)
        {
            RectTransform rect = text.rectTransform;
            float offset = rect.InverseTransformPoint(contentLeft).x - rect.rect.xMin;
            float clear = Mathf.Max(0f, offset + markAndGap);
            Vector4 margin = text.margin;
            margin.x = clear;
            // A text that was centred on an earlier pass keeps no right margin once it is left-aligned.
            margin.z = text.horizontalAlignment == HorizontalAlignmentOptions.Center ? clear : 0f;
            text.margin = margin;
            text.ForceMeshUpdate();

            float room = rect.rect.width - margin.x - margin.z;
            float needs = text.GetPreferredValues(text.text).x;
            if (text.textWrappingMode == TextWrappingModes.NoWrap && !text.enableAutoSizing && needs > room)
                Debug.LogWarning($"[RideSafe.UI] '{text.text}' needs {needs:F0} beside the mark and has {room:F0}.", text);
            EditorUtility.SetDirty(text);
        }

        /// <summary>
        /// No mark beside this title, and its text back to the box's full width. For panels that share
        /// the view with the one that carries the title (Cindy, 2026-10-21: one logo in view, by the title).
        /// </summary>
        public static void Remove(TMP_Text title)
        {
            Transform existing = title.rectTransform.parent.Find(ObjectName);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);
            Vector4 margin = title.margin;
            if (title.horizontalAlignment == HorizontalAlignmentOptions.Center)
                margin.z = 0f;
            margin.x = 0f;
            title.margin = margin;
            EditorUtility.SetDirty(title);
        }

        private static float PadOf(RectTransform rect)
        {
            UnityEngine.UI.Image image = rect.GetComponent<UnityEngine.UI.Image>();
            return image != null && image.enabled && image.type == UnityEngine.UI.Image.Type.Sliced && image.pixelsPerUnitMultiplier > 0f
                ? UIBuilderKit.PadFor(image.pixelsPerUnitMultiplier)
                : 0f;
        }
    }
}
