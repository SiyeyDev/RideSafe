using System.IO;
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

        public static string MonoName(string source) => source.Replace("Status_", "StatusMono_");

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
            Debug.Log("[RideSafe.UI] Status icons written to " + Folder);
        }

        /// <summary>Dark or saturated ink becomes white (keeping its alpha); light pixels become transparent.</summary>
        internal static Color Ink(Color c)
        {
            float luminance = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
            float ink = Mathf.Clamp01((0.86f - luminance) / 0.3f);
            return new Color(1f, 1f, 1f, c.a * ink);
        }

        private static void Configure(string path)
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
