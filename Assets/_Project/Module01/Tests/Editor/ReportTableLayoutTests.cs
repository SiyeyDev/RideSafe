using System.Linq;
using NUnit.Framework;
using RideSafe.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// «Mi Elección» como tabla (PO, 2026-10-26): «en una columna se ven los elementos y en la otra
    /// si fue correcto, no necesario o incorrecto».
    /// <para>
    /// Contra la escena v5, que es lo que se juega: el prefab tenía la plantilla bien y la
    /// escena guardaba solo cinco palabras de estado, así que las filas amarillas salían sin la suya.
    /// </para>
    /// </summary>
    public class ReportTableLayoutTests
    {
        private const string ScenePath = "Assets/_Project/Scene/RideSafe_Garaje_v5.unity";

        [Test]
        public void Cada_estado_tiene_su_palabra_y_va_en_su_propia_columna()
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                ComparisonView view = scene.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<ComparisonView>(true)).Single();

                string[] keys = ReadStateKeys(view);
                Assert.AreEqual(System.Enum.GetValues(typeof(ItemStatus)).Length, keys.Length, "one word per ItemStatus");
                Assert.AreEqual("M1/Legend_Correct", keys[(int)ItemStatus.Selected]);
                Assert.AreEqual("M1/Legend_Incorrect", keys[(int)ItemStatus.NotSuited]);
                Assert.AreEqual("M1/Legend_NoEffect", keys[(int)ItemStatus.NoEffect], "the yellow rows showed no word");

                RectTransform template = (RectTransform)new SerializedObject(view).FindProperty("_rowTemplate").objectReferenceValue;
                Transform state = template.Find("State");
                LayoutElement column = state.GetComponent<LayoutElement>();
                Assert.GreaterOrEqual(column.minWidth, ComparisonView.StateColumnWidth - .01f, "the state column must not shrink");
                Assert.AreEqual(column.minWidth, column.preferredWidth, .01f, "fixed width: the words line up as a column");
                Assert.IsTrue(column.flexibleWidth <= 0f, "the name takes the spare width, not the state");
                TMP_Text word = state.GetComponent<TMP_Text>();
                Assert.AreEqual(HorizontalAlignmentOptions.Left, word.horizontalAlignment, "a column reads from its left edge");
                Assert.AreEqual(TextWrappingModes.NoWrap, word.textWrappingMode);
                // «NO NECESARIO», the widest word, in the column's caps style.
                float needed = word.GetPreferredValues(word.fontStyle.HasFlag(FontStyles.UpperCase) ? "NO NECESARIO" : "No necesario", 0f, 0f).x;
                Assert.LessOrEqual(needed, column.minWidth, "«No necesario» spills out of its column");
            }
            finally
            {
                if (openedHere)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static string[] ReadStateKeys(ComparisonView view)
        {
            SerializedProperty keys = new SerializedObject(view).FindProperty("_stateKeys");
            return Enumerable.Range(0, keys.arraySize).Select(i => keys.GetArrayElementAtIndex(i).stringValue).ToArray();
        }
    }
}
