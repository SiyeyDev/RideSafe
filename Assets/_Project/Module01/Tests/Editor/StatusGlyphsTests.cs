using NUnit.Framework;
using RideSafe.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// The report's icons are a traffic light (PO, 2026-10-20): the status colours the icon,
    /// never the row's background or text.
    /// </summary>
    public class StatusGlyphsTests
    {
        private GameObject _row;
        private Image _icon;

        [SetUp]
        public void SetUp()
        {
            _row = new GameObject("Row", typeof(RectTransform));
            GameObject icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(_row.transform, false);
            _icon = icon.GetComponent<Image>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_row);

        [Test]
        public void TheStatus_ColoursTheIcon()
        {
            Color green = new Color(0.18f, 0.61f, 0.34f), red = new Color(0.84f, 0.27f, 0.25f);
            StatusGlyphs glyphs = new StatusGlyphs { Colors = new[] { green, red } };
            glyphs.Bind(_row.transform, new ComparisonEntry(ItemStatus.Selected, "Casco"));
            Assert.AreEqual(green, _icon.color);
            glyphs.Bind(_row.transform, new ComparisonEntry(ItemStatus.CoreOmitted, "Calzado"));
            Assert.AreEqual(red, _icon.color);
        }

        [Test]
        public void WithoutAColour_TheIconKeepsItsArt()
        {
            _icon.color = Color.gray;
            new StatusGlyphs().Bind(_row.transform, new ComparisonEntry(ItemStatus.NotSuited, "Bufanda"));
            Assert.AreEqual(Color.white, _icon.color);
        }
    }
}
