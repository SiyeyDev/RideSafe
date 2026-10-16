using NUnit.Framework;
using RideSafe.Tutorial;
using UnityEngine;

namespace RideSafe.Help.Tests
{
    public class CueAndPauseTests
    {
        private TutorialGestureSO _gesture;
        private FakeHintView _view;
        private HelpDirector _director;
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _gesture = ScriptableObject.CreateInstance<TutorialGestureSO>();
            _gesture.Configure(ControllerControl.Trigger, TutorialGestureSO.HandChoice.Dominant, "", "");
            _view = new FakeHintView();
            _director = new HelpDirector(_gesture, _view, new FakeHighlighter(), key => key);
            TutorialHelp.Register(_director);
            _go = new GameObject("Section");
        }

        [TearDown]
        public void TearDown()
        {
            TutorialHelp.Unregister(_director);
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_gesture);
        }

        [Test]
        public void Cue_ShowsItsGesture_WhileActive()
        {
            TutorialCue cue = _go.AddComponent<TutorialCue>();
            cue.Configure(_gesture, _go.transform);
            cue.Begin();
            Assert.IsTrue(_view.Visible);
            cue.End();
            Assert.IsFalse(_view.Visible);
        }

        [Test]
        public void Pause_StopsObserving_WhileActive()
        {
            TutorialPause pause = _go.AddComponent<TutorialPause>();
            pause.Begin();
            Assert.IsFalse(TutorialHelp.IsObserving);
            pause.End();
            Assert.IsTrue(TutorialHelp.IsObserving);
        }

        [Test]
        public void Pause_WatchingAModuleScreen_PausesWhileThatScreenIsOpen()
        {
            GameObject screen = new GameObject("Orientation");
            try
            {
                TutorialPause pause = _go.AddComponent<TutorialPause>();
                pause.Watch(screen);
                pause.Tick();
                Assert.IsFalse(TutorialHelp.IsObserving, "the screen is open");
                screen.SetActive(false);
                pause.Tick();
                Assert.IsTrue(TutorialHelp.IsObserving, "the screen closed");
            }
            finally
            {
                Object.DestroyImmediate(screen);
            }
        }
    }
}
