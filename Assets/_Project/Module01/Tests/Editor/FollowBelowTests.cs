using System.Collections.Generic;
using NUnit.Framework;
using RideSafe.UI;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Los botones finales vivían clavados a y = −358 del HUD; con 11 filas la lista de «Mi
    /// Elección» les pasaba por debajo. Tienen que quedar justo debajo de la lista, con aire.
    /// </summary>
    public class FollowBelowTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private RectTransform _list, _buttons;
        private FollowBelow _follow;

        [SetUp]
        public void SetUp()
        {
            Transform panelCanvas = Canvas("Panel", new Vector3(0f, 1.5f, 2f));
            Transform hudCanvas = Canvas("HUD", new Vector3(0.3f, 1.2f, 2f));
            _list = Rect(panelCanvas, "LeftColumn", new Vector2(0f, 50f), new Vector2(620f, 400f), new Vector2(.5f, 1f));
            _buttons = Rect(hudCanvas, "FinalResult", new Vector2(0f, -358f), new Vector2(660f, 72f), new Vector2(.5f, .5f));
            _follow = _buttons.gameObject.AddComponent<FollowBelow>();
            _follow.Configure(_list, 12f);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects)
                Object.DestroyImmediate(go);
            _objects.Clear();
        }

        private Transform Canvas(string name, Vector3 position)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            _objects.Add(go);
            go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            go.transform.position = position;
            go.transform.localScale = Vector3.one * .002f;
            return go.transform;
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size, Vector2 pivot)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Vector3 Corner(RectTransform r, int i)
        {
            Vector3[] corners = new Vector3[4];
            r.GetWorldCorners(corners);
            return corners[i];
        }

        [Test]
        public void Los_botones_quedan_justo_debajo_de_la_lista_y_centrados()
        {
            _follow.Tick();
            float listBottom = Corner(_list, 0).y;
            float buttonsTop = Corner(_buttons, 1).y;
            Assert.AreEqual(listBottom - 12f * .002f, buttonsTop, 1e-4f, "12 px de aire bajo la lista");
            float listCentre = (Corner(_list, 0).x + Corner(_list, 3).x) / 2f;
            float buttonsCentre = (Corner(_buttons, 0).x + Corner(_buttons, 3).x) / 2f;
            Assert.AreEqual(listCentre, buttonsCentre, 1e-4f);
        }

        [Test]
        public void Si_la_lista_crece_los_botones_bajan_con_ella()
        {
            _follow.Tick();
            float before = Corner(_buttons, 1).y;
            _list.sizeDelta = new Vector2(620f, 500f);
            _follow.Tick();
            Assert.AreEqual(before - 100f * .002f, Corner(_buttons, 1).y, 1e-4f);
        }

        [Test]
        public void Sin_lista_visible_no_se_mueven()
        {
            _list.gameObject.SetActive(false);
            Vector3 before = _buttons.position;
            _follow.Tick();
            Assert.AreEqual(before, _buttons.position);
        }
    }
}
