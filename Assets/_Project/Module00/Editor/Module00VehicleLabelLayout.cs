using UnityEditor;
using UnityEngine;

namespace RideSafe.Module00.Editor
{
    /// <summary>
    /// Widens the two vehicle name cards of PF_Module00_UI to 340 frame px around their centres,
    /// so the Spanish names fit on one line. Edits the prefab in place instead of regenerating
    /// it: a regeneration gives its objects new internal ids and would break the scene
    /// references into it. UIModules.Module00 builds the same layout, so both agree.
    /// Idempotent: sets absolute values.
    /// </summary>
    public static class Module00VehicleLabelLayout
    {
        public const string PrefabPath = "Assets/_Project/UI/Prefabs/PF_Module00_UI.prefab";
        public const float CardWidth = 340f;
        private const float Pad = 16f / 2.8f; // UIBuilderKit.PadFor(2.8)

        private static readonly (string Name, float Centre)[] Cards =
        {
            ("Option_ebike", 267f),
            ("Option_escooter", 763f)
        };

        [MenuItem("RideSafe/Módulo 0/Ensanchar etiquetas de vehículo")]
        public static void Apply()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                foreach (var card in Cards)
                    Layout(Find(root.transform, card.Name), card.Centre);
                LayoutConfirmationBar(Find(root.transform, "ConfirmationBar"));
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[Module00] Vehicle cards widened to " + CardWidth + " px in " + PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void Layout(RectTransform card, float centre)
        {
            float x0 = centre - CardWidth / 2f;
            card.anchoredPosition = new Vector2(x0 - Pad, card.anchoredPosition.y);
            card.sizeDelta = new Vector2(CardWidth + 2f * Pad, card.sizeDelta.y);
            foreach (string text in new[] { "Title", "Description", "StateCaption" })
            {
                RectTransform rect = Find(card, text);
                rect.sizeDelta = new Vector2(CardWidth - 16f, rect.sizeDelta.y);
            }
            RectTransform check = Find(card, "Check");
            check.anchoredPosition = new Vector2(Pad + CardWidth - 46f, check.anchoredPosition.y);

            // At runtime the card hugs its (translated) text, and grows for the check when selected.
            RideSafe.UI.FitCardToContent fit = card.GetComponent<RideSafe.UI.FitCardToContent>();
            if (fit == null)
                fit = card.gameObject.AddComponent<RideSafe.UI.FitCardToContent>();
            fit.Configure(card.GetComponent<UnityEngine.UI.Toggle>(), Find(card, "Title").GetComponent<TMPro.TMP_Text>(),
                          Find(card, "Description").GetComponent<TMPro.TMP_Text>(), check, Find(card, "StateCaption"));
        }

        /// <summary>
        /// "Patineta eléctrica de pie seleccionada —" and "¿confirmar para continuar?" ran into each
        /// other side by side: two lines, left of the buttons; the sentence shrinks to fit one line.
        /// </summary>
        private static void LayoutConfirmationBar(RectTransform bar)
        {
            RectTransform selected = Find(bar, "SelectedText");
            selected.anchoredPosition = new Vector2(26.4f, -16.4f);
            selected.sizeDelta = new Vector2(318f, 28f);
            TMPro.TMP_Text sentence = selected.GetComponent<TMPro.TMP_Text>();
            sentence.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            sentence.enableAutoSizing = true;
            sentence.fontSizeMin = 12f;
            sentence.fontSizeMax = 16f;
            RectTransform prompt = Find(bar, "Prompt");
            prompt.anchoredPosition = new Vector2(26.4f, -44.4f);
            prompt.sizeDelta = new Vector2(318f, 28f);
        }

        private static RectTransform Find(Transform root, string name)
        {
            foreach (RectTransform t in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (t.name == name)
                    return t;
            }
            throw new System.InvalidOperationException(root.name + " has no " + name);
        }
    }
}
