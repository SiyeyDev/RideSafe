using NUnit.Framework;
using RideSafe.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// El video se ve con sus colores (PO, 2026-10-28, «quítale ese filtro azul»). La pantalla es
    /// un RawImage que el builder pinta azul marino para que sin video no quede un hueco blanco, y
    /// un RawImage multiplica su textura por ese color: cada fotograma salía azul y oscuro.
    /// </summary>
    public class VideoSurfaceColorTests
    {
        private GameObject _root;
        private ExplanationView _view;
        private RawImage _surface;
        private RenderTexture _texture;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Explanation", typeof(RectTransform));
            _surface = new GameObject("VideoSurface", typeof(RectTransform)).AddComponent<RawImage>();
            _surface.transform.SetParent(_root.transform, false);
            _surface.color = new Color32(0x12, 0x3D, 0x6B, 0xFF);
            _view = _root.AddComponent<ExplanationView>();
            var so = new SerializedObject(_view);
            so.FindProperty("_videoSurface").objectReferenceValue = _surface;
            so.ApplyModifiedPropertiesWithoutUndo();
            _texture = new RenderTexture(16, 9, 0);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_texture);
        }

        [Test]
        public void Con_video_la_pantalla_no_tine_los_fotogramas()
        {
            _view.ShowVideo(_texture);

            Assert.AreSame(_texture, _surface.texture);
            Assert.AreEqual(Color.white, _surface.color, "el color del RawImage tiñe cada fotograma");
        }
    }
}
