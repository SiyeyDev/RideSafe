using System.Text.RegularExpressions;
using NUnit.Framework;
using RideSafe.Tutorial;
using RideSafe.Tutorial.Presentation;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace RideSafe.Help.Tests
{
    public class GestureHintViewTests
    {
        private GameObject _root;
        private GestureHintView _view;
        private ControllerModelVisual _left, _right;
        private TMP_Text _caption;
        private TutorialGestureSO _trigger;

        private static ControllerModelVisual Model(Transform parent, XRNodeRole hand)
        {
            GameObject go = new GameObject(hand.ToString());
            go.transform.SetParent(parent, false);
            ControllerModelVisual model = go.AddComponent<ControllerModelVisual>();
            SerializedObject serialized = new SerializedObject(model);
            serialized.FindProperty("_hand").enumValueIndex = (int)hand;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            GameObject trigger = new GameObject("Trigger");
            trigger.transform.SetParent(go.transform, false);
            trigger.AddComponent<ControllerButtonVisual>(); // Trigger by default
            return model;
        }

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Gesture Hint");
            _view = _root.AddComponent<GestureHintView>();
            _left = Model(_root.transform, XRNodeRole.LeftHand);
            _right = Model(_root.transform, XRNodeRole.RightHand);
            GameObject caption = new GameObject("Caption", typeof(TextMeshPro));
            caption.transform.SetParent(_root.transform, false);
            _caption = caption.GetComponent<TMP_Text>();
            _view.Configure(_left, _right, _caption);
            _trigger = ScriptableObject.CreateInstance<TutorialGestureSO>();
            _trigger.Configure(ControllerControl.Trigger, TutorialGestureSO.HandChoice.Dominant, "", "");
            _view.Hide();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_trigger);
        }

        [Test]
        public void Show_UsesTheRequestedHand_AndHidesTheOther()
        {
            _view.Show(_trigger, XRNodeRole.LeftHand, null, null);
            Assert.IsTrue(_view.IsVisible);
            Assert.IsTrue(_left.gameObject.activeSelf);
            Assert.IsFalse(_right.gameObject.activeSelf);
            Assert.AreEqual(XRNodeRole.LeftHand, _view.ShownHand);
        }

        [Test]
        public void Caption_OnlyWhenThereIsText()
        {
            _view.Show(_trigger, XRNodeRole.RightHand, "Apunta y presiona el gatillo", null);
            Assert.IsTrue(_caption.gameObject.activeSelf);
            Assert.AreEqual("Apunta y presiona el gatillo", _view.CaptionText);
            _view.Show(_trigger, XRNodeRole.RightHand, null, null);
            Assert.IsFalse(_caption.gameObject.activeSelf);
        }

        [Test]
        public void Hide_DeactivatesTheView()
        {
            _view.Show(_trigger, XRNodeRole.RightHand, null, null);
            _view.Hide();
            Assert.IsFalse(_view.IsVisible);
            Assert.IsFalse(_root.activeSelf);
        }

        [Test]
        public void Show_WithAMissingButton_HidesAndWarns()
        {
            TutorialGestureSO grip = ScriptableObject.CreateInstance<TutorialGestureSO>();
            grip.Configure(ControllerControl.Grip, TutorialGestureSO.HandChoice.Dominant, "", "");
            LogAssert.Expect(LogType.Warning, new Regex("Grip"));
            _view.Show(grip, XRNodeRole.RightHand, null, null);
            Assert.IsFalse(_view.IsVisible);
            Object.DestroyImmediate(grip);
        }

        [Test]
        public void Place_PutsTheControllerBelowAndBesideTheGaze_FacingTheLearner()
        {
            _view.Show(_trigger, XRNodeRole.RightHand, null, null);
            _view.Place(new Pose(new Vector3(0f, 1.6f, 0f), Quaternion.identity));
            Vector3 p = _root.transform.position;
            Assert.Less(p.y, 1.6f, "below eye level");
            Assert.Greater(p.z, 0.3f, "in front");
            Assert.Greater(Vector3.Dot(_root.transform.forward, new Vector3(0f, 1.6f, 0f) - p), 0f, "faces the learner");
        }

        [Test]
        public void PressAt_HighlightsThenPressesThenRests()
        {
            Assert.AreEqual(0f, GestureHintView.PressAt(0.2f, 0.4f, 0.25f, 0.6f));
            Assert.AreEqual(1f, GestureHintView.PressAt(0.65f, 0.4f, 0.25f, 0.6f), 1e-3f);
            Assert.AreEqual(0f, GestureHintView.PressAt(1.2f, 0.4f, 0.25f, 0.6f));
        }

        [Test]
        public void AMissingButton_WarnsOnce_NotEveryFrame()
        {
            TutorialGestureSO grip = ScriptableObject.CreateInstance<TutorialGestureSO>();
            grip.Configure(ControllerControl.Grip, TutorialGestureSO.HandChoice.Dominant, "", "");
            LogAssert.Expect(LogType.Warning, new Regex("Grip"));
            _view.Show(grip, XRNodeRole.RightHand, null, null);
            _view.Show(grip, XRNodeRole.RightHand, null, null);
            _view.Show(grip, XRNodeRole.RightHand, null, null);
            LogAssert.NoUnexpectedReceived();
            Object.DestroyImmediate(grip);
        }
    }
}
