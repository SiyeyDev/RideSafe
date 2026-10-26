using System.Collections.Generic;
using NUnit.Framework;
using RideSafe.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// «Revisa tus elecciones»: con siete elementos las filas se salían de la caja y pisaban el
    /// aviso «Al confirmar se evaluará esta sección» y los botones.
    /// </summary>
    public class GrowWithContentTests
    {
        private const float RowHeight = 40f;
        private readonly List<GameObject> _objects = new List<GameObject>();
        private RectTransform _modal, _box, _rows, _notice;
        private GrowWithContent _grow;

        [SetUp]
        public void SetUp()
        {
            GameObject canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            _objects.Add(canvas);
            _modal = Rect(canvas.transform, "Modal", 0f, -50f, 700f, 450f);
            _box = Rect(_modal, "ListBox", 40f, -125f, 630f, 150f);
            _rows = Rect(_box, "Rows", 19f, -17f, 590f, 119f);
            VerticalLayoutGroup layout = _rows.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            _notice = Rect(_modal, "LockNotice", 38f, -284f, 630f, 60f);
            _grow = _modal.gameObject.AddComponent<GrowWithContent>();
            _grow.Configure(_rows, _box, new[] { _modal }, new[] { _notice }, _modal);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects)
                Object.DestroyImmediate(go);
            _objects.Clear();
        }

        private static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        private void SetRows(int count)
        {
            for (int i = _rows.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(_rows.GetChild(i).gameObject);
            for (int i = 0; i < count; i++)
                Rect(_rows, "Row " + i, 0f, 0f, 590f, RowHeight).gameObject.AddComponent<LayoutElement>().preferredHeight = RowHeight;
            _grow.Refresh();
        }

        [Test]
        public void AShortList_LeavesThePanelAsBuilt()
        {
            SetRows(2);
            Assert.AreEqual(150f, _box.sizeDelta.y, .01f);
            Assert.AreEqual(450f, _modal.sizeDelta.y, .01f);
            Assert.AreEqual(-284f, _notice.anchoredPosition.y, .01f);
        }

        [Test]
        public void ALongList_GrowsTheBox_AndPushesTheNoticeBelowIt()
        {
            SetRows(7);
            float extra = 7 * RowHeight - 119f;
            Assert.AreEqual(150f + extra, _box.sizeDelta.y, .01f, "the box holds every row");
            Assert.AreEqual(450f + extra, _modal.sizeDelta.y, .01f, "the panel grows with it");
            Assert.AreEqual(-284f - extra, _notice.anchoredPosition.y, .01f, "the notice moves down, never onto the rows");
            float boxBottom = _box.anchoredPosition.y - _box.sizeDelta.y;
            Assert.LessOrEqual(_notice.anchoredPosition.y, boxBottom + .01f);
            Assert.AreEqual(-50f + extra / 2f, _modal.anchoredPosition.y, .01f, "re-centred, not sliding to the floor");
        }

        /// <summary>
        /// With twenty items the box went down past the view (PO, 2026-10-26: «un scrollview para que
        /// no quede tan largo sino que mantenga las proporciones»). The rows sit in a scrolled viewport:
        /// the box grows with them only up to a cap, and past it the rows scroll inside.
        /// </summary>
        private RectTransform Scrolled(float maxExtra)
        {
            RectTransform viewport = Rect(_box, "Viewport", 19f, -17f, 590f, 119f);
            _rows.SetParent(viewport, false);
            _rows.anchorMin = new Vector2(0f, 1f);
            _rows.anchorMax = Vector2.one;
            _rows.pivot = new Vector2(.5f, 1f);
            _rows.anchoredPosition = Vector2.zero;
            _rows.sizeDelta = Vector2.zero;
            _rows.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _grow.Configure(viewport, _box, new[] { _modal }, new[] { _notice }, _modal, _rows, maxExtra);
            return viewport;
        }

        [Test]
        public void AScrolledList_GrowsTheBoxUpToTheCap_AndScrollsTheRest()
        {
            RectTransform viewport = Scrolled(120f);
            SetRows(20);
            Assert.AreEqual(150f + 120f, _box.sizeDelta.y, .01f, "the box stops at the cap");
            Assert.AreEqual(119f + 120f, viewport.sizeDelta.y, .01f, "so does the visible window");
            Assert.AreEqual(450f + 120f, _modal.sizeDelta.y, .01f);
            Assert.AreEqual(-284f - 120f, _notice.anchoredPosition.y, .01f, "the notice stays right under the box");
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rows);
            Assert.AreEqual(20 * RowHeight, _rows.rect.height, .5f, "the rows keep their full height to scroll through");
        }

        [Test]
        public void AScrolledShortList_LeavesThePanelAsBuilt()
        {
            RectTransform viewport = Scrolled(120f);
            SetRows(2);
            Assert.AreEqual(150f, _box.sizeDelta.y, .01f);
            Assert.AreEqual(119f, viewport.sizeDelta.y, .01f);
            SetRows(4);
            Assert.AreEqual(150f + 4 * RowHeight - 119f, _box.sizeDelta.y, .01f, "under the cap it grows as before");
        }

        [Test]
        public void ShrinkingTheList_GoesBackToTheBuiltLayout()
        {
            SetRows(7);
            SetRows(1);
            Assert.AreEqual(150f, _box.sizeDelta.y, .01f);
            Assert.AreEqual(-284f, _notice.anchoredPosition.y, .01f);
            Assert.AreEqual(-50f, _modal.anchoredPosition.y, .01f);
        }
    }
}
