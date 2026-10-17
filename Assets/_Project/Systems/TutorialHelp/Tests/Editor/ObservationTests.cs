using System.Collections.Generic;
using NUnit.Framework;
using RideSafe.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RideSafe.Help.Tests
{
    public class ObservationTests
    {
        private static readonly Pose Viewer = new Pose(Vector3.zero, Quaternion.identity); // looking along +Z
        private readonly List<GameObject> _objects = new List<GameObject>();

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

        private Button WorldButton(Vector3 position, RenderMode mode = RenderMode.WorldSpace)
        {
            GameObject canvasGo = Track(new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)));
            canvasGo.GetComponent<Canvas>().renderMode = mode;
            canvasGo.transform.position = position;
            GameObject buttonGo = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(canvasGo.transform, false);
            Button button = buttonGo.GetComponent<Button>();
            button.targetGraphic = buttonGo.GetComponent<Image>();
            return button;
        }

        private List<HelpTarget> Scan(Transform ignore = null)
        {
            List<HelpTarget> results = new List<HelpTarget>();
            new TargetScanner().Scan(Viewer, 2f, ignore, results);
            return results;
        }

        [Test]
        public void Scanner_FindsAnInteractableButtonInView()
        {
            Button button = WorldButton(new Vector3(0f, 0f, 2f));
            List<HelpTarget> found = Scan();
            HelpTarget target = found.Find(t => t.Transform == button.transform);
            Assert.AreEqual(InteractionKind.Interface, target.Kind);
            Assert.AreSame(button.targetGraphic, target.Graphic);
        }

        [Test]
        public void Scanner_SkipsADisabledButton()
        {
            Button button = WorldButton(new Vector3(0f, 0f, 2f));
            button.interactable = false;
            Assert.IsFalse(Scan().Exists(t => t.Transform == button.transform), "Continue before a choice is not a target");
        }

        [Test]
        public void Scanner_SkipsAButtonBehindTheLearner()
        {
            Button button = WorldButton(new Vector3(0f, 0f, -2f));
            Assert.IsFalse(Scan().Exists(t => t.Transform == button.transform));
        }

        [Test]
        public void Scanner_SkipsAHiddenCanvasGroup()
        {
            Button button = WorldButton(new Vector3(0f, 0f, 2f));
            button.transform.parent.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            Assert.IsFalse(Scan().Exists(t => t.Transform == button.transform));
        }

        [Test]
        public void Scanner_SkipsScreenSpaceCanvases()
        {
            Button button = WorldButton(new Vector3(0f, 0f, 2f), RenderMode.ScreenSpaceOverlay);
            Assert.IsFalse(Scan().Exists(t => t.Transform == button.transform));
        }

        [Test]
        public void Scanner_FindsAClickableObject_AsObject3D()
        {
            GameObject cube = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            cube.transform.position = new Vector3(0.3f, 0f, 3f);
            cube.AddComponent<EventTrigger>(); // a runtime click receiver that is not a Selectable, like a module's 3D object
            Physics.SyncTransforms();
            HelpTarget target = Scan().Find(t => t.Transform == cube.transform);
            Assert.AreEqual(cube.transform, target.Transform);
            Assert.AreEqual(InteractionKind.Object3D, target.Kind);
        }

        [Test]
        public void Scanner_SkipsAnObjectThePointerCannotReach()
        {
            GameObject item = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            item.transform.position = new Vector3(0f, 0f, 4f);
            item.AddComponent<EventTrigger>();
            GameObject door = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            door.transform.position = new Vector3(0f, 0f, 2f);
            door.transform.localScale = new Vector3(4f, 4f, 0.1f);
            Physics.SyncTransforms();
            Assert.IsFalse(Scan().Exists(t => t.Transform == item.transform), "behind a closed garage door");
        }

        [Test]
        public void Scanner_SkipsAnObjectBeyondArmsReach()
        {
            GameObject item = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            item.transform.position = new Vector3(0f, 0f, 6f);
            item.AddComponent<EventTrigger>();
            Physics.SyncTransforms();
            Assert.IsFalse(Scan().Exists(t => t.Transform == item.transform),
                           "the EPP behind a collider-less garage door, 7.6 m away, is not this screen's target");
        }

        [Test]
        public void Scanner_SkipsWhatIsUnderTheTutorial()
        {
            GameObject tutorial = Track(new GameObject("TutorialHelp"));
            Button button = WorldButton(new Vector3(0f, 0f, 2f));
            button.transform.parent.SetParent(tutorial.transform, true);
            Assert.IsFalse(Scan(tutorial.transform).Exists(t => t.Transform == button.transform));
        }

        [Test]
        public void InView_RespectsAngleAndDistance()
        {
            Assert.IsTrue(TargetScanner.InView(Viewer, new Vector3(0f, 0f, 3f)));
            Assert.IsFalse(TargetScanner.InView(Viewer, new Vector3(0f, 0f, 9f)), "beyond the AutoHand ray");
            Assert.IsFalse(TargetScanner.InView(Viewer, new Vector3(3f, 0f, 1f)), "far off to the side");
            Assert.IsTrue(TargetScanner.InView(Viewer, Quaternion.Euler(0f, 40f, 0f) * Vector3.forward * 2.5f),
                          "the glasses at the side of the station, 39 degrees off, are plainly in view");
        }

        [Test]
        public void Witness_HearsTheSameClickAsTheButton()
        {
            Button button = WorldButton(new Vector3(0f, 0f, 2f));
            int clicks = 0;
            button.onClick.AddListener(() => clicks++);
            ClickWitness witness = ClickWitness.Attach(button.gameObject, InteractionKind.Interface);
            ClickWitness heard = null;
            witness.Clicked += w => heard = w;

            ExecuteEvents.Execute(button.gameObject, new PointerEventData(null) { button = PointerEventData.InputButton.Left },
                                  ExecuteEvents.pointerClickHandler);

            Assert.AreEqual(1, clicks, "the module's own click still happens");
            Assert.AreSame(witness, heard);
            Assert.AreEqual(InteractionKind.Interface, heard.Kind);
            Assert.IsTrue((witness.hideFlags & HideFlags.DontSave) != 0, "never saved into the scene");
        }

        [Test]
        public void Witness_AttachIsIdempotent()
        {
            GameObject cube = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            ClickWitness first = ClickWitness.Attach(cube, InteractionKind.Object3D);
            Assert.AreSame(first, ClickWitness.Attach(cube, InteractionKind.Object3D));
            Assert.AreEqual(1, cube.GetComponents<ClickWitness>().Length);
        }

        [Test]
        public void Narration_IsALongNonLoopingSound()
        {
            Assert.IsTrue(QuietDetector.IsNarration(true, false, 3f));
            Assert.IsFalse(QuietDetector.IsNarration(true, true, 30f), "ambient loop");
            Assert.IsFalse(QuietDetector.IsNarration(true, false, 0.5f), "click sound");
            Assert.IsFalse(QuietDetector.IsNarration(false, false, 3f), "not playing");
        }

        [Test]
        public void Video_IsOnWhilePlayingOrPausedMidClip()
        {
            Assert.IsTrue(QuietDetector.IsVideoOn(true, false, 40, 300));
            Assert.IsTrue(QuietDetector.IsVideoOn(false, true, 40, 300), "paused by the learner, or by lost focus");
            Assert.IsTrue(QuietDetector.IsVideoOn(false, true, 0, 0), "paused before the length is known");
            Assert.IsFalse(QuietDetector.IsVideoOn(false, true, 299, 300), "paused on the last frame: it ended");
            Assert.IsFalse(QuietDetector.IsVideoOn(false, false, 120, 300), "stopped");
        }

        [Test]
        public void HandTracker_IsRightUntilComfortSaysOtherwise()
        {
            GameObject panelGo = Track(new GameObject("Comfort"));
            ComfortSettingsPanel panel = panelGo.AddComponent<ComfortSettingsPanel>();
            HandTracker tracker = new HandTracker();
            tracker.Listen(new[] { panel });
            Assert.AreEqual(DominantHand.Right, tracker.Hand);
            panel.onStart.Invoke(new ComfortSettings { Hand = DominantHand.Left });
            Assert.AreEqual(DominantHand.Left, tracker.Hand);
            tracker.Dispose();
            panel.onStart.Invoke(new ComfortSettings { Hand = DominantHand.Right });
            Assert.AreEqual(DominantHand.Left, tracker.Hand, "stops listening once disposed");
        }
    }
}
