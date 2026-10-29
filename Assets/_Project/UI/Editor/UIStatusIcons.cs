using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RideSafe.UI.EditorTools
{
    /// <summary>
    /// White copies of the five status glyphs of atlas B, for the traffic-light colours of the
    /// report and the go/no-go (PO, 2026-10-20: green, yellow and red on the icons only).
    /// <para>
    /// The atlas draws them navy (and Retry coral), and an Image tint multiplies: a navy glyph
    /// tinted green comes out nearly black. Here the glyph's ink becomes white, so the tint is the
    /// colour, and its light inner detail (the check of a filled disc) becomes transparent, so it
    /// still reads as a cut-out over the light panel. One PNG per glyph, packed by the UI
    /// SpriteAtlas like the rest. Idempotent.
    /// </para>
    /// </summary>
    internal static class UIStatusIcons
    {
        public const string Folder = "Assets/_Project/UI/StatusIcons";
        private const string AtlasB = "Assets/_Project/UI/RideSafe_UI_Atlas_B_4096.png";

        public static readonly string[] Sources =
            { "Status_Independent", "Status_Supported", "Status_Ready", "Status_Retry", "Status_Attention" };

        /// <summary>
        /// Glyphs the atlas does not have, drawn here in the style of <c>Status_Independent</c> (a
        /// filled disc with the sign cut out): the report's three states are a green check, an X
        /// and a dash (PO, 2026-10-29). The X replaces the red ring with a bar of <c>Status_Retry</c>,
        /// which read as a «!», and the dash the yellow ringed check of <c>Status_Ready</c>, which
        /// read as a second «correct».
        /// </summary>
        public static readonly string[] Drawn = { "Status_Wrong", "Status_NotNeeded" };

        /// <summary>
        /// The atlas's navigation chevrons, as white copies in <c>UI/Icons</c> (PO, 2026-10-29: icons
        /// instead of words on the module 0 Back / Continue). Navy in the atlas, they could not be
        /// tinted white for the coral Continue; white, <c>SelectableStyle</c> gives them the label's colour.
        /// </summary>
        public static readonly string[] Navigation = { "Icon_Back", "Icon_Continue" };

        public static string NavigationName(string source) => source.Replace("Icon_", "IconMono_");

        /// <summary>Writes the white <see cref="Navigation"/> chevrons. Idempotent.</summary>
        public static void BuildNavigation()
        {
            Texture2D sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            sheet.LoadImage(File.ReadAllBytes(AtlasB));
            try
            {
                foreach (string source in Navigation)
                {
                    Rect r = Find(source).rect;
                    Color[] pixels = sheet.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);
                    for (int i = 0; i < pixels.Length; i++)
                        pixels[i] = Ink(pixels[i]);
                    Texture2D icon = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGBA32, false);
                    icon.SetPixels(pixels);
                    icon.Apply();
                    string path = UIComposition.IconsFolder + "/" + NavigationName(source) + ".png";
                    File.WriteAllBytes(path, icon.EncodeToPNG());
                    Object.DestroyImmediate(icon);
                    AssetDatabase.ImportAsset(path);
                    Configure(path);
                }
            }
            finally
            {
                Object.DestroyImmediate(sheet);
            }
        }

        /// <summary>Every status glyph with a white copy in <see cref="Folder"/>.</summary>
        public static IEnumerable<string> All => Sources.Concat(Drawn);

        public static string MonoName(string source) => source.Replace("Status_", "StatusMono_");

        // Measured on StatusMono_Independent: 256 px, disc 40..216 (radius 88), check stroke ~20.
        private const int GlyphSize = 256;
        private const float DiscRadius = 88f;
        private const float StrokeHalf = 10f;

        [MenuItem("RideSafe/UI/Crear iconos de estado para el semáforo")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/_Project/UI", "StatusIcons");

            Texture2D sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            sheet.LoadImage(File.ReadAllBytes(AtlasB));
            try
            {
                foreach (string source in Sources)
                {
                    Sprite sprite = Find(source);
                    Rect r = sprite.rect;
                    Color[] pixels = sheet.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);
                    for (int i = 0; i < pixels.Length; i++)
                        pixels[i] = Ink(pixels[i]);
                    Texture2D icon = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGBA32, false);
                    icon.SetPixels(pixels);
                    icon.Apply();
                    string path = Folder + "/" + MonoName(source) + ".png";
                    File.WriteAllBytes(path, icon.EncodeToPNG());
                    Object.DestroyImmediate(icon);
                    AssetDatabase.ImportAsset(path);
                    Configure(path);
                }
            }
            finally
            {
                Object.DestroyImmediate(sheet);
            }
            DrawGlyphs();
            Debug.Log("[RideSafe.UI] Status icons written to " + Folder);
        }

        /// <summary>Writes the <see cref="Drawn"/> glyphs. Idempotent.</summary>
        public static void DrawGlyphs()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/_Project/UI", "StatusIcons");
            const float arm = 32f;   // the X's arms end 32 px off centre on each axis
            Write("Status_Wrong", new[] { (new Vector2(-arm, -arm), new Vector2(arm, arm)), (new Vector2(-arm, arm), new Vector2(arm, -arm)) });
            Write("Status_NotNeeded", new[] { (new Vector2(-38f, 0f), new Vector2(38f, 0f)) });
        }

        /// <summary>A white disc with round-capped strokes cut out of it, anti-aliased by distance.</summary>
        private static void Write(string glyph, (Vector2 A, Vector2 B)[] strokes)
        {
            Color[] pixels = new Color[GlyphSize * GlyphSize];
            float half = GlyphSize * 0.5f;
            for (int y = 0; y < GlyphSize; y++)
            for (int x = 0; x < GlyphSize; x++)
            {
                Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                float disc = Mathf.Clamp01(DiscRadius - p.magnitude + 0.5f);
                float cut = 0f;
                foreach ((Vector2 a, Vector2 b) in strokes)
                    cut = Mathf.Max(cut, Mathf.Clamp01(StrokeHalf - DistanceToSegment(p, a, b) + 0.5f));
                pixels[y * GlyphSize + x] = new Color(1f, 1f, 1f, disc * (1f - cut));
            }

            Texture2D icon = new Texture2D(GlyphSize, GlyphSize, TextureFormat.RGBA32, false);
            icon.SetPixels(pixels);
            icon.Apply();
            string path = Folder + "/" + MonoName(glyph) + ".png";
            File.WriteAllBytes(path, icon.EncodeToPNG());
            Object.DestroyImmediate(icon);
            AssetDatabase.ImportAsset(path);
            Configure(path);
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + t * ab);
        }

        /// <summary>Dark or saturated ink becomes white (keeping its alpha); light pixels become transparent.</summary>
        internal static Color Ink(Color c)
        {
            float luminance = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
            float ink = Mathf.Clamp01((0.86f - luminance) / 0.3f);
            return new Color(1f, 1f, 1f, c.a * ink);
        }

        internal static void Configure(string path)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed; // source only: the SpriteAtlas compresses
            importer.SaveAndReimport();
        }

        private static Sprite Find(string name)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(AtlasB))
            {
                if (asset is Sprite sprite && sprite.name == name)
                    return sprite;
            }
            throw new System.InvalidOperationException("Atlas B has no " + name);
        }
    }
}
