using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace RideSafe.UI.EditorTools
{
    /// <summary>
    /// One SpriteAtlas for every UI sprite (sheets A and B and the logo), 2026-10-16 review.
    /// <para>
    /// The two hand-made sheets are 4096 x 4096 and mostly empty: together about 15 MB of GPU
    /// memory on Quest for under half that much art, with no mipmaps (world-space panels at
    /// 1.6–1.8 m shimmer) and crunch at quality 50 (blocky 9-slice edges). Packed tight into
    /// 2048 pages, with mipmaps, ASTC 6x6 on Android and no crunch, the same sprites cost about
    /// half and draw cleanly. The sheets stay as the source: every Image keeps its sprite, and
    /// only the build packs them.
    /// </para>
    /// <para>
    /// No rotation and no tight packing: both break 9-sliced sprites. Idempotent.
    /// </para>
    /// </summary>
    internal static class UIAtlas
    {
        public const string AtlasPath = "Assets/_Project/UI/RideSafe_UI.spriteatlasv2";
        private static readonly string[] Sources =
        {
            "Assets/_Project/UI/RideSafe_UI_Atlas_A_4096.png",
            "Assets/_Project/UI/RideSafe_UI_Atlas_B_4096.png",
            "Assets/_Project/UI/ridesafe-logo.png",
            "Assets/_Project/UI/StatusIcons",
            UIBrand.Folder
        };

        [MenuItem("RideSafe/UI/Crear o actualizar el SpriteAtlas de la UI")]
        public static void Apply()
        {
            // The sheets are only the source now: uncompressed, so the atlas is compressed once,
            // not crunch first and ASTC on top. The build ships the atlas pages, not the sheets.
            foreach (string source in Sources)
            {
                TextureImporter sheet = AssetImporter.GetAtPath(source) as TextureImporter;
                if (sheet == null) // a folder of sprites (StatusIcons, Brand), already uncompressed by its tool
                    continue;
                if (sheet.textureCompression == TextureImporterCompression.Uncompressed && !sheet.crunchedCompression)
                    continue;
                sheet.textureCompression = TextureImporterCompression.Uncompressed;
                sheet.crunchedCompression = false;
                sheet.SaveAndReimport();
            }

            // The packable list is written every time, so a new source joins an existing atlas; the
            // atlas's GUID and its settings live in the .meta and survive the rewrite.
            {
                SpriteAtlasAsset asset = new SpriteAtlasAsset();
                Object[] packables = new Object[Sources.Length];
                for (int i = 0; i < Sources.Length; i++)
                {
                    packables[i] = AssetDatabase.LoadMainAssetAtPath(Sources[i]);
                    if (packables[i] == null)
                        throw new System.IO.FileNotFoundException("UI sprite source missing", Sources[i]);
                }
                asset.Add(packables);
                SpriteAtlasAsset.Save(asset, AtlasPath);
                AssetDatabase.ImportAsset(AtlasPath);
            }

            SpriteAtlasImporter importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(AtlasPath);
            importer.includeInBuild = true;
            importer.packingSettings = new SpriteAtlasPackingSettings
            {
                enableRotation = false,
                enableTightPacking = false,
                enableAlphaDilation = true,
                padding = 4
            };
            importer.textureSettings = new SpriteAtlasTextureSettings
            {
                generateMipMaps = true,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 2,
                sRGB = true,
                readable = false
            };
            importer.SetPlatformSettings(new TextureImporterPlatformSettings
            {
                name = "DefaultTexturePlatform",
                maxTextureSize = 2048,
                textureCompression = TextureImporterCompression.Compressed,
                crunchedCompression = false
            });
            importer.SetPlatformSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = 2048,
                format = TextureImporterFormat.ASTC_6x6,
                textureCompression = TextureImporterCompression.Compressed,
                crunchedCompression = false
            });
            importer.SaveAndReimport();

            // Packing on in the editor too, so Play shows what the Quest build draws.
            EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;

            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath);
            SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
            atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath);
            Debug.Log("[RideSafe.UI] SpriteAtlas " + AtlasPath + ": " + atlas.spriteCount + " sprites.");
        }
    }
}
