using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Fotografía cada EPP para su tarjeta de «Revisa tus elecciones» y «Evaluación de tus
    /// elecciones» (PO, 2026-11-04) y deja el render en <see cref="SafetyItemSO.Thumbnail"/>.
    /// <para>
    /// <b>Renders propios y no las fotos del xlsx.</b> La biblioteca del cliente enlaza fotos de
    /// producto de marcas, y su propia «Guía visual» dice que la referencia no da permiso de
    /// reutilización. Los modelos del garaje son nuestros, y además son lo que el aprendiz ve.
    /// </para>
    /// <para>
    /// <b>El mismo objeto que el pedestal</b>, en su mismo orden (<see cref="ItemInspector"/>): el
    /// modelo de inspección si lo hay (el pantalón abierto), si no la copia de lo que el artista
    /// puso en la mesa de la v5 —con sus materiales: las gafas dañadas lo son por material— y si no
    /// el modelo del catálogo. La foto es una vista 3/4 desde la parada donde se ve, de arriba como
    /// se mira una mesa, sobre fondo transparente, como pide la «Guía visual» del xlsx.
    /// </para>
    /// <para>
    /// Todo pasa en una escena de previsualización aislada: ni la v5 ni sus luces horneadas se
    /// tocan. Se vuelve a correr cuando el arte cambie; sobrescribe los PNG con el mismo nombre.
    /// </para>
    /// </summary>
    public static class EppThumbnailRenderer
    {
        public const string Folder = "Assets/_Project/Module01/Art/Thumbnails";
        public const int Size = 512;
        private const string CatalogPath = "Assets/_Project/Module01/Data/SO_Module01Catalog.asset";
        private const string ScenePath = "Assets/_Project/Scene/RideSafe_Garaje_v5.unity";

        /// <summary>De frente a la parada, girado a tres cuartos.</summary>
        private const float TurnDegrees = 30f;
        /// <summary>Desde arriba, como se mira una mesa de pie.</summary>
        private const float ElevationDegrees = 35f;
        /// <summary>Aire alrededor del objeto dentro del cuadro.</summary>
        private const float Margin = 1.08f;

        [MenuItem("RideSafe/Módulo 1/Fotografiar EPP (tarjetas de Revisa y Evaluación)")]
        public static void RenderAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Sal de Play antes de fotografiar los EPP.");
            Module01CatalogSO catalog = AssetDatabase.LoadAssetAtPath<Module01CatalogSO>(CatalogPath);
            if (catalog == null)
                throw new System.InvalidOperationException("No está el catálogo " + CatalogPath);
            if (!SceneManager.GetSceneByPath(ScenePath).isLoaded)
                Debug.LogWarning("[EPP] La v5 no está abierta: se fotografía el modelo del catálogo, no lo que hay en la mesa.");

            Directory.CreateDirectory(Folder);
            Dictionary<string, Transform> entities = SceneEntities();
            Dictionary<string, Transform> anchors = Anchors(catalog);
            List<string> done = new List<string>(), missing = new List<string>();

            foreach (Module01Library.Entry entry in Module01Library.All)
            {
                if (!catalog.TryGetItem(entry.ItemId, out SafetyItemSO item) || item == null)
                {
                    missing.Add(entry.ItemId + " (no está en el catálogo)");
                    continue;
                }
                entities.TryGetValue(item.ItemId, out Transform onTable);
                anchors.TryGetValue(item.ItemId, out Transform stop);
                Texture2D photo = Photograph(item, onTable, stop);
                if (photo == null)
                {
                    missing.Add(item.ItemId + " (sin modelo)");
                    continue;
                }
                string path = Folder + "/RS_M1_" + entry.LibraryId + "_" + item.ItemId + ".png";
                File.WriteAllBytes(path, photo.EncodeToPNG());
                Object.DestroyImmediate(photo);
                Assign(item, path);
                done.Add(item.ItemId);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[EPP] Fotografiados " + done.Count + " en " + Folder +
                      (missing.Count > 0 ? ". Sin foto: " + string.Join(", ", missing) : "."));
        }

        /// <summary>El objeto de la mesa de cada EPP, por su id.</summary>
        private static Dictionary<string, Transform> SceneEntities()
        {
            Dictionary<string, Transform> found = new Dictionary<string, Transform>();
            foreach (WorldObjectTaskEntity entity in Object.FindObjectsByType<WorldObjectTaskEntity>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!string.IsNullOrEmpty(entity.ItemId) && !found.ContainsKey(entity.ItemId))
                    found[entity.ItemId] = entity.transform;
            return found;
        }

        /// <summary>Desde dónde se ve cada EPP: el ancla de la parada de su zona.</summary>
        private static Dictionary<string, Transform> Anchors(Module01CatalogSO catalog)
        {
            Dictionary<string, Transform> anchors = new Dictionary<string, Transform>();
            foreach (ZoneSO zone in catalog.Zones)
            {
                if (zone == null)
                    continue;
                GameObject anchor = GameObject.Find("anchor_" + zone.ZoneId);
                if (anchor == null)
                    continue;
                foreach (SafetyItemSO item in zone.Items)
                    if (item != null && !anchors.ContainsKey(item.ItemId))
                        anchors[item.ItemId] = anchor.transform;
            }
            return anchors;
        }

        private static Texture2D Photograph(SafetyItemSO item, Transform onTable, Transform stop)
        {
            PreviewRenderUtility studio = new PreviewRenderUtility();
            try
            {
                GameObject model = Model(item, onTable);
                if (model == null)
                    return null;
                studio.AddSingleGO(model);
                Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0)
                    return null;
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer r in renderers)
                    bounds.Encapsulate(r.bounds);

                Vector3 view = ViewDirection(bounds.center, onTable, stop);
                Light(studio, view);
                Camera camera = studio.camera;
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.allowHDR = false;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                Frame(camera, bounds, view);
                // Las esquinas de la caja del mundo dejan aire de sobra en lo que va girado: una
                // primera toma dice dónde quedó el objeto, y la segunda lo llena.
                Texture2D draft = Shoot(studio);
                Refit(camera, draft);
                Object.DestroyImmediate(draft);
                return Shoot(studio);
            }
            finally
            {
                studio.Cleanup();
            }
        }

        /// <summary>El orden del pedestal: modelo de inspección, copia de la mesa, modelo del catálogo.</summary>
        private static GameObject Model(SafetyItemSO item, Transform onTable)
        {
            if (item.InspectPrefab != null)
                return (GameObject)Object.Instantiate(item.InspectPrefab);
            if (onTable != null)
            {
                GameObject copy = VisibleMeshCopy.Of(onTable.GetComponentsInChildren<MeshRenderer>(true), item.ItemId, null);
                if (copy != null)
                    return copy;
            }
            return item.DisplayPrefab != null ? (GameObject)Object.Instantiate(item.DisplayPrefab) : null;
        }

        /// <summary>Hacia la cámara: de la mesa a la parada, girado a 3/4 y levantado.</summary>
        private static Vector3 ViewDirection(Vector3 centre, Transform onTable, Transform stop)
        {
            Vector3 flat = Vector3.back;
            if (onTable != null && stop != null)
            {
                Vector3 toStop = stop.position - onTable.position;
                toStop.y = 0f;
                if (toStop.sqrMagnitude > 1e-4f)
                    flat = toStop.normalized;
            }
            flat = Quaternion.AngleAxis(TurnDegrees, Vector3.up) * flat;
            float up = ElevationDegrees * Mathf.Deg2Rad;
            return (flat * Mathf.Cos(up) + Vector3.up * Mathf.Sin(up)).normalized;
        }

        /// <summary>Luz de producto: principal arriba a un lado, relleno al otro y una luz ambiente suave.</summary>
        private static void Light(PreviewRenderUtility studio, Vector3 view)
        {
            studio.ambientColor = new Color(0.42f, 0.44f, 0.48f);
            Light key = studio.lights[0];
            key.type = LightType.Directional;
            key.intensity = 1.2f;
            key.color = Color.white;
            key.transform.rotation = Quaternion.LookRotation(-(Quaternion.AngleAxis(-40f, Vector3.up) * view + Vector3.up * 0.6f));
            Light fill = studio.lights[1];
            fill.type = LightType.Directional;
            fill.intensity = 0.6f;
            fill.color = Color.white;
            fill.transform.rotation = Quaternion.LookRotation(-(Quaternion.AngleAxis(60f, Vector3.up) * view));
        }

        /// <summary>El objeto entero y centrado en el cuadro, con <see cref="Margin"/> de aire.</summary>
        private static void Frame(Camera camera, Bounds bounds, Vector3 view)
        {
            float radius = bounds.extents.magnitude;
            camera.transform.rotation = Quaternion.LookRotation(-view, Vector3.up);
            camera.transform.position = bounds.center + view * (radius * 4f + 1f);

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 local = camera.transform.InverseTransformPoint(corner);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }
            Vector2 middle = (min + max) * 0.5f;
            camera.transform.position += camera.transform.right * middle.x + camera.transform.up * middle.y;
            camera.orthographicSize = Mathf.Max(max.x - min.x, max.y - min.y) * 0.5f * Margin;
        }

        /// <summary>Centra y ajusta la cámara a lo que de verdad ocupa el objeto en la toma.</summary>
        private static void Refit(Camera camera, Texture2D draft)
        {
            Color[] pixels = draft.GetPixels();
            int width = draft.width, height = draft.height;
            int x0 = width, y0 = height, x1 = -1, y1 = -1;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (pixels[y * width + x].a > 0.05f)
                    {
                        x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x);
                        y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y);
                    }
            if (x1 < 0)
                return;
            float span = 2f * camera.orthographicSize;
            float cx = ((x0 + x1 + 1) * 0.5f / width - 0.5f) * span;
            float cy = ((y0 + y1 + 1) * 0.5f / height - 0.5f) * span;
            camera.transform.position += camera.transform.right * cx + camera.transform.up * cy;
            float extent = Mathf.Max((x1 - x0 + 1f) / width, (y1 - y0 + 1f) / height) * span;
            camera.orthographicSize = extent * 0.5f * Margin;
        }

        /// <summary>
        /// Dos tomas, sobre negro y sobre blanco: lo que cambia entre ellas es lo que no tapa el
        /// objeto. Así sale el alfa con cualquier pipeline, también si el de URP no lo conserva.
        /// </summary>
        private static Texture2D Shoot(PreviewRenderUtility studio)
        {
            Texture2D onBlack = Take(studio, Color.black), onWhite = Take(studio, Color.white);
            Color[] b = onBlack.GetPixels(), w = onWhite.GetPixels();
            Color[] result = new Color[b.Length];
            for (int i = 0; i < result.Length; i++)
            {
                float alpha = Mathf.Clamp01(1f - ((w[i].r - b[i].r) + (w[i].g - b[i].g) + (w[i].b - b[i].b)) / 3f);
                result[i] = alpha > 0.004f
                    ? new Color(Mathf.Clamp01(b[i].r / alpha), Mathf.Clamp01(b[i].g / alpha), Mathf.Clamp01(b[i].b / alpha), alpha)
                    : Color.clear;
            }
            Texture2D photo = new Texture2D(onBlack.width, onBlack.height, TextureFormat.RGBA32, false);
            photo.SetPixels(result);
            photo.Apply();
            Object.DestroyImmediate(onBlack);
            Object.DestroyImmediate(onWhite);
            return photo;
        }

        private static Texture2D Take(PreviewRenderUtility studio, Color background)
        {
            studio.camera.backgroundColor = background;
            studio.BeginStaticPreview(new Rect(0f, 0f, Size, Size));
            studio.Render(true, false);
            return studio.EndStaticPreview();
        }

        /// <summary>Sprite sin mipmaps a 512, con transparencia, y en el campo del elemento.</summary>
        private static void Assign(SafetyItemSO item, string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = Size;
            importer.SaveAndReimport();

            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            SerializedObject serialized = new SerializedObject(item);
            serialized.FindProperty("_thumbnail").objectReferenceValue = sprite;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
        }
    }
}
