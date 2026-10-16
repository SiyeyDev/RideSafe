using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.Help.Tests
{
    public class TargetHighlighterTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private TargetHighlighter _highlighter;

        [SetUp]
        public void SetUp()
        {
            GameObject go = new GameObject("Highlighter");
            _objects.Add(go);
            _highlighter = go.AddComponent<TargetHighlighter>();
            Material halo = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _objects.Add(halo);
            _highlighter.Configure(halo);
        }

        [TearDown]
        public void TearDown()
        {
            _highlighter.Clear();
            foreach (Object o in _objects)
                Object.DestroyImmediate(o);
            _objects.Clear();
        }

        private Image CanvasImage(Color rendererColor)
        {
            GameObject canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            _objects.Add(canvas);
            GameObject go = new GameObject("Card", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            Image image = go.GetComponent<Image>();
            image.canvasRenderer.SetColor(rendererColor);
            return image;
        }

        private static HelpTarget Ui(Image image) => new HelpTarget(image.transform, InteractionKind.Interface, image);

        [Test]
        public void Pulse_DimsTheGraphic_AndRestoresItExactly()
        {
            Color original = new Color(1f, 0.9f, 0.8f, 1f);
            Image image = CanvasImage(original);
            _highlighter.Highlight(new[] { Ui(image) });
            _highlighter.Tick(0.8f); // half of the 1.6 s cycle: darkest
            Color dimmed = image.canvasRenderer.GetColor();
            Assert.AreEqual(original.r * 0.85f, dimmed.r, 0.01f);
            Assert.AreEqual(original.a, dimmed.a, 1e-4f, "never fades the control out");

            _highlighter.Tick(0.3f); // cut mid-pulse
            _highlighter.Clear();
            Assert.AreEqual(original, image.canvasRenderer.GetColor());
        }

        [Test]
        public void SeveralGraphics_PulseInStep()
        {
            Image a = CanvasImage(Color.white), b = CanvasImage(Color.white);
            _highlighter.Highlight(new[] { Ui(a), Ui(b) });
            _highlighter.Tick(0.5f);
            Assert.AreEqual(a.canvasRenderer.GetColor(), b.canvasRenderer.GetColor());
            Assert.AreEqual(2, _highlighter.HighlightedCount);
        }

        [Test]
        public void Highlight_RestoresTargetsNoLongerWanted()
        {
            Image a = CanvasImage(Color.white), b = CanvasImage(Color.white);
            _highlighter.Highlight(new[] { Ui(a), Ui(b) });
            _highlighter.Tick(0.8f);
            _highlighter.Highlight(new[] { Ui(b) });
            Assert.AreEqual(Color.white, a.canvasRenderer.GetColor());
            Assert.AreEqual(1, _highlighter.HighlightedCount);
        }

        [Test]
        public void Halo_AddsAHullUnderEachMesh_AndRemovesIt()
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _objects.Add(cube);
            _highlighter.Highlight(new[] { new HelpTarget(cube.transform, InteractionKind.Object3D, null) });
            Transform halo = cube.transform.Find(TargetHighlighter.HaloName);
            Assert.IsNotNull(halo);
            Assert.AreSame(cube.GetComponent<MeshFilter>().sharedMesh, halo.GetComponent<MeshFilter>().sharedMesh);
            Assert.IsTrue((halo.gameObject.hideFlags & HideFlags.DontSave) != 0);

            _highlighter.Highlight(new[] { new HelpTarget(cube.transform, InteractionKind.Object3D, null) });
            Assert.AreEqual(1, cube.GetComponentsInChildren<MeshFilter>().Length - 1, "highlighting twice adds no second hull");

            _highlighter.Clear();
            Assert.IsNull(cube.transform.Find(TargetHighlighter.HaloName));
        }

        [Test]
        public void Pulse_FollowsTheModulesOwnTint_AndRestoresIt()
        {
            Image image = CanvasImage(Color.white);
            _highlighter.Highlight(new[] { Ui(image) });
            _highlighter.Tick(0.4f);
            Color hover = new Color(0.9f, 0.9f, 1f, 1f);
            image.canvasRenderer.SetColor(hover); // a ColorTint button's own hover tween
            _highlighter.Tick(0.4f);
            _highlighter.Clear();
            Assert.AreEqual(hover, image.canvasRenderer.GetColor(), "not the stale colour captured before the hover");
        }
    }
}
