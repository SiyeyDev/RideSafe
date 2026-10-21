using System.Collections.Generic;
using NUnit.Framework;
using RideSafe.UI;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Titles floated above their box and buttons below it: a panel's pieces were laid out
    /// one by one with nothing behind them. The frame has to hold every visible piece, with the
    /// same margin on every side, and follow them when a list grows.
    /// </summary>
    public class EncloseTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private RectTransform _panel, _frame, _title, _list;
        private Enclose _enclose;

        [SetUp]
        public void SetUp()
        {
            GameObject canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            _objects.Add(canvas);
            _panel = Rect(canvas.transform, "Panel", 0f, 0f, 1002f, 556f);
            _frame = Rect(_panel, "Frame", 0f, 0f, 10f, 10f);
            _title = Rect(_panel, "Title", 300f, 60f, 700f, 100f);
            _list = Rect(_panel, "List", 250f, 120f, 750f, 400f);
            _enclose = _frame.gameObject.AddComponent<Enclose>();
            _enclose.Configure(new[] { _title, _list }, new Vector4(24f, 20f, 24f, 28f), 6f);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects)
                Object.DestroyImmediate(go);
            _objects.Clear();
        }

        private static RectTransform Rect(Transform parent, string name, float x0, float y0, float x1, float y1)
        {
            RectTransform rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x0, -y0);
            rect.sizeDelta = new Vector2(x1 - x0, y1 - y0);
            return rect;
        }

        [Test]
        public void TheFrame_HoldsEveryPiece_WithItsMarginAndTheAtlasPadding()
        {
            _enclose.Refresh();
            // Visible art from (250-24, 60-20) to (750+24, 400+28); the rect adds 6 of padding per side.
            Assert.AreEqual(new Vector2(250f - 24f - 6f, -(60f - 20f - 6f)), _frame.anchoredPosition);
            Assert.AreEqual(new Vector2(500f + 48f + 12f, 340f + 48f + 12f), _frame.sizeDelta);
        }

        [Test]
        public void TheFrame_FollowsAGrowingList()
        {
            _enclose.Refresh();
            _list.sizeDelta = new Vector2(500f, 420f);
            _enclose.Refresh();
            Assert.AreEqual(420f + 60f + 48f + 12f, _frame.sizeDelta.y, 0.01f, "40 above the title, 420 of list, margins and padding");
        }

        [Test]
        public void AHiddenPiece_DoesNotStretchTheFrame()
        {
            RectTransform hidden = Rect(_panel, "Hidden", 0f, 500f, 1002f, 556f);
            hidden.gameObject.SetActive(false);
            _enclose.Configure(new[] { _title, _list, hidden }, new Vector4(24f, 20f, 24f, 28f), 6f);
            _enclose.Refresh();
            Assert.AreEqual(340f + 48f + 12f, _frame.sizeDelta.y, 0.01f);
        }

        [Test]
        public void TheFrame_SitsBehindThePieces()
        {
            _frame.SetAsLastSibling();
            _enclose.Refresh();
            Assert.AreEqual(0, _frame.GetSiblingIndex());
        }

        [Test]
        public void TwoFrames_InOnePanel_StayBehind_WithoutSwappingEveryFrame()
        {
            // The module 0 screens have two boxes (2026-10-21): the title's and the options'.
            RectTransform second = Rect(_panel, "HeaderFrame", 0f, 0f, 10f, 10f);
            Enclose other = second.gameObject.AddComponent<Enclose>();
            other.Configure(new[] { _title }, new Vector4(24f, 4f, 24f, 12f), 6f);
            _enclose.Refresh();
            other.Refresh();
            int first = _frame.GetSiblingIndex(), then = second.GetSiblingIndex();
            _enclose.Refresh();
            other.Refresh();
            Assert.AreEqual(first, _frame.GetSiblingIndex(), "a frame that moves every LateUpdate dirties its canvas");
            Assert.AreEqual(then, second.GetSiblingIndex());
            Assert.Less(Mathf.Max(first, then), _title.GetSiblingIndex(), "both behind the pieces");
        }
    }
}
