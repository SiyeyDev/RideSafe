using System.IO;
using UnityEditor;
using UnityEngine;

namespace RideSafe.UI.EditorTools
{
    /// <summary>
    /// El panel «Achievement» del módulo 1 (reunión del 2026-11-11): la felicitación al aprobar una
    /// sección y el cierre del módulo con una ilustración de premio.
    /// <para>
    /// Es una copia de «Orientation» —la misma caja, el mismo kit y las mismas tipografías— con un
    /// solo botón, <c>ContinueButton</c>, y la insignia <c>Award</c> encima. Se edita el prefab en su
    /// sitio, como <see cref="UIComposition"/>, para no perder lo que otros comandos le hicieron.
    /// Idempotente: si ya existe, se rehace.
    /// </para>
    /// </summary>
    public static class UIAchievementPanel
    {
        public const string PanelId = "Achievement";
        public const string AwardPath = UIComposition.IconsFolder + "/Award_Rosette.png";

        [MenuItem("RideSafe/UI/Panel de logro del módulo 1")]
        public static void Apply()
        {
            DrawAward();
            Sprite award = AssetDatabase.LoadAssetAtPath<Sprite>(AwardPath);
            GameObject root = PrefabUtility.LoadPrefabContents(UIComposition.Module01Path);
            try
            {
                Transform old = root.transform.Find(PanelId);
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);

                Transform source = root.transform.Find("Orientation");
                GameObject panel = Object.Instantiate(source.gameObject, root.transform);
                panel.name = PanelId;
                panel.SetActive(false);

                Transform box = panel.transform.Find("OrientationPanel");
                box.name = "AchievementPanel";
                Object.DestroyImmediate(box.Find("RepeatAudioButton").gameObject);
                Transform subtitle = panel.transform.Find("Subtitle");
                if (subtitle != null)
                    subtitle.gameObject.SetActive(false);

                RectTransform button = (RectTransform)box.Find("StartButton");
                button.name = "ContinueButton";
                WordsNotIcon(button);
                CenterX(button);
                // «Continuar al módulo 2» no cabe en el ancho de «Comenzar» (104).
                button.sizeDelta = new Vector2(250f, button.sizeDelta.y);

                // La insignia corona la caja, centrada y medio fuera de su borde superior.
                GameObject icon = new GameObject("Award", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                icon.layer = box.gameObject.layer;
                RectTransform rect = (RectTransform)icon.transform;
                rect.SetParent(box, false);
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
                rect.pivot = new Vector2(.5f, .5f);
                rect.sizeDelta = new Vector2(120f, 120f);
                rect.anchoredPosition = new Vector2(0f, 8f);
                UnityEngine.UI.Image image = icon.GetComponent<UnityEngine.UI.Image>();
                image.sprite = award;
                image.preserveAspect = true;
                image.raycastTarget = false;

                Register(root.GetComponent<ModuleUI>(), panel);
                PrefabUtility.SaveAsPrefabAsset(root, UIComposition.Module01Path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            Debug.Log("[M1-BATCH] Panel de logro en " + UIComposition.Module01Path);
        }

        /// <summary>
        /// La bienvenida lleva ▶ en vez de «Comenzar» (PO, 2026-11-07; UIComposition.IconInsteadOfLabel).
        /// Este botón dice lo que hace —«Continuar», «Intentar de nuevo», «Continuar al módulo 2»—, así
        /// que la copia recupera su texto y suelta el icono.
        /// </summary>
        private static void WordsNotIcon(RectTransform button)
        {
            Transform label = button.Find("Label");
            if (label != null)
                label.gameObject.SetActive(true);
            Transform icon = button.Find("Icon");
            if (icon == null)
                return;
            foreach (SelectableStyle style in button.GetComponentsInChildren<SelectableStyle>(true))
            {
                SerializedObject so = new SerializedObject(style);
                SerializedProperty iconRef = so.FindProperty("_icon");
                if (iconRef != null && iconRef.objectReferenceValue != null &&
                    ((Component)iconRef.objectReferenceValue).transform == icon)
                {
                    iconRef.objectReferenceValue = null;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            Object.DestroyImmediate(icon.gameObject);
        }

        private static void CenterX(RectTransform rect)
        {
            Vector2 size = rect.rect.size;
            float y = rect.anchoredPosition.y, anchorY = rect.anchorMin.y, pivotY = rect.pivot.y;
            rect.anchorMin = new Vector2(.5f, anchorY);
            rect.anchorMax = new Vector2(.5f, anchorY);
            rect.pivot = new Vector2(.5f, pivotY);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(0f, y);
        }

        private static void Register(ModuleUI module, GameObject panel)
        {
            SerializedObject so = new SerializedObject(module);
            SerializedProperty list = so.FindProperty("_panels");
            for (int i = list.arraySize - 1; i >= 0; i--)
                if (list.GetArrayElementAtIndex(i).FindPropertyRelative("Id").stringValue == PanelId)
                    list.DeleteArrayElementAtIndex(i);
            list.arraySize++;
            SerializedProperty element = list.GetArrayElementAtIndex(list.arraySize - 1);
            element.FindPropertyRelative("Id").stringValue = PanelId;
            element.FindPropertyRelative("Root").objectReferenceValue = panel;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Una escarapela de premio (el «moño» que pidió el cliente) con los colores de la marca:
        /// dos cintas menta, un disco coral festoneado y el centro blanco con una estrella navy.
        /// Provisional hasta que arte entregue la suya.
        /// </summary>
        private static void DrawAward()
        {
            if (File.Exists(AwardPath))
                return;
            const int size = 256;
            Color coral = UIBuilderKit.Coral, mint = UIBuilderKit.Mint, navy = UIBuilderKit.BeamNavy;
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(size * .5f, size * .58f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + .5f, y + .5f);
                    Vector2 d = p - c;
                    float r = d.magnitude, a = Mathf.Atan2(d.y, d.x);
                    Color col = Color.clear;
                    // Cintas: dos bandas inclinadas bajo el disco, con la punta en V.
                    foreach (float side in new[] { -1f, 1f })
                    {
                        Vector2 q = new Vector2((p.x - c.x) * side, c.y - p.y);
                        bool band = q.y > 0 && q.y < 118f && Mathf.Abs(q.x - q.y * .45f - 10f) < 22f;
                        bool notch = q.y > 96f && Mathf.Abs(q.x - q.y * .45f - 10f) < (q.y - 96f);
                        if (band && !notch) col = mint;
                    }
                    float scallop = 92f + 7f * Mathf.Cos(a * 14f);
                    if (r < scallop) col = coral;
                    if (r < 70f) col = Color.white;
                    if (r < 64f && Star(d, 46f, 20f)) col = navy;
                    // Borde suave de un píxel.
                    if (col.a > 0f && r < scallop && r > scallop - 1f) col.a = scallop - r;
                    px[y * size + x] = col;
                }
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(AwardPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(AwardPath);
            UIStatusIcons.Configure(AwardPath);
        }

        /// <summary>¿Cae el punto dentro de una estrella de cinco puntas centrada?</summary>
        private static bool Star(Vector2 d, float outer, float inner)
        {
            float a = Mathf.Atan2(d.x, d.y);   // punta hacia arriba
            float seg = Mathf.PI / 5f;
            float t = Mathf.Repeat(a, 2f * seg) / seg;   // 0..2 dentro de un par punta-valle
            float k = t <= 1f ? t : 2f - t;
            // Radio del borde de la estrella en ese ángulo, interpolando punta y valle en línea recta.
            Vector2 tip = new Vector2(0f, outer), valley = new Vector2(Mathf.Sin(seg), Mathf.Cos(seg)) * inner;
            Vector2 edge = Vector2.Lerp(tip, valley, k);
            return d.magnitude <= edge.magnitude;
        }
    }
}
