using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RideSafe.Module01.Editor;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// El nombre del elemento en el popup «¿Quieres añadir…?».
    /// <para>
    /// Los nombres son la columna «Elemento» de la hoja del cliente y llegan a 67 caracteres.
    /// En una sola línea encogían hasta 15 y aun así tocaban los bordes (PO, 2026-10-26): ahora
    /// van en dos líneas con margen.
    /// </para>
    /// </summary>
    public class PopupNameLayoutTests
    {
        private const string ScenePath = "Assets/_Project/Scene/RideSafe_Garaje_v5.unity";
        private const float MinSideMargin = 40f;

        [Test]
        public void El_nombre_envuelve_con_margen_y_el_mas_largo_cabe_en_dos_lineas()
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                RectTransform name = scene.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<RectTransform>(true))
                    .First(t => t.name == "ItemName" && t.parent.name == "Confirmation");
                RectTransform popup = (RectTransform)name.parent;
                TMP_Text title = name.GetComponent<TMP_Text>();

                Assert.AreEqual(TextWrappingModes.Normal, title.textWrappingMode, "one line shrank the long names into the edges");
                float margin = (popup.rect.width - name.rect.width) * 0.5f;
                Assert.GreaterOrEqual(margin, MinSideMargin, "the name touched the popup's edges");

                Dictionary<string, string[]> terms = I2TermsTests.ReadProjectTerms();
                string longest = Module01Library.All
                    .Select(e => terms.TryGetValue(e.NameKey, out string[] t) ? t[1] : string.Empty)
                    .OrderByDescending(s => s.Length).First();
                // Laid out as the game lays it out, auto-size included; the scene's own text is put back.
                string original = title.text;
                try
                {
                    title.text = longest;
                    title.ForceMeshUpdate(true, true);
                    Assert.LessOrEqual(title.textInfo.lineCount, 2, "«" + longest + "» takes more than two lines");
                    Assert.Greater(title.fontSize, title.fontSizeMin + .01f,
                        "«" + longest + "» had to shrink to its minimum: the next longer name gets cut");
                    Assert.LessOrEqual(title.GetRenderedValues(true).y, name.rect.height + 1f,
                        "«" + longest + "» does not fit the name's box");
                }
                finally
                {
                    title.text = original;
                    title.ForceMeshUpdate(true, true);
                }
            }
            finally
            {
                if (openedHere)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
