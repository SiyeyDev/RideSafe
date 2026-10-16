using NUnit.Framework;
using RideSafe.Tutorial;
using UnityEngine;

namespace RideSafe.Help.Tests
{
    public class TutorialHelpFacadeTests
    {
        private TutorialGestureSO _gesture;
        private HelpDirector _director;

        [SetUp]
        public void SetUp()
        {
            _gesture = ScriptableObject.CreateInstance<TutorialGestureSO>();
            _gesture.Configure(ControllerControl.Trigger, TutorialGestureSO.HandChoice.Dominant, "", "");
            _director = new HelpDirector(_gesture, new FakeHintView(), new FakeHighlighter(), key => key);
        }

        [TearDown]
        public void TearDown()
        {
            TutorialHelp.Unregister(_director);
            Object.DestroyImmediate(_gesture);
        }

        [Test]
        public void WithoutTutorial_CallsAreHarmless()
        {
            Assert.IsFalse(TutorialHelp.IsAvailable);
            Assert.DoesNotThrow(() =>
            {
                TutorialHelp.Stop();
                TutorialHelp.Resume();
                using (TutorialHelp.Pause()) { }
                TutorialHelp.Show(null).Dispose();
            });
            Assert.IsFalse(TutorialHelp.IsObserving);
        }

        [Test]
        public void StopAndResume_ReachTheTutorial()
        {
            TutorialHelp.Register(_director);
            TutorialHelp.Stop();
            Assert.IsFalse(_director.Observing);
            TutorialHelp.Resume();
            Assert.IsTrue(_director.Observing);
        }

        [Test]
        public void Pause_StopsForTheBlock_ThenResumes()
        {
            TutorialHelp.Register(_director);
            using (TutorialHelp.Pause())
                Assert.IsFalse(TutorialHelp.IsObserving);
            Assert.IsTrue(TutorialHelp.IsObserving);
        }

        [Test]
        public void Unregister_LeavesTheCallsHarmlessAgain()
        {
            TutorialHelp.Register(_director);
            TutorialHelp.Unregister(_director);
            Assert.IsFalse(TutorialHelp.IsAvailable);
        }
    }
}
