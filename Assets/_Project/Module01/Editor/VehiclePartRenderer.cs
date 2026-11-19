using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Fotografía cada vehículo una vez por punto de inspección, con la pieza de ese punto en coral y
    /// el resto en gris, para las tarjetas de su revisión (cliente, 2026-11-11: «cambiar la lista por
    /// el render de la bici o la patineta, que muestre qué componentes se revisaron y cuáles faltaron»).
    /// <para>
    /// Las piezas salen de <see cref="VehicleProfileSO.PartNamesFor"/>: un punto sin pieza sale con el
    /// vehículo entero en gris. De costado y a tres cuartos, como se ve en el punto de exhibición.
    /// </para>
    /// </summary>
    public static class VehiclePartRenderer
    {
        public const string Folder = EppThumbnailRenderer.Folder + "/Vehicle";
        private static readonly Color Highlight = new Color(1f, 0.33f, 0.22f);
        // Oscuro a propósito: con la luz de producto de las tarjetas un gris claro sale blanco.
        private static readonly Color Body = new Color(0.20f, 0.23f, 0.28f);

        /// <summary>Una pieza más pequeña que esto se encuadra de cerca, con su entorno.</summary>
        private const float CloseUp = 0.6f;

        [MenuItem("RideSafe/Módulo 1/Fotografiar las piezas de los vehículos (revisión)")]
        public static void RenderAll()
        {
            Directory.CreateDirectory(Folder);
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            Material body = new Material(lit) { color = Body };
            body.SetColor("_BaseColor", Body);
            Material part = new Material(lit);
            part.SetColor("_BaseColor", Highlight);
            part.EnableKeyword("_EMISSION");
            part.SetColor("_EmissionColor", Highlight * 1.5f);

            foreach (VehicleProfiles.Definition definition in VehicleProfiles.Definitions)
            {
                VehicleProfileSO profile = VehicleProfiles.Load(definition);
                if (profile == null || profile.Model == null)
                    continue;
                var renders = new List<(string, Sprite)>();
                foreach (ZoneSO zone in profile.InspectionZones)
                    foreach (SafetyItemSO item in zone.Items)
                    {
                        string path = $"{Folder}/RS_M1_{definition.AssetSuffix}_{item.ItemId}.png";
                        Texture2D photo = Photograph(profile.Model, profile.PartNamesFor(item.ItemId), body, part);
                        File.WriteAllBytes(path, photo.EncodeToPNG());
                        Object.DestroyImmediate(photo);
                        renders.Add((item.ItemId, Import(path)));
                    }
                profile.ConfigurePointRenders(renders);
                EditorUtility.SetDirty(profile);
                Debug.Log($"[M1-BATCH] {definition.AssetSuffix}: {renders.Count} renders de piezas.");
            }
            Object.DestroyImmediate(body);
            Object.DestroyImmediate(part);
            AssetDatabase.SaveAssets();
        }

        private static Texture2D Photograph(GameObject prefab, IReadOnlyList<string> parts, Material body, Material part)
        {
            PreviewRenderUtility studio = new PreviewRenderUtility();
            try
            {
                GameObject model = Object.Instantiate(prefab);
                studio.AddSingleGO(model);
                Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
                // Solo la malla propia de la pieza: los hijos de 'tenedor' son otras piezas.
                HashSet<Renderer> lit = new HashSet<Renderer>(model.GetComponentsInChildren<Transform>()
                    .Where(t => parts.Contains(t.name)).Select(t => t.GetComponent<Renderer>()).Where(r => r != null));
                foreach (Renderer r in renderers)
                    r.sharedMaterials = Enumerable.Repeat(lit.Contains(r) ? part : body, r.sharedMaterials.Length).ToArray();

                Bounds bounds = renderers[0].bounds;
                foreach (Renderer r in renderers)
                    bounds.Encapsulate(r.bounds);
                // De costado: mirando a lo largo del eje horizontal corto, un poco girado y desde arriba.
                Vector3 side = bounds.size.x >= bounds.size.z ? Vector3.forward : Vector3.right;
                Vector3 view = (Quaternion.AngleAxis(-25f, Vector3.up) * side * Mathf.Cos(.3f) + Vector3.up * Mathf.Sin(.3f)).normalized;

                EppThumbnailRenderer.Light(studio, view);
                Camera camera = studio.camera;
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.allowHDR = false;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                camera.transform.rotation = Quaternion.LookRotation(-view, Vector3.up);
                camera.transform.position = bounds.center + view * (bounds.extents.magnitude * 4f + 1f);
                camera.orthographicSize = bounds.extents.magnitude;

                // Lo pequeño (frenos, pantalla, luces) se encuadra de cerca: con el vehículo entero
                // una maneta de 3 cm no se ve en la tarjeta.
                if (lit.Count > 0)
                {
                    Bounds focus = lit.First().bounds;
                    foreach (Renderer r in lit)
                        focus.Encapsulate(r.bounds);
                    if (Mathf.Max(focus.size.x, focus.size.y, focus.size.z) < CloseUp)
                    {
                        camera.transform.position = focus.center + view * (bounds.extents.magnitude * 4f + 1f);
                        camera.orthographicSize = CloseUp * .5f;
                        return EppThumbnailRenderer.Shoot(studio);
                    }
                }
                Texture2D draft = EppThumbnailRenderer.Shoot(studio);
                EppThumbnailRenderer.Refit(camera, draft);
                Object.DestroyImmediate(draft);
                return EppThumbnailRenderer.Shoot(studio);
            }
            finally
            {
                studio.Cleanup();
            }
        }

        private static Sprite Import(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = EppThumbnailRenderer.Size;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        }
    }
}
