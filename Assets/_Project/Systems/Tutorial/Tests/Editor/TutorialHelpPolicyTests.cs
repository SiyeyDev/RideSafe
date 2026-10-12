using NUnit.Framework;

namespace RideSafe.Tutorial.Tests
{
    public class TutorialHelpPolicyTests
    {
        private static readonly TutorialHelpPolicy Progressive = new TutorialHelpPolicy(
            reinforceBelow: 2, lightBelow: 4,
            locate: new HelpTiming(0f, -1f),
            teach: new HelpTiming(0f, 0f),
            reinforce: new HelpTiming(0f, 0f),
            light: new HelpTiming(0f, 6f),
            known: new HelpTiming(3f, 10f));

        [TestCase(false, 0, HelpStage.Locate)]
        [TestCase(false, 9, HelpStage.Locate)]
        [TestCase(true, 0, HelpStage.Teach)]
        [TestCase(true, 1, HelpStage.Reinforce)]
        [TestCase(true, 2, HelpStage.Light)]
        [TestCase(true, 3, HelpStage.Light)]
        [TestCase(true, 4, HelpStage.Known)]
        [TestCase(true, 20, HelpStage.Known)]
        public void Stage_FadesWithPractice(bool hasAction, int practice, HelpStage expected)
        {
            Assert.AreEqual(expected, Progressive.StageFor(hasAction, practice));
        }

        [Test]
        public void Teach_ShowsEverythingImmediately()
        {
            HelpTiming timing = Progressive.TimingFor(HelpStage.Teach);
            Assert.IsTrue(timing.ShowHighlight(0f));
            Assert.IsTrue(timing.ShowControllerHint(0f));
        }

        [Test]
        public void Light_ShowsWhereAtOnce_ControllerOnlyAfterHesitation()
        {
            HelpTiming timing = Progressive.TimingFor(HelpStage.Light);
            Assert.IsTrue(timing.ShowHighlight(0f));
            Assert.IsFalse(timing.ShowControllerHint(5.9f));
            Assert.IsTrue(timing.ShowControllerHint(6f));
        }

        [Test]
        public void Locate_NeverShowsAController()
        {
            HelpTiming timing = Progressive.TimingFor(HelpStage.Locate);
            Assert.IsTrue(timing.ShowHighlight(0f));
            Assert.IsFalse(timing.ShowControllerHint(1000f));
        }

        [Test]
        public void Defaults_KeepTheOriginalFirstUseThenReminderLadder()
        {
            TutorialHelpPolicy defaults = new TutorialHelpPolicy();
            HelpTiming teach = defaults.TimingFor(defaults.StageFor(true, 0));
            HelpTiming reminder = defaults.TimingFor(defaults.StageFor(true, 3));

            Assert.IsTrue(teach.ShowControllerHint(0f));
            Assert.IsFalse(reminder.ShowHighlight(2.7f));
            Assert.IsTrue(reminder.ShowHighlight(2.75f));
            Assert.IsFalse(reminder.ShowControllerHint(4.9f));
            Assert.IsTrue(reminder.ShowControllerHint(5f));
        }
    }

    public class TutorialProgressPracticeTests
    {
        private static readonly ActionId Select = new ActionId(ActionIds.PrimarySelect);

        [Test]
        public void RecordPractice_CountsAndMarksLearned()
        {
            TutorialProgress progress = new TutorialProgress();
            Assert.AreEqual(0, progress.PracticeCount(Select));

            progress.RecordPractice(Select);
            progress.RecordPractice(Select);

            Assert.AreEqual(2, progress.PracticeCount(Select));
            Assert.IsTrue(progress.IsActionLearned(Select));
        }

        [Test]
        public void MarkActionLearned_CountsAsOnePractice()
        {
            TutorialProgress progress = new TutorialProgress();
            progress.MarkActionLearned(Select);
            Assert.AreEqual(1, progress.PracticeCount(Select));
        }

        [Test]
        public void Clear_ForgetsPractice()
        {
            TutorialProgress progress = new TutorialProgress();
            progress.RecordPractice(Select);
            progress.Clear();
            Assert.AreEqual(0, progress.PracticeCount(Select));
            Assert.IsFalse(progress.IsActionLearned(Select));
        }
    }
}
