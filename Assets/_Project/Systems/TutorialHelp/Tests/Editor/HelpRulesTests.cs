using NUnit.Framework;

namespace RideSafe.Help.Tests
{
    public class HelpRulesTests
    {
        private static HelpDecision Run(HelpRules rules, float seconds, bool ui, bool obj, bool quiet = false)
        {
            const float step = 0.1f;
            HelpDecision decision = HelpDecision.None;
            int steps = UnityEngine.Mathf.RoundToInt(seconds / step);
            for (int i = 0; i < steps; i++)
                decision = rules.Tick(step, ui, obj, quiet);
            return decision;
        }

        [Test]
        public void NoTargets_NoHelp()
        {
            Assert.AreEqual(HelpMode.None, Run(new HelpRules(), 20f, false, false).Mode);
        }

        [Test]
        public void FirstInterfaceTarget_WaitsToSettle_ThenTeaches()
        {
            HelpRules rules = new HelpRules();
            Assert.AreEqual(HelpMode.None, Run(rules, 0.5f, true, false).Mode, "the screen is still arriving");
            HelpDecision decision = Run(rules, 0.6f, true, false);
            Assert.AreEqual(HelpMode.FirstTime, decision.Mode);
            Assert.AreEqual(InteractionKind.Interface, decision.Kind);
        }

        [Test]
        public void AClick_LearnsTheKind_ForTheSession()
        {
            HelpRules rules = new HelpRules();
            Run(rules, 2f, true, false);
            rules.Clicked(InteractionKind.Interface);
            Assert.IsTrue(rules.IsLearned(InteractionKind.Interface));
            Assert.AreEqual(HelpMode.None, Run(rules, 5f, true, false).Mode);
        }

        [Test]
        public void ObjectKind_IsTaughtSeparately()
        {
            HelpRules rules = new HelpRules();
            rules.Clicked(InteractionKind.Interface);
            HelpDecision decision = Run(rules, 1.2f, true, true);
            Assert.AreEqual(HelpMode.FirstTime, decision.Mode);
            Assert.AreEqual(InteractionKind.Object3D, decision.Kind);
        }

        [Test]
        public void TenSecondsWithoutAClick_IsStuck_EvenWhenLearned()
        {
            HelpRules rules = new HelpRules();
            rules.Clicked(InteractionKind.Interface);
            Assert.AreEqual(HelpMode.None, Run(rules, 9.8f, true, false).Mode);
            Assert.AreEqual(HelpMode.Stuck, Run(rules, 0.3f, true, false).Mode);
        }

        [Test]
        public void AnyClick_RestartsTheStuckCount()
        {
            HelpRules rules = new HelpRules();
            rules.Clicked(InteractionKind.Interface);
            Run(rules, 9f, true, false);
            rules.Clicked(InteractionKind.Interface);
            Assert.AreEqual(HelpMode.None, Run(rules, 9f, true, false).Mode);
        }

        [Test]
        public void Quiet_FreezesTheCount_AndShowsNothing()
        {
            HelpRules rules = new HelpRules();
            rules.Clicked(InteractionKind.Interface);
            Run(rules, 6f, true, false);
            Assert.AreEqual(HelpMode.None, Run(rules, 30f, true, false, quiet: true).Mode);
            Assert.AreEqual(HelpMode.Stuck, Run(rules, 4.2f, true, false).Mode, "6 s before + 4.2 s after the narration");
        }

        [Test]
        public void AfterQuiet_FirstTimeHelp_WaitsForTheScreenToSettleAgain()
        {
            HelpRules rules = new HelpRules();
            rules.Clicked(InteractionKind.Interface);
            Run(rules, 1.5f, true, false);
            Run(rules, 1.5f, true, true, quiet: true);
            Assert.AreEqual(HelpMode.None, rules.Tick(0.02f, true, true, false).Mode, "the video just ended");
            Assert.AreEqual(HelpMode.FirstTime, Run(rules, 1.1f, true, true).Mode);
        }

        [Test]
        public void Quiet_DelaysTheFirstTimeHelp()
        {
            HelpRules rules = new HelpRules();
            Assert.AreEqual(HelpMode.None, Run(rules, 5f, true, false, quiet: true).Mode);
            Assert.AreEqual(HelpMode.FirstTime, Run(rules, 1.1f, true, false).Mode);
        }

        [Test]
        public void LosingAllTargets_RestartsTheCount()
        {
            HelpRules rules = new HelpRules();
            rules.Clicked(InteractionKind.Interface);
            Run(rules, 9f, true, false);
            Run(rules, 0.2f, false, false);
            Assert.AreEqual(HelpMode.None, Run(rules, 9f, true, false).Mode);
        }

        [Test]
        public void Stopped_ShowsNothing_AndIgnoresClicks()
        {
            HelpRules rules = new HelpRules();
            rules.Stop();
            Assert.AreEqual(HelpMode.None, Run(rules, 20f, true, true).Mode);
            rules.Clicked(InteractionKind.Object3D);
            Assert.IsFalse(rules.IsLearned(InteractionKind.Object3D), "a click during a pause teaches nothing");
            rules.Resume();
            rules.Clicked(InteractionKind.Interface);
            HelpDecision decision = Run(rules, 1.2f, false, true);
            Assert.AreEqual(HelpMode.FirstTime, decision.Mode);
            Assert.AreEqual(InteractionKind.Object3D, decision.Kind);
        }

        [Test]
        public void Stop_IsASimpleSwitch()
        {
            HelpRules rules = new HelpRules();
            rules.Stop();
            rules.Stop();
            rules.Resume();
            Assert.IsTrue(rules.Observing);
        }
    }
}
