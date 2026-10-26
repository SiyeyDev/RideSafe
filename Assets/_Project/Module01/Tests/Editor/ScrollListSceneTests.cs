using System.Linq;
using NUnit.Framework;
using RideSafe.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// «Revisa tus elecciones» y «Mi Elección» con las filas en una ventana que se desplaza (PO,
    /// 2026-10-26: «un scrollview para esta parte para que no quede tan largo sino que mantenga las
    /// proporciones»). Contra la escena v5, la que se juega.
    /// </summary>
    public class ScrollListSceneTests
    {
        private const string ScenePath = "Assets/_Project/Scene/RideSafe_Garaje_v5.unity";

        [Test]
        public void Revisa_tus_elecciones_desplaza_sus_filas_y_la_caja_crece_con_tope()
        {
            WithScene(all =>
            {
                RectTransform modal = (RectTransform)all.Single(t => t.name == "Modal" && t.parent.name == "ReviewChoices");
                RectTransform rows = (RectTransform)modal.Find("ListBox/Viewport/Rows");
                Assert.IsNotNull(rows, "the rows live in ListBox/Viewport");
                AssertScrolls(rows);
                SerializedObject grow = new SerializedObject(modal.GetComponent<GrowWithContent>());
                Assert.AreSame(rows, grow.FindProperty("_scrolledRows").objectReferenceValue);
                Assert.AreSame(rows.parent, grow.FindProperty("_content").objectReferenceValue, "the window grows, not the rows");
                Assert.Greater(grow.FindProperty("_maxExtra").floatValue, 0f, "the box grows up to a cap");
            });
        }

        [Test]
        public void Mi_Eleccion_desplaza_sus_filas_en_una_ventana_de_alto_maximo()
        {
            WithScene(all =>
            {
                ComparisonView view = all.Select(t => t.GetComponent<ComparisonView>()).Single(v => v != null);
                RectTransform column = (RectTransform)view.transform.Find("LeftColumn");
                RectTransform rows = (RectTransform)column.Find("Viewport/Rows");
                Assert.IsNotNull(rows, "the rows live in LeftColumn/Viewport");
                AssertScrolls(rows);
                SerializedObject serialized = new SerializedObject(view);
                Assert.AreSame(rows, serialized.FindProperty("_leftRows").objectReferenceValue);
                Assert.AreSame(column, serialized.FindProperty("_column").objectReferenceValue, "the note follows the whole box");
                Assert.AreSame(rows.parent.GetComponent<LayoutElement>(), serialized.FindProperty("_viewport").objectReferenceValue);
                Assert.IsNotNull(column.Find("EmptyState"), "the empty notice stays in the box, outside the window");
            });
        }

        private static void AssertScrolls(RectTransform rows)
        {
            RectTransform viewport = (RectTransform)rows.parent;
            Assert.IsNotNull(viewport.GetComponent<RectMask2D>(), "rows past the window are clipped");
            Image catcher = viewport.GetComponent<Image>();
            Assert.IsTrue(catcher != null && catcher.raycastTarget, "the ray can drag the list anywhere in the window");
            ScrollRect scroll = viewport.GetComponent<ScrollRect>();
            Assert.IsNotNull(scroll);
            Assert.AreSame(rows, scroll.content);
            Assert.AreSame(viewport, scroll.viewport);
            Assert.IsTrue(scroll.vertical);
            Assert.IsFalse(scroll.horizontal);
            Assert.AreEqual(ScrollRect.MovementType.Clamped, scroll.movementType, "no rubber band in VR");
            Assert.IsNotNull(scroll.verticalScrollbar, "a bar shows there is more and can be dragged");
            Assert.AreEqual(ScrollRect.ScrollbarVisibility.AutoHide, scroll.verticalScrollbarVisibility, "only when it does not fit");
            ContentSizeFitter fit = rows.GetComponent<ContentSizeFitter>();
            Assert.IsTrue(fit != null && fit.verticalFit == ContentSizeFitter.FitMode.PreferredSize, "the rows take their full height");
            Assert.AreEqual(new Vector2(0f, 1f), rows.anchorMin);
            Assert.AreEqual(Vector2.one, rows.anchorMax);
            Assert.AreEqual(1f, rows.pivot.y, "rows hang from the top of the window");
        }

        private static void WithScene(System.Action<Transform[]> check)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.isLoaded;
            if (opened)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                check(scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray());
            }
            finally
            {
                if (opened)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
