using System.IO;
using UnityEditor;
using UnityEngine;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Las gafas dañadas (H07): las mismas gafas de seguridad de O07 con otro material, rayado y
    /// rajado. El artista lo confirmó el 2026-10-28: no hay modelo aparte, «es un material».
    /// <para>
    /// Una <b>variante de prefab</b> y no el FBX con el material cambiado en la escena: el pedestal
    /// del inspector instancia el prefab de exhibición, y ahí también tienen que verse dañadas.
    /// Variante y no FBX también para que <see cref="Module01SceneEntities"/> no las confunda con
    /// las gafas buenas, que el artista colocó con ese mismo FBX.
    /// </para>
    /// <para>
    /// Solo se crea lo que falta. Si 3D entrega su material, se pone en <see cref="MaterialPath"/>
    /// —o se le cambia a la variante— y esto ya no lo pisa.
    /// </para>
    /// </summary>
    public static class Module01DamagedGlasses
    {
        private const string Folder = "Assets/_Project/Module01/Art/GafasDanadas";
        public const string PrefabPath = Folder + "/P_GafasDanadas.prefab";
        public const string MaterialPath = Folder + "/M_GafasDanadas.mat";
        private const string TexturePath = Folder + "/T_GafasDanadas.png";
        private const string MeshPath = Folder + "/ME_GafasDanadas.asset";

        private const string Model = Module01Library.Models + "gafdas de seguridad.fbx";
        private const string CleanMaterial = Module01Library.ArtMaterials + "gafas_de_seguridad.mat";

        public static void Ensure()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(Folder).Replace('\\', '/'), Path.GetFileName(Folder));

            if (AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath) == null)
            {
                File.WriteAllBytes(TexturePath, CrackedLens().EncodeToPNG());
                AssetDatabase.ImportAsset(TexturePath);
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                // Del material limpio, para que siga siendo el mismo plástico transparente.
                material = new Material(AssetDatabase.LoadAssetAtPath<Material>(CleanMaterial)) { name = "M_GafasDanadas" };
                Texture2D cracks = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
                material.SetTexture("_BaseMap", cracks);
                material.SetTexture("_MainTex", cracks);
                // Blanco opaco: la opacidad la pone la textura, empañada en el fondo y fuerte en
                // las grietas. Con el alfa de 0,25 del limpio las grietas apenas se verían.
                material.SetColor("_BaseColor", Color.white);
                material.SetColor("_Color", Color.white);
                material.SetFloat("_Smoothness", .35f);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                return;
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model));
            try
            {
                instance.name = "gafas_danadas";
                // El FBX llega sin UV: sin ellas la textura se ve de un solo color y no hay grietas.
                MeshFilter filter = instance.GetComponentInChildren<MeshFilter>(true);
                Mesh mapped = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
                if (mapped == null)
                {
                    mapped = BoxMapped(filter.sharedMesh);
                    AssetDatabase.CreateAsset(mapped, MeshPath);
                }
                filter.sharedMesh = mapped;
                foreach (Renderer r in instance.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] slots = r.sharedMaterials;
                    for (int i = 0; i < slots.Length; i++)
                        slots[i] = material;
                    r.sharedMaterials = slots;
                }
                PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
            Debug.Log("[Module01] Creadas las gafas dañadas: " + PrefabPath);
        }

        /// <summary>
        /// La malla con UV de caja: cada triángulo se proyecta sobre el plano de los ejes que más
        /// encara, escalado a la medida mayor del objeto, así la textura cubre las gafas una vez.
        /// Los vértices se separan por triángulo para que dos caras no compartan UV.
        /// </summary>
        private static Mesh BoxMapped(Mesh source)
        {
            Vector3[] v = source.vertices;
            Vector3[] n = source.normals;
            int[] tris = source.triangles;
            Bounds b = source.bounds;
            float span = Mathf.Max(b.size.x, b.size.y, b.size.z);

            var vertices = new Vector3[tris.Length];
            var normals = new Vector3[tris.Length];
            var uvs = new Vector2[tris.Length];
            var indices = new int[tris.Length];
            for (int t = 0; t < tris.Length; t += 3)
            {
                Vector3 a = v[tris[t]], c = v[tris[t + 1]], d = v[tris[t + 2]];
                Vector3 face = Vector3.Cross(c - a, d - a);
                Vector3 abs = new Vector3(Mathf.Abs(face.x), Mathf.Abs(face.y), Mathf.Abs(face.z));
                for (int k = 0; k < 3; k++)
                {
                    Vector3 p = (v[tris[t + k]] - b.min) / span;
                    vertices[t + k] = v[tris[t + k]];
                    normals[t + k] = n.Length > 0 ? n[tris[t + k]] : face.normalized;
                    uvs[t + k] = abs.x >= abs.y && abs.x >= abs.z ? new Vector2(p.z, p.y)
                               : abs.y >= abs.z ? new Vector2(p.x, p.z)
                               : new Vector2(p.x, p.y);
                    indices[t + k] = t + k;
                }
            }

            var mesh = new Mesh { name = "gafas_danadas" };
            mesh.indexFormat = tris.Length > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = indices;
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        /// <summary>
        /// Lente empañada con un golpe: grietas que salen del impacto, dos anillos rotos alrededor
        /// y rayones sueltos. Semilla fija, para que rehacerla dé la misma imagen.
        /// </summary>
        private static Texture2D CrackedLens()
        {
            const int size = 512;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(222, 220, 205, 60);

            var rng = new System.Random(7);
            Vector2 impact = new Vector2(size * .38f, size * .56f);

            for (int k = 0; k < 11; k++)
            {
                float angle = (k + (float)rng.NextDouble() * .6f) * Mathf.PI * 2f / 11f;
                Vector2 p = impact;
                float length = size * (.25f + (float)rng.NextDouble() * .45f);
                for (float run = 0; run < length; run += 14f)
                {
                    angle += ((float)rng.NextDouble() - .5f) * .5f;
                    Vector2 next = p + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 14f;
                    Line(pixels, size, p, next, 2, 245);
                    p = next;
                }
            }

            foreach (float radius in new[] { size * .07f, size * .15f })
            {
                float start = 0f;
                while (start < Mathf.PI * 2f)
                {
                    float arc = .3f + (float)rng.NextDouble() * .6f;
                    Vector2 prev = impact + Polar(start, radius);
                    for (float a = start; a < start + arc; a += .08f)
                    {
                        Vector2 next = impact + Polar(a, radius * (.92f + (float)rng.NextDouble() * .16f));
                        Line(pixels, size, prev, next, 1, 230);
                        prev = next;
                    }
                    start += arc + .2f + (float)rng.NextDouble() * .4f;
                }
            }

            for (int k = 0; k < 40; k++)
            {
                Vector2 a = new Vector2(rng.Next(size), rng.Next(size));
                float angle = (float)rng.NextDouble() * Mathf.PI;
                Vector2 b = a + Polar(angle, 20f + (float)rng.NextDouble() * 90f);
                Line(pixels, size, a, b, 0, 185);
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        private static Vector2 Polar(float angle, float radius) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

        private static void Line(Color32[] pixels, int size, Vector2 from, Vector2 to, int half, byte alpha)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(from, to)) + 1;
            for (int s = 0; s <= steps; s++)
            {
                Vector2 p = Vector2.Lerp(from, to, s / (float)steps);
                for (int dx = -half; dx <= half; dx++)
                    for (int dy = -half; dy <= half; dy++)
                    {
                        int x = Mathf.RoundToInt(p.x) + dx, y = Mathf.RoundToInt(p.y) + dy;
                        if (x < 0 || y < 0 || x >= size || y >= size)
                            continue;
                        pixels[y * size + x] = new Color32(250, 250, 250, alpha);
                    }
            }
        }
    }
}
