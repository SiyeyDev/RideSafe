using NUnit.Framework;
using RideSafe.Tutorial.Presentation;
using TMPro;
using UnityEngine;

namespace RideSafe.Tutorial.Tests
{
    /// <summary>9-slice sprites (borders) must be drawn Sliced and sized, never stretched as Simple.</summary>
    internal static class SlicedSprites
    {
        public static Sprite Create(Texture2D texture, float border)
        {
            return Sprite.Create(texture, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }
    }

    public class SpriteTargetHighlightTests
    {
        private Texture2D _texture;
        private GameObject _go;
        private SpriteTargetHighlight _ring;
        private SpriteRenderer _renderer;

        [SetUp]
        public void SetUp()
        {
            _texture = new Texture2D(256, 256);
            _go = new GameObject("Focus Ring");
            _renderer = _go.AddComponent<SpriteRenderer>();
            _renderer.sprite = SlicedSprites.Create(_texture, 64f);
            _ring = _go.AddComponent<SpriteTargetHighlight>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_texture);
        }

        [Test]
        public void Frame_DrawsSliced_SizedToTheTarget_WithUniformScale()
        {
            ITargetHighlightView view = _ring;
            view.Show();
            view.Frame(new Vector3(1f, 2f, 3f), new Vector2(0.4f, 0.1f), Quaternion.identity, true);

            Assert.AreEqual(SpriteDrawMode.Sliced, _renderer.drawMode);
            Vector3 scale = _go.transform.localScale;
            Assert.AreEqual(scale.x, scale.y, 1e-5f, "uniform scale keeps the corners round");
            Vector2 world = _renderer.size * scale.x;
            Assert.Greater(world.x, 0.8f);
            Assert.Greater(world.y, 0.2f);
            Assert.Greater(world.x / world.y, 3f, "a wide target gets a wide ring, not a stretched one");
            Assert.AreEqual(new Vector3(1f, 2f, 3f), _go.transform.position);
        }

        [Test]
        public void Corners_KeepTheirSize_WhateverTheTarget()
        {
            ITargetHighlightView view = _ring;
            view.Frame(Vector3.zero, new Vector2(0.05f, 0.05f), Quaternion.identity, true);
            float small = _go.transform.localScale.x;
            view.Frame(Vector3.zero, new Vector2(1f, 0.3f), Quaternion.identity, true);
            Assert.AreEqual(small, _go.transform.localScale.x, 1e-5f);
        }

        [Test]
        public void ShowAndHide_ToggleTheRing()
        {
            ITargetHighlightView view = _ring;
            view.Hide();
            Assert.IsFalse(_go.activeSelf);
            view.Show();
            Assert.IsTrue(_go.activeSelf);
        }
    }

    public class TextBackplateTests
    {
        private Texture2D _texture;
        private GameObject _label;
        private SpriteRenderer _plate;

        [SetUp]
        public void SetUp()
        {
            _texture = new Texture2D(256, 256);
            _label = new GameObject("Label", typeof(TextMeshPro));
            GameObject plate = new GameObject("Backplate");
            plate.transform.SetParent(_label.transform, false);
            _plate = plate.AddComponent<SpriteRenderer>();
            _plate.sprite = SlicedSprites.Create(_texture, 64f);
            TextBackplate backplate = _label.AddComponent<TextBackplate>();
            SerializedFields.Set(backplate, "_plate", _plate);
            TextMeshPro text = _label.GetComponent<TextMeshPro>();
            text.rectTransform.sizeDelta = new Vector2(40f, 5f); // room for one line
            text.text = "Choose your language / Elige tu idioma";
            backplate.Refit();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_label);
            Object.DestroyImmediate(_texture);
        }

        [Test]
        public void Plate_IsSliced_WithUniformScale()
        {
            Assert.AreEqual(SpriteDrawMode.Sliced, _plate.drawMode);
            Assert.AreEqual(_plate.transform.localScale.x, _plate.transform.localScale.y, 1e-5f);
        }

        [Test]
        public void Plate_IsWiderThanTall_ForAOneLineText()
        {
            Vector2 size = _plate.size;
            Assert.Greater(size.x / size.y, 2f);
            Assert.IsTrue(_plate.enabled);
        }
    }
}
