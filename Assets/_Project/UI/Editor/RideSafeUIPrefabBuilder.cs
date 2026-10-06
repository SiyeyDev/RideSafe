using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TextCore.LowLevel;

namespace RideSafe.UI.EditorTools
{
    /// <summary>
    /// Generates the functional UI of Modules 0, 1 and 2 from the UX/UI storyboard v3:
    /// one prefab per module (a world-space canvas whose children are the module's panels,
    /// no backdrop, controls and view scripts wired) and a catalog scene to try them with
    /// XRPlayer.
    /// <para>
    /// Re-running is safe: prefabs are overwritten in place, so their GUIDs and every scene
    /// reference to them survive.
    /// </para>
    /// </summary>
    public static class RideSafeUIPrefabBuilder
    {
        private const string k_AtlasA = "Assets/DriveSafe/UI/RideSafe_UI_Atlas_A_4096.png";
        private const string k_AtlasB = "Assets/DriveSafe/UI/RideSafe_UI_Atlas_B_4096.png";
        private const string k_LogoPath = "Assets/DriveSafe/UI/ridesafe-logo.png";
        private const string k_FontsDir = "Assets/DriveSafe/UI/Fonts";
        private const string k_LiberationSans = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string k_CatalogScene = "Assets/DriveSafe/Scene/RideSafe_UI_Panels.unity";
        private const string k_XRPlayerPath = "Assets/AutoHand/Examples/Scenes/XR/Prefabs/XRPlayer.prefab";

        private const string k_Charset =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "¡¿ÁÉÍÓÚÑÜáéíóúñü·×–—‘’“”•…↑↓€°";

        private const float k_ColumnPitch = 2.35f;

        #region Entry points

        [MenuItem("RideSafe/UI/Build UI prefabs and catalog scene")]
        public static void BuildMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            Build(null);
        }

        /// <summary>Batchmode entry. Optional "-shotsDir &lt;path&gt;" renders every screen to PNG.</summary>
        public static void BuildFromCommandLine()
        {
            try
            {
                Build(GetArg("-shotsDir"));
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static string GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                    return args[i + 1];
            }
            return null;
        }

        #endregion

        public static void Build(string shotsDir)
        {
            TMP_FontAsset fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(k_LiberationSans);
            UIBuilderKit kit = new UIBuilderKit(LoadAtlasSprites(), AssetDatabase.LoadAssetAtPath<Sprite>(k_LogoPath),
                EnsureFont("Manrope-Bold", fallback), EnsureFont("Manrope-SemiBold", fallback),
                EnsureFont("Inter-Regular", fallback), EnsureFont("Inter-Medium", fallback),
                EnsureFont("Inter-SemiBold", fallback));
            AssetDatabase.SaveAssets();

            // Prefabs are assembled in a throwaway scene so nothing leaks into the user's scene.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            UIModules modules = new UIModules(new UIElements(kit));
            modules.BuildAll();
            AssetDatabase.SaveAssets();

            foreach (string missing in kit.MissingSprites)
                Debug.LogError("[RideSafe.UI] Sprite not found in the UI atlases: " + missing);
            Debug.Log("[RideSafe.UI] Built " + modules.Modules.Count + " module prefabs (" + kit.MissingSprites.Count +
                      " missing sprites).");

            List<GameObject> instances = BuildCatalogScene(kit, modules);
            if (!string.IsNullOrEmpty(shotsDir))
                CaptureShots(kit, modules, instances, shotsDir);
        }

        #region Catalog scene

