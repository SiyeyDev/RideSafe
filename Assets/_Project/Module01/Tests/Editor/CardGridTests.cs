using System.Collections.Generic;
using NUnit.Framework;
using RideSafe.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// «Revisa tus elecciones» y «Evaluación de tus elecciones» como cuadrícula de tarjetas (PO,
    /// 2026-11-04): imagen del EPP, su nombre y, solo al evaluar, el icono del estado arriba a la
    /// derecha. Una sola tarjeta y una sola cuadrícula para los dos paneles.
    /// </summary>
    public class CardGridTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private CardGrid _grid;
        private RectTransform _content;
        private StatusGlyphs _glyphs;
        private Sprite _render;

        [SetUp]
        public void SetUp()
        {
            ItemCard card = NewCard();
            card.gameObject.SetActive(false);

            GameObject panel = Track(new GameObject("ReviewChoices", typeof(RectTransform)));
            _content = (RectTransform)new GameObject("Cards", typeof(RectTransform), typeof(GridLayoutGroup)).transform;
            _content.SetParent(panel.transform, false);
            _grid = panel.AddComponent<CardGrid>();
            _grid.Configure(card, _content);

            Texture2D texture = Track(new Texture2D(4, 4));
            _render = Track(Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), Vector2.one * 0.5f));

            Texture2D glyphTexture = Track(new Texture2D(2, 2));
            Sprite check = Track(Sprite.Create(glyphTexture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f));
            Sprite cross = Track(Sprite.Create(glyphTexture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f));
            Sprite dash = Track(Sprite.Create(glyphTexture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f));
            _glyphs = new StatusGlyphs
            {
                Sprites = new[] { check, cross, dash, cross, cross, dash },
                Colors = new[] { Color.green, Color.red, Color.yellow, Color.red, Color.red, Color.yellow }
            };
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _objects)
                if (o != null)
                    Object.DestroyImmediate(o);
            _objects.Clear();
        }

        private T Track<T>(T o) where T : Object
        {
            _objects.Add(o);
            return o;
        }

        /// <summary>La tarjeta del prefab: Picture, Placeholder, Name y Status.</summary>
        private ItemCard NewCard()
        {
            GameObject root = Track(new GameObject("PF_EppCard", typeof(RectTransform)));
            Image picture = new GameObject("Picture", typeof(RectTransform)).AddComponent<Image>();
            picture.transform.SetParent(root.transform, false);
            GameObject placeholder = new GameObject("Placeholder", typeof(RectTransform));
            placeholder.transform.SetParent(root.transform, false);
            TextMeshProUGUI label = new GameObject("Name", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(root.transform, false);
            Image status = new GameObject("Status", typeof(RectTransform)).AddComponent<Image>();
            status.transform.SetParent(root.transform, false);

            ItemCard card = root.AddComponent<ItemCard>();
            SerializedObject serialized = new SerializedObject(card);
            serialized.FindProperty("_picture").objectReferenceValue = picture;
            serialized.FindProperty("_placeholder").objectReferenceValue = placeholder;
            serialized.FindProperty("_name").objectReferenceValue = label;
            serialized.FindProperty("_status").objectReferenceValue = status;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return card;
        }

        private static ComparisonEntry Entry(ItemStatus status, string name, string id, Sprite image = null) =>
            new ComparisonEntry(status, name) { ItemId = id, Image = image };

        [Test]
        public void Show_MakesOneCardPerEntry_InOrder()
        {
            _grid.Show(new[]
            {
                Entry(ItemStatus.Selected, "Casco abrochado", "helmet_ok", _render),
                Entry(ItemStatus.NoEffect, "Coderas", "elbow_pads", _render),
                Entry(ItemStatus.NotSuited, "Paraguas de mano", "umbrella", _render),
            });

            Assert.AreEqual(3, _grid.Cards.Count);
            Assert.AreEqual(3, _content.childCount, "the cards live in the grid's content");
            Assert.AreEqual("helmet_ok", _grid.Cards[0].ItemId);
            Assert.AreEqual("Paraguas de mano", _grid.Cards[2].Name);
            Assert.AreSame(_render, _grid.Cards[1].Picture);
        }

        [Test]
        public void Show_Again_ReplacesTheCards()
        {
            _grid.Show(new[] { Entry(ItemStatus.Selected, "Casco", "helmet_ok"), Entry(ItemStatus.Selected, "Calzado", "shoes_ok") });
            _grid.Show(new[] { Entry(ItemStatus.Selected, "Alforja", "pannier") });

            Assert.AreEqual(1, _grid.Cards.Count);
            Assert.AreEqual(1, _content.childCount, "«Seguir eligiendo» and back must not pile cards up");
            Assert.AreEqual("pannier", _grid.Cards[0].ItemId);
        }

        [Test]
        public void Review_HasNoStatusIcon()
        {
            // «Revisa tus elecciones» no adelanta la respuesta (PO, 2026-11-04).
            _grid.Show(new[] { Entry(ItemStatus.Selected, "Casco", "helmet_ok", _render) });
            Assert.IsFalse(_grid.Cards[0].ShowsStatus);
        }

        [Test]
        public void Evaluation_ShowsTheIconOfEachState()
        {
            _grid.Show(new[]
            {
                Entry(ItemStatus.Selected, "Casco", "helmet_ok", _render),
                Entry(ItemStatus.NoEffect, "Coderas", "elbow_pads", _render),
                Entry(ItemStatus.NotSuited, "Paraguas", "umbrella", _render),
            }, _glyphs);

            for (int i = 0; i < 3; i++)
                Assert.IsTrue(_grid.Cards[i].ShowsStatus, "card " + i);
            Assert.AreSame(_glyphs.For(ItemStatus.Selected), _grid.Cards[0].StatusSprite);
            Assert.AreEqual(Color.green, _grid.Cards[0].StatusColor);
            Assert.AreSame(_glyphs.For(ItemStatus.NoEffect), _grid.Cards[1].StatusSprite);
            Assert.AreEqual(Color.yellow, _grid.Cards[1].StatusColor);
            Assert.AreSame(_glyphs.For(ItemStatus.NotSuited), _grid.Cards[2].StatusSprite);
            Assert.AreEqual(Color.red, _grid.Cards[2].StatusColor);
        }

        [Test]
        public void ACardWithoutRender_ShowsThePlaceholderAndItsName()
        {
            // Un EPP nuevo que todavía no se fotografió no rompe el panel.
            Assert.DoesNotThrow(() => _grid.Show(new[] { Entry(ItemStatus.Selected, "Elemento nuevo", "new_item") }));
            ItemCard card = _grid.Cards[0];
            Assert.IsNull(card.Picture);
            Assert.IsTrue(card.ShowsPlaceholder);
            Assert.AreEqual("Elemento nuevo", card.Name);
        }

        [Test]
        public void ACardWithRender_HidesThePlaceholder()
        {
            _grid.Show(new[] { Entry(ItemStatus.Selected, "Casco", "helmet_ok", _render) });
            Assert.IsFalse(_grid.Cards[0].ShowsPlaceholder);
        }

        [Test]
        public void TheTemplateIsNeverShown()
        {
            _grid.Show(new[] { Entry(ItemStatus.Selected, "Casco", "helmet_ok") });
            foreach (ItemCard card in _grid.Cards)
                Assert.IsTrue(card.gameObject.activeSelf);
        }
    }
}
