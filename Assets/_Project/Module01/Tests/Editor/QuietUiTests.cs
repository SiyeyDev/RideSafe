using System.Collections.Generic;
using NUnit.Framework;
using RideSafe.UI;
using UnityEditor;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Tocar un RectTransform dentro de un canvas lo marca sucio y lo rehace entero. En Quest,
    /// hacerlo en cada fotograma sin que nada cambie cuesta CPU de render para nada.
    /// </summary>
    public class QuietUiTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects)
                Object.DestroyImmediate(go);
            _objects.Clear();
        }

        private RectTransform Rect(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            if (parent == null)
                _objects.Add(go);
            else
                go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        [Test]
        public void La_barra_de_progreso_no_se_rehace_por_cambios_imperceptibles()
        {
            RectTransform bar = Rect(null, "Progress");
            RectTransform fill = Rect(bar, "Fill");
            ProgressView view = bar.gameObject.AddComponent<ProgressView>();
            SerializedObject serialized = new SerializedObject(view);
            serialized.FindProperty("_mode").enumValueIndex = 0; // Bar
            serialized.FindProperty("_fill").objectReferenceValue = fill;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            view.SetValue(.5f);
            Assert.AreEqual(.5f, fill.anchorMax.x, 1e-5f);
            fill.hasChanged = false;
            view.SetValue(.501f);
            Assert.IsFalse(fill.hasChanged, "una milésima no se ve: no se toca el canvas");
            view.SetValue(.6f);
            Assert.AreEqual(.6f, fill.anchorMax.x, 1e-5f);
            view.SetValue(1f);
            Assert.AreEqual(1f, fill.anchorMax.x, 1e-5f, "el final siempre llega exacto");
        }

        [Test]
        public void FollowBelow_no_mueve_nada_si_la_lista_no_se_movio()
        {
            GameObject canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            _objects.Add(canvas);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            RectTransform list = Rect(canvas.transform, "LeftColumn");
            list.sizeDelta = new Vector2(600f, 300f);
            RectTransform buttons = Rect(canvas.transform, "FinalResult");
            buttons.sizeDelta = new Vector2(600f, 70f);
            FollowBelow follow = buttons.gameObject.AddComponent<FollowBelow>();
            follow.Configure(list, 12f);

            follow.Tick();
            buttons.hasChanged = false;
            follow.Tick();
            Assert.IsFalse(buttons.hasChanged, "sin cambios no se reescribe la posición");
        }
    }
}