        private static List<GameObject> BuildCatalogScene(UIBuilderKit kit, UIModules modules)
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject light = new GameObject("Directional Light");
            Light sun = light.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(1.6f, 1f, 0.8f);

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_XRPlayerPath);
            if (playerPrefab != null)
                PrefabUtility.InstantiatePrefab(playerPrefab).name = "XRPlayer";
            else
                Debug.LogWarning("[RideSafe.UI] XRPlayer prefab not found at " + k_XRPlayerPath);

            // One module per slot, at eye height, in front of the player.
            List<GameObject> instances = new List<GameObject>();
            for (int i = 0; i < modules.Modules.Count; i++)
            {
                Vector3 position = new Vector3((i - 1) * k_ColumnPitch, 1.5f, 2.5f);
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(modules.Modules[i].Prefab);
                instance.transform.position = position;
                instances.Add(instance);

                bool dark = modules.Modules[i].Panels[0].Value;
                EnvironmentTile(kit, null, position + Vector3.forward * 0.02f, dark).name =
                    "EnvironmentTile (scene only) - " + instance.name;
                Caption(kit, null, instance.name, position + new Vector3(0f, -0.62f, 0f));
            }

            GameObject preview = new GameObject("Preview Camera (editor only)");
            preview.tag = "EditorOnly";
            Camera camera = preview.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UIBuilderKit.Hex("#EDF1F5");
            preview.transform.position = new Vector3(0f, 1.5f, -1.5f);
            preview.SetActive(false);

            if (!EditorSceneManager.SaveScene(scene, k_CatalogScene))
                throw new IOException("Could not save " + k_CatalogScene);
            Debug.Log("[RideSafe.UI] Catalog scene saved: " + k_CatalogScene);
            return instances;
        }

        private static GameObject EnvironmentTile(UIBuilderKit kit, Transform parent, Vector3 position, bool dark)
        {
            GameObject tile = new GameObject("EnvironmentTile (scene only)", typeof(RectTransform), typeof(Canvas));
            tile.transform.SetParent(parent, false);
            tile.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            RectTransform rect = (RectTransform)tile.transform;
            rect.sizeDelta = new Vector2(UIBuilderKit.FrameW, UIBuilderKit.FrameH);
            rect.localScale = Vector3.one * UIBuilderKit.WorldScale;
            rect.position = position;
            kit.AddSliced(tile, "Panel_Light_Base", 2f, dark ? UIBuilderKit.Hex("#0A2748") : UIBuilderKit.Hex("#D4DDE6"));
            return tile;
        }

        private static void Caption(UIBuilderKit kit, Transform parent, string text, Vector3 position)
        {
            GameObject caption = new GameObject("Caption", typeof(RectTransform), typeof(Canvas));
            caption.transform.SetParent(parent, false);
            caption.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            RectTransform rect = (RectTransform)caption.transform;
            rect.sizeDelta = new Vector2(UIBuilderKit.FrameW, 44f);
            rect.localScale = Vector3.one * UIBuilderKit.WorldScale;
            rect.position = position;
            kit.AddText(caption, text, kit.H2.With(24f), TextAlignmentOptions.Left);
        }

        #endregion

        #region Assets

        private static Dictionary<string, Sprite> LoadAtlasSprites()
        {
            Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
            foreach (string path in new[] { k_AtlasA, k_AtlasB })
            {
                foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
                {
                    Sprite sprite = asset as Sprite;
                    if (sprite != null)
                        sprites[sprite.name] = sprite;
                }
            }
            if (sprites.Count == 0)
                throw new InvalidOperationException("No sprites found in the RideSafe UI atlases.");
            return sprites;
        }

        /// <summary>
        /// Returns the TMP font asset for a TTF in the fonts folder, creating it once with
        /// Latin + Spanish glyphs baked in and LiberationSans as fallback.
        /// </summary>
        private static TMP_FontAsset EnsureFont(string fileName, TMP_FontAsset fallback)
        {
            string assetPath = k_FontsDir + "/" + fileName + " SDF.asset";
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
                return existing;

            Font font = AssetDatabase.LoadAssetAtPath<Font>(k_FontsDir + "/" + fileName + ".ttf");
            if (font == null)
            {
                Debug.LogWarning("[RideSafe.UI] Missing font " + fileName + ".ttf; using fallback.");
                return fallback;
            }

            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 64, 6, GlyphRenderMode.SDFAA, 2048, 2048,
                                                                 AtlasPopulationMode.Dynamic, false);
            asset.name = fileName + " SDF";
            AssetDatabase.CreateAsset(asset, assetPath);
            asset.material.name = fileName + " SDF Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            if (fallback != null)
                asset.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };

            string missing;
            asset.TryAddCharacters(k_Charset, out missing);
            for (int i = 0; i < asset.atlasTextures.Length; i++)
            {
                Texture2D texture = asset.atlasTextures[i];
                if (texture == null || AssetDatabase.Contains(texture))
                    continue;
                texture.name = fileName + " SDF Atlas " + i;
                AssetDatabase.AddObjectToAsset(texture, asset);
            }
            EditorUtility.SetDirty(asset);
            return asset;
        }

        #endregion

        #region Screenshots

        /// <summary>
        /// Renders every panel of every module head-on (showing one at a time through
        /// ModuleUI), over a light or navy tile matching the storyboard environment.
        /// </summary>
        private static void CaptureShots(UIBuilderKit kit, UIModules modules, List<GameObject> instances, string directory)
        {
            Directory.CreateDirectory(directory);

            GameObject cameraObject = new GameObject("__ShotCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UIBuilderKit.Hex("#EDF1F5");
            camera.orthographic = true;
            camera.orthographicSize = UIBuilderKit.FrameH * UIBuilderKit.WorldScale * 0.5f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 1f;

            try
            {
                for (int m = 0; m < instances.Count; m++)
                {
                    GameObject instance = instances[m];
                    ModuleUI module = instance.GetComponent<ModuleUI>();
                    GameObject tile = EnvironmentTile(kit, null, instance.transform.position + Vector3.forward * 0.01f, false);
                    cameraObject.transform.SetPositionAndRotation(instance.transform.position - Vector3.forward * 0.5f,
                                                                  Quaternion.identity);

                    foreach (KeyValuePair<string, bool> panel in modules.Modules[m].Panels)
                    {
                        module.Show(panel.Key);
                        tile.GetComponentInChildren<UnityEngine.UI.Image>().color =
                            panel.Value ? UIBuilderKit.Hex("#0A2748") : UIBuilderKit.Hex("#D4DDE6");
                        UIBuilderKit.RebuildLayouts(instance);
                        Canvas.ForceUpdateCanvases();
                        foreach (TMP_Text text in instance.GetComponentsInChildren<TMP_Text>())
                            text.ForceMeshUpdate(true, true);
                        Save(camera, 1503, 834, Path.Combine(directory, instance.name + "__" + panel.Key + ".png"));
                    }
                    UnityEngine.Object.DestroyImmediate(tile);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
            Debug.Log("[RideSafe.UI] Screenshots written to " + directory);
        }

        private static void Save(Camera camera, int width, int height, string path)
        {
            RenderTexture target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                RenderPipeline.StandardRequest request = new RenderPipeline.StandardRequest { destination = target };
                if (GraphicsSettings.currentRenderPipeline != null && RenderPipeline.SupportsRenderRequest(camera, request))
                    RenderPipeline.SubmitRenderRequest(camera, request);
                else
                    camera.Render();

                RenderTexture.active = target;
                Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
            }
        }

        #endregion
    }
}
