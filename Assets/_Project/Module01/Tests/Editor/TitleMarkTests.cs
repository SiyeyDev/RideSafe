using System.Collections.Generic;
using NUnit.Framework;
using RideSafe.UI;
using TMPro;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// The company mark sits at the top left of a panel, on the line of its title (Cindy, 2026-10-21).
    /// Localisation moves that line —a longer Spanish title wraps and a centred one climbs— so the
    /// mark has to find the first line at runtime, not where the English prefab left it.
    /// </summary>
    public class TitleMarkTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private TextMeshProUGUI _title;
        private RectTransform _mark;
        private TitleMark _titleMark;

        [SetUp]
        public void SetUp()
        {
            GameObject canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            _objects.Add(canvas);
            RectTransform panel = (RectTransform)new GameObject("Panel", typeof(RectTransform)).transform;
            panel.SetParent(canvas.transform, false);
            panel.sizeDelta = new Vector2(1000f, 600f);

            _title = new GameObject("Title", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            _title.transform.SetParent(panel, false);
            RectTransform title = _title.rectTransform;
            title.anchorMin = title.anchorMax = title.pivot = new Vector2(0f, 1f);
            title.anchoredPosition = new Vector2(100f, -40f);
            title.sizeDelta = new Vector2(400f, 120f);
            _title.fontSize = 30f;
            _title.alignment = TextAlignmentOptions.Center;
            _title.text = "Uno";

            _mark = (RectTransform)new GameObject("BrandMark", typeof(RectTransform)).transform;
            _mark.SetParent(panel, false);
            _mark.sizeDelta = new Vector2(44f, 24f);
            _titleMark = _mark.gameObject.AddComponent<TitleMark>();
            _titleMark.Configure(_title, -12f);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects)
                Object.DestroyImmediate(go);
            _objects.Clear();
        }

        private float MarkCentreY()
        {
            Vector3[] corners = new Vector3[4];
            _mark.GetWorldCorners(corners);
            return _title.transform.InverseTransformPoint((corners[0] + corners[1]) * 0.5f).y;
        }

        private void AssertOnFirstLine()
        {
            _title.ForceMeshUpdate();
            TMP_LineInfo line = _title.textInfo.lineInfo[0];
            float y = MarkCentreY();
            Assert.Greater(y, line.baseline, "the mark's centre sits above the first line's baseline");
            Assert.Less(y, line.baseline + 0.75f * _title.fontSize, "and below its capitals' top");
        }

        [Test]
        public void TheMark_SitsOnTheTitlesFirstLine()
        {
            _titleMark.Refresh();
            AssertOnFirstLine();
        }

        [Test]
        public void TheMark_FollowsTheFirstLine_WhenTheTitleWraps()
        {
            _titleMark.Refresh();
            float oneLine = MarkCentreY();
            _title.text = "Después de tu revisión, ¿qué decides con este vehículo?";
            _titleMark.Refresh();
            AssertOnFirstLine();
            Assert.Greater(MarkCentreY(), oneLine + 5f, "a centred title that wraps climbs, and the mark with it");
        }

        [Test]
        public void TheMark_KeepsItsPlace_LeftOfTheTitlesBox()
        {
            _titleMark.Refresh();
            Vector3[] corners = new Vector3[4];
            _mark.GetWorldCorners(corners);
            float left = _title.transform.InverseTransformPoint(corners[0]).x;
            Assert.AreEqual(_title.rectTransform.rect.xMin - 12f, left, 0.01f);
        }
    }
}
