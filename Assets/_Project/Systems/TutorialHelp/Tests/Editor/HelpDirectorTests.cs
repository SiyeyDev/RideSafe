using System.Collections.Generic;
using NUnit.Framework;
using RideSafe.Tutorial;
using RideSafe.UI;
using UnityEngine;

namespace RideSafe.Help.Tests
{
    internal sealed class FakeHintView : IGestureHintView
    {
        public TutorialGestureSO Gesture;
        public XRNodeRole Hand;
        public string Text;
        public bool Visible;
        public void Show(TutorialGestureSO gesture, XRNodeRole hand, string text, Vector3? focus)
        {
            Gesture = gesture;
            Hand = hand;
            Text = text;
            Visible = true;
        }
        public void Hide() => Visible = false;
        public bool IsVisible => Visible;
    }

    internal sealed class FakeHighlighter : ITargetHighlighter
    {
        public readonly List<HelpTarget> Current = new List<HelpTarget>();
        public void Highlight(IReadOnlyList<HelpTarget> targets)
        {
            Current.Clear();
            Current.AddRange(targets);
        }
        public void Clear() => Current.Clear();
    }

    public class HelpDirectorTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private TutorialGestureSO _gesture;
        private FakeHintView _view;
        private FakeHighlighter _highlighter;
        private HelpDirector _director;

        [SetUp]
        public void SetUp()
        {
            _gesture = ScriptableObject.CreateInstance<TutorialGestureSO>();
            _gesture.Configure(ControllerControl.Trigger, TutorialGestureSO.HandChoice.Dominant, "k.ui", "k.obj");
            _view = new FakeHintView();
            _highlighter = new FakeHighlighter();
            _director = new HelpDirector(_gesture, _view, _highlighter, key => "T:" + key);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects)
                Object.DestroyImmediate(go);
            _objects.Clear();
            Object.DestroyImmediate(_gesture);
        }

        private HelpTarget Target(InteractionKind kind)
        {
            GameObject go = new GameObject(kind.ToString());
            _objects.Add(go);
            return new HelpTarget(go.transform, kind, null);
        }

        private void Run(float seconds, params HelpTarget[] visible)
        {
            int steps = Mathf.RoundToInt(seconds / 0.1f);
            for (int i = 0; i < steps; i++)
                _director.Update(0.1f, visible, false);
        }

        [Test]
        public void FirstTime_ShowsControllerHighlightAndText()
        {
            HelpTarget a = Target(InteractionKind.Interface), b = Target(InteractionKind.Interface);
            Run(1.2f, a, b);
            Assert.IsTrue(_view.Visible);
            Assert.AreEqual("T:k.ui", _view.Text);
            Assert.AreEqual(XRNodeRole.RightHand, _view.Hand, "right until Comfort says otherwise");
            Assert.AreEqual(2, _highlighter.Current.Count, "every option, alike");
        }

        [Test]
        public void FirstTime_HighlightsOnlyThatKind()
        {
            HelpTarget ui = Target(InteractionKind.Interface), obj = Target(InteractionKind.Object3D);
            _director.Clicked(InteractionKind.Interface);
            Run(1.2f, ui, obj);
            Assert.AreEqual("T:k.obj", _view.Text);
            Assert.AreEqual(1, _highlighter.Current.Count);
            Assert.AreEqual(obj, _highlighter.Current[0]);
        }

        [Test]
        public void Stuck_HighlightsEveryVisibleTarget_WithoutText()
        {
            HelpTarget ui = Target(InteractionKind.Interface), obj = Target(InteractionKind.Object3D);
            _director.Clicked(InteractionKind.Interface);
            _director.Clicked(InteractionKind.Object3D);
            Run(10.2f, ui, obj);
            Assert.AreEqual(HelpMode.Stuck, _director.Decision.Mode);
            Assert.IsTrue(_view.Visible);
            Assert.IsNull(_view.Text);
            Assert.AreEqual(2, _highlighter.Current.Count);
        }

        [Test]
        public void ADominantLeftHand_ShowsTheLeftController()
        {
            _director.Hand = DominantHand.Left;
            Run(1.2f, Target(InteractionKind.Interface));
            Assert.AreEqual(XRNodeRole.LeftHand, _view.Hand);
        }

        [Test]
        public void AClick_HidesTheFirstTimeHelp()
        {
            HelpTarget ui = Target(InteractionKind.Interface);
            Run(1.2f, ui);
            _director.Clicked(InteractionKind.Interface);
            Run(0.1f, ui);
            Assert.IsFalse(_view.Visible);
            Assert.AreEqual(0, _highlighter.Current.Count);
        }

        [Test]
        public void Stop_HidesTheAutomaticHelp()
        {
            HelpTarget ui = Target(InteractionKind.Interface);
            Run(1.2f, ui);
            _director.Stop();
            Run(0.1f);
            Assert.IsFalse(_view.Visible);
        }

        [Test]
        public void ExplicitShow_WorksWhileStopped_AndHidesOnDispose()
        {
            HelpTarget handlebar = Target(InteractionKind.Object3D);
            _director.Stop();
            System.IDisposable help = _director.Show(_gesture, new[] { handlebar.Transform });
            Assert.IsTrue(_view.Visible);
            Assert.AreEqual(1, _highlighter.Current.Count);
            Assert.IsNull(_view.Text, "an explicit call shows no text unless the module adds its own");
            help.Dispose();
            Assert.IsFalse(_view.Visible);
            help.Dispose();
        }

        [Test]
        public void ExplicitShow_WithoutAGesture_UsesTheDefaultOne()
        {
            _director.Show(null, new Transform[0]);
            Assert.AreSame(_gesture, _view.Gesture);
        }

        [Test]
        public void Stuck_KeepsEveryOptionItShowed_WhenTheLearnerLooksAtOne()
        {
            HelpTarget a = Target(InteractionKind.Interface), b = Target(InteractionKind.Interface);
            _director.Clicked(InteractionKind.Interface);
            Run(10.2f, a, b);
            Run(0.3f, a); // b drifted out of the gaze cone
            Assert.AreEqual(2, _highlighter.Current.Count, "an evaluated choice never shows one option alone");
        }

        [Test]
        public void Stuck_DropsAnOptionThatIsGone()
        {
            HelpTarget a = Target(InteractionKind.Interface), b = Target(InteractionKind.Interface);
            _director.Clicked(InteractionKind.Interface);
            Run(10.2f, a, b);
            b.Transform.gameObject.SetActive(false);
            Run(0.1f, a);
            Assert.AreEqual(1, _highlighter.Current.Count);
        }
    }
}
