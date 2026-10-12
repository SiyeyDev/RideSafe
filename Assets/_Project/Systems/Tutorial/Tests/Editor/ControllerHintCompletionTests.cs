using NUnit.Framework;
using RideSafe.TaskSequence;
using RideSafe.TaskSequence.Tests;

namespace RideSafe.Tutorial.Tests
{
    /// <summary>
    /// The 3D hint is instructive only. These run the real runner with the hint animating
    /// next to it: however long the demonstration plays, only the learner's input counts.
    /// </summary>
    public class ControllerHintCompletionTests : TutorialRunnerFixture
    {
        private FakeInput _input;
        private TouchPlusRig _rig;

        [SetUp]
        public void SetUp()
        {
            _input = new FakeInput(ActionIds.PrimarySelect);
            Provide<ITutorialInputService>(_input);
            _rig = new TouchPlusRig();
        }

        [TearDown]
        public void TearDown() => _rig.Destroy();

        [Test]
        public void HintAnimation_NeverCompletesTheStep()
        {
            StartSingleStep(new InputPressedValidator(ActionIds.PrimarySelect));
            Runner.TickUntilWaiting();
            Assert.IsTrue(_rig.View.Show(new PhysicalBinding(XRNodeRole.RightHand, ControllerControl.Trigger)));

            // Ten seconds of demonstrated presses, including a full release cycle.
            for (int frame = 0; frame < 600; frame++)
            {
                _rig.View.Tick(1f / 60f);
                Runner.TickFrames(1);
            }
            _rig.View.Release();
            for (int frame = 0; frame < 60; frame++)
            {
                _rig.View.Tick(1f / 60f);
                Runner.TickFrames(1);
            }

            Assert.IsTrue(Runner.IsRunning);
            Assert.AreEqual(TaskStepPhase.WaitForInteraction, Runner.Phase);
        }

        [Test]
        public void RealInput_CompletesTheStep_WhileTheHintIsShowing()
        {
            StartSingleStep(new InputPressedValidator(ActionIds.PrimarySelect));
            Runner.TickUntilWaiting();
            _rig.View.Show(new PhysicalBinding(XRNodeRole.RightHand, ControllerControl.Trigger));
            _rig.View.Tick(0.3f);

            _input.Press(ActionIds.PrimarySelect);
            Runner.TickFrames(1);
            _input.EndFrame();

            Assert.AreEqual(TaskSequenceStatus.Completed, Runner.Status);
        }
    }
}
