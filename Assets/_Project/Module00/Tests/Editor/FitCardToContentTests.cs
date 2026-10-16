using System.Collections.Generic;
using NUnit.Framework;
using RideSafe.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.Module00.Tests
{
    public class FitCardToContentTests
    {
        private const float Pad = 16f / 2.8f;
        private const float Centre = 763f;
        private readonly List<GameObject> _objects = new List<GameObject>();
        private RectTransform _card, _title, _description, _check, _caption;
        private Toggle _toggle;
        private FitCardToContent _fit;

        [SetUp]
        public void SetUp()
        {
            GameObject canvas = Track(new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)));
            _card = Child(canvas.transform, "Option_escooter");
            _card.sizeDelta = new Vector2(340f + 2f * Pad, 77f);
            _card.anchoredPosition = new Vector2(Centre - 170f - Pad, -180f);
            _toggle = _card.gameObject.AddComponent<Toggle>();
            _title = Text(_card, "Title", "Patineta eléctrica de pie", 22f);
            _description = Text(_card, "Description", "De pie · acelerador", 13f);
            _caption = Text(_card, "StateCaption", "SELECCIONADO", 12f);
            _check = Child(_card, "Check");
            _check.sizeDelta = new Vector2(32f, 32f);
            _fit = _card.gameObject.AddComponent<FitCardToContent>();
            _fit.Configure(_toggle, _title.GetComponent<TMP_Text>(), _description.GetComponent<TMP_Text>(), _check, _caption);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects)
                Object.DestroyImmediate(go);
            _objects.Clear();
        }

        private GameObject Track(GameObject go)
        {
            _objects.Add(go);
            return go;
        }

        private static RectTransform Child(Transform parent, string name)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            return rect;
        }

        private static RectTransform Text(Transform parent, string name, string text, float size)
        {
            RectTransform rect = Child(parent, name);
            TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            rect.sizeDelta = new Vector2(324f, size + 10f);
            return rect;
        }

        private float VisibleWidth => _card.sizeDelta.x - 2f * Pad;
        private float CardCentre => _card.anchoredPosition.x + _card.sizeDelta.x / 2f;

        [Test]
        public void Unselected_HugsItsText_AndStaysOverItsVehicle()
        {
            _fit.Refresh();
            Assert.AreEqual(_fit.ContentWidth + 2f * FitCardToContent.SideMargin, VisibleWidth, 0.01f);
            Assert.AreEqual(Centre, CardCentre, 0.01f);
            Assert.AreEqual(_fit.ContentWidth, _title.sizeDelta.x, 0.01f, "the name gets exactly its width");
            Assert.IsFalse(_check.gameObject.activeSelf && _toggle.isOn);
        }

        [Test]
        public void Selected_GrowsJustEnoughForTheCheck_BesideTheText()
        {
            _fit.Refresh();
            float unselected = VisibleWidth;
            _toggle.isOn = true;
            _fit.Refresh();
            Assert.AreEqual(unselected + FitCardToContent.CheckGap + _check.sizeDelta.x, VisibleWidth, 0.01f);
            Assert.AreEqual(Centre, CardCentre, 0.01f);
            Assert.GreaterOrEqual(_check.anchoredPosition.x, _title.anchoredPosition.x + _title.sizeDelta.x, "beside the text, not on it");
            Assert.LessOrEqual(_check.anchoredPosition.x + _check.sizeDelta.x, _card.sizeDelta.x - Pad, "inside the card");
        }

        [Test]
        public void AShorterName_GivesANarrowerCard()
        {
            _fit.Refresh();
            float spanish = VisibleWidth;
            _title.GetComponent<TMP_Text>().text = "E-bike";
            _description.GetComponent<TMP_Text>().text = "Pedal assist";
            _fit.Refresh();
            Assert.Less(VisibleWidth, spanish);
            Assert.AreEqual(Centre, CardCentre, 0.01f);
        }
    }
}
