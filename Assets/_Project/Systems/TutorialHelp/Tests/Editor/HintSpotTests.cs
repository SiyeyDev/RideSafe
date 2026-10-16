using System.Collections.Generic;
using NUnit.Framework;
using RideSafe.Tutorial;
using RideSafe.Tutorial.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.Help.Tests
{
    public class HintSpotTests
    {
        private static readonly Rect Centre = new Rect(.4f, .1f, .2f, .3f);
        private static readonly Rect Right = new Rect(.65f, .1f, .2f, .3f);
        private static readonly Rect Left = new Rect(.15f, .1f, .2f, .3f);

        [Test]
        public void Nothing_in_the_way_keeps_the_centre()
        {
            Assert.AreEqual(0, HintSpot.Choose(new[] { Centre, Right, Left }, new List<Rect>()));
        }

        [Test]
        public void A_panel_on_the_centre_moves_it_to_a_free_side()
        {
            var panels = new List<Rect> { new Rect(.35f, 0f, .3f, .6f) };
            Assert.AreEqual(1, HintSpot.Choose(new[] { Centre, Right, Left }, panels));
        }

        [Test]
        public void With_no_free_spot_it_takes_the_least_covered()
        {
            var panels = new List<Rect> { new Rect(.3f, 0f, .7f, .6f), new Rect(0f, 0f, .2f, .6f) };
            Assert.AreEqual(2, HintSpot.Choose(new[] { Centre, Right, Left }, panels), "left is the least covered");
        }
    }

    public class GestureHintViewSpotTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private GestureHintView _view;
        private TutorialGestureSO _trigger;
        private Camera _camera;

        [SetUp]
        public void SetUp()
        {
            GameObject root = Track(new GameObject("Gesture Hint"));
            _view = root.AddComponent<GestureHintView>();
            ControllerModelVisual right = Model(root.transform, XRNodeRole.RightHand);
            ControllerModelVisual left = Model(root.transform, XRNodeRole.LeftHand);
            _view.Configure(left, right, null);
            _trigger = ScriptableObject.CreateInstance<TutorialGestureSO>();
            _objects.Add(_trigger);
            _trigger.Configure(ControllerControl.Trigger, TutorialGestureSO.HandChoice.Dominant, "", "");
            GameObject cam = Track(new GameObject("Camera", typeof(Camera)));
            cam.transform.position = new Vector3(0f, 1.6f, 0f);
            _camera = cam.GetComponent<Camera>();
            _camera.fieldOfView = 60f;
            _camera.aspect = 16f / 9f;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _objects)
                Object.DestroyImmediate(o);
            _objects.Clear();
        }

        private GameObject Track(GameObject go)
        {
            _objects.Add(go);
            return go;
        }

        private static ControllerModelVisual Model(Transform parent, XRNodeRole hand)
        {
            GameObject go = new GameObject(hand.ToString());
            go.transform.SetParent(parent, false);
            ControllerModelVisual model = go.AddComponent<ControllerModelVisual>();
            SerializedObject serialized = new SerializedObject(model);
            serialized.FindProperty("_hand").enumValueIndex = (int)hand;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            new GameObject("Trigger").transform.SetParent(go.transform, false);
            go.transform.Find("Trigger").gameObject.AddComponent<ControllerButtonVisual>();
            return model;
        }

        private void Panel(Vector3 centre, Vector2 metres)
        {
            GameObject canvas = Track(new GameObject("Module panel", typeof(RectTransform), typeof(Canvas)));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            RectTransform rect = (RectTransform)canvas.transform;
            rect.position = centre;
            rect.sizeDelta = metres / .002f;
            rect.localScale = Vector3.one * .002f;
            GameObject image = new GameObject("Background", typeof(RectTransform), typeof(Image));
            image.transform.SetParent(canvas.transform, false);
            RectTransform imageRect = (RectTransform)image.transform;
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.sizeDelta = Vector2.zero;
        }

        private float Lateral()
        {
            Vector3 local = _camera.transform.InverseTransformPoint(_view.transform.position);
            return local.x;
        }

        [Test]
        public void With_the_centre_free_it_stays_centred()
        {
            _view.Show(_trigger, XRNodeRole.RightHand, null, null);
            _view.Respot(_camera);
            Assert.AreEqual(0f, Lateral(), .01f);
        }

        [Test]
        public void A_module_panel_below_the_gaze_pushes_it_to_the_side()
        {
            Panel(new Vector3(0f, 1.3f, 1.2f), new Vector2(.7f, .5f)); // like «Mi Elección» low and centred
            _view.Show(_trigger, XRNodeRole.RightHand, null, null);
            _view.Respot(_camera);
            Assert.Greater(Mathf.Abs(Lateral()), .2f, "beside the panel, not on it");
        }
    }
}
