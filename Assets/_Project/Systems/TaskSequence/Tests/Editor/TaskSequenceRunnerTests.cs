using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RideSafe.TaskSequence.Tests
{
    public class TaskSequenceRunnerTests
    {
        private SequenceFactory _sequences;
        private TaskContextService _entities;
        private TaskSequenceRunner _runner;
        private List<TaskSequenceStatus> _finished;
        private List<string> _completedSteps;
        private List<string> _failedSteps;

        [SetUp]
        public void SetUp()
        {
            TaskLog.Verbose = false;
            _sequences = new SequenceFactory();
            _entities = new TaskContextService();
            _runner = new TaskSequenceRunner(_entities);

            _finished = new List<TaskSequenceStatus>();
            _completedSteps = new List<string>();
            _failedSteps = new List<string>();
            _runner.SequenceFinished += (r, status) => _finished.Add(status);
            _runner.StepCompleted += (r, step, success) => (success ? _completedSteps : _failedSteps).Add(step.StepId);
        }

        [TearDown]
        public void TearDown()
        {
            _runner.Abort("teardown");
            _sequences.DestroyAll();
            TaskLog.Verbose = true;
        }

        private static TaskStepData Step(string id, ITaskValidator validator = null,
            StepCompletionMode mode = StepCompletionMode.Immediate, float feedback = 0f, float timeout = 0f,
            StepFailurePolicy failure = StepFailurePolicy.Retry, string entity = null,
            MissingEntityPolicy missing = MissingEntityPolicy.SkipStep, SkipPolicy skip = SkipPolicy.SkipStep) =>
            new TaskStepData(id, validator, entity, mode, feedback, timeout, failure, missing, skip);

        #region Basic lifecycle

        [Test]
        public void ImmediateStep_CompletesWhenValidatorSatisfied_AndSequenceCompletes()
        {
            FakeValidator validator = new FakeValidator();
            Assert.IsTrue(_runner.Start(_sequences.Create("seq", Step("a", validator))));

            _runner.TickUntilWaiting();
            _runner.TickFrames(3);
            Assert.IsTrue(_runner.IsRunning, "must wait while the validator is unsatisfied");

            validator.Satisfied = true;
            _runner.TickFrames(1);

            Assert.AreEqual(TaskSequenceStatus.Completed, _runner.Status);
            CollectionAssert.AreEqual(new[] { "a" }, _completedSteps);
            CollectionAssert.AreEqual(new[] { TaskSequenceStatus.Completed }, _finished);
            Assert.AreEqual(1, validator.PrepareCount);
            Assert.AreEqual(1, validator.CleanupCount);
        }

        [Test]
        public void StepWithoutValidator_PassesAsPresentationOnly()
        {
            _runner.Start(_sequences.Create("seq", Step("intro"), Step("outro")));
            _runner.TickFrames(4);

            Assert.AreEqual(TaskSequenceStatus.Completed, _runner.Status);
            CollectionAssert.AreEqual(new[] { "intro", "outro" }, _completedSteps);
        }

        [Test]
        public void Start_WhileRunning_IsRefused()
        {
            _runner.Start(_sequences.Create("first", Step("a", new FakeValidator())));

            LogAssert.Expect(LogType.Error, new Regex("already running"));
            Assert.IsFalse(_runner.Start(_sequences.Create("second", Step("b"))));
            Assert.AreEqual("first", _runner.SequenceId);
        }

        [Test]
        public void Start_EmptySequence_IsRefused()
        {
            LogAssert.Expect(LogType.Error, new Regex("no steps"));
            Assert.IsFalse(_runner.Start(_sequences.Create("empty")));
            Assert.AreEqual(TaskSequenceStatus.Idle, _runner.Status);
        }

        [Test]
        public void Abort_CleansUpValidator_AndReportsAborted()
        {
            FakeValidator validator = new FakeValidator();
            _runner.Start(_sequences.Create("seq", Step("a", validator)));
            _runner.TickUntilWaiting();

            _runner.Abort("test");

            Assert.AreEqual(TaskSequenceStatus.Aborted, _runner.Status);
            Assert.AreEqual(1, validator.CleanupCount);
            Assert.AreEqual(TaskStepPhase.None, _runner.Phase);
        }

        #endregion

        #region Completion modes

        [Test]
        public void ManualStep_DoesNotSelfAdvance_EvenWithZeroFeedbackDuration()
        {
            FakeValidator validator = new FakeValidator();
            _runner.Start(_sequences.Create("seq", Step("manual", validator, StepCompletionMode.Manual, feedback: 0f)));
            _runner.TickUntilWaiting();

            validator.Satisfied = true;
            _runner.TickFrames(50);

            Assert.IsTrue(_runner.IsRunning);
            Assert.AreEqual(TaskStepPhase.Feedback, _runner.Phase);
            Assert.IsEmpty(_completedSteps);

            _runner.CompleteCurrentStep();

            Assert.AreEqual(TaskSequenceStatus.Completed, _runner.Status);
            CollectionAssert.AreEqual(new[] { "manual" }, _completedSteps);
        }

        [Test]
        public void ManualStep_WithFeedbackDuration_StillWaitsForCompleteCurrentStep()
        {
            FakeValidator validator = new FakeValidator();
            _runner.Start(_sequences.Create("seq", Step("manual", validator, StepCompletionMode.Manual, feedback: 0.2f)));
            _runner.TickUntilWaiting();

            validator.Satisfied = true;
            _runner.TickFrames(50);

            Assert.IsTrue(_runner.IsRunning, "Manual must ignore the feedback timer");
            _runner.CompleteCurrentStep();
            Assert.AreEqual(TaskSequenceStatus.Completed, _runner.Status);
        }

        [Test]
        public void AfterFeedbackStep_DwellsForFeedbackDuration_ThenAdvances()
        {
            FakeValidator validator = new FakeValidator();
            _runner.Start(_sequences.Create("seq", Step("a", validator, StepCompletionMode.AfterFeedback, feedback: 1f)));
            _runner.TickUntilWaiting();

            validator.Satisfied = true;
            _runner.TickFrames(1, 0.1f); // validated -> Feedback
            Assert.AreEqual(TaskStepPhase.Feedback, _runner.Phase);

            _runner.TickFrames(5, 0.1f); // 0.5 s of feedback
            Assert.IsTrue(_runner.IsRunning);

            _runner.TickFrames(6, 0.1f); // past 1 s
            Assert.AreEqual(TaskSequenceStatus.Completed, _runner.Status);
        }

        [Test]
        public void CompleteCurrentStep_WorksWhileStillWaiting()
        {
            _runner.Start(_sequences.Create("seq", Step("a", new FakeValidator()), Step("b", new FakeValidator())));
            _runner.TickUntilWaiting();

            _runner.CompleteCurrentStep();

            Assert.AreEqual("b", _runner.StepId);
            CollectionAssert.AreEqual(new[] { "a" }, _completedSteps);
        }

        #endregion

        #region Broken validators fail fast

        [Test]
        public void ValidatorBrokenAfterPrepare_FailsImmediately_AndRetryFallsBackToSkip()
        {
            FakeValidator broken = new FakeValidator { BreakOnPrepare = true };
            FakeValidator next = new FakeValidator();

            LogAssert.Expect(LogType.Error, new Regex("not retryable"));
            _runner.Start(_sequences.Create("seq", Step("broken", broken, failure: StepFailurePolicy.Retry), Step("next", next)));

            // No ticks needed: the learner is never left waiting on a broken step.
            Assert.AreEqual("next", _runner.StepId);
            Assert.AreEqual(1, broken.PrepareCount, "a broken validator is never retried");
            Assert.AreEqual(1, broken.CleanupCount);
            CollectionAssert.AreEqual(new[] { "broken" }, _failedSteps);

            _runner.TickUntilWaiting();
            next.Satisfied = true;
            _runner.TickFrames(1);
            Assert.AreEqual(TaskSequenceStatus.Failed, _runner.Status, "a skipped failure still marks the run as Failed");
        }

        [Test]
        public void ValidatorBrokenDuringEvaluate_FailsOnThatFrame()
        {
            FakeValidator validator = new FakeValidator();
            _runner.Start(_sequences.Create("seq", Step("a", validator, failure: StepFailurePolicy.FailSequence)));
            _runner.TickUntilWaiting();

            validator.IsBroken = true;
            LogAssert.Expect(LogType.Error, new Regex("not retryable"));
            _runner.TickFrames(1);

            Assert.AreEqual(TaskSequenceStatus.Failed, _runner.Status);
        }

        [Test]
        public void ValidatorThrowingInEvaluate_FailsStep_WithoutRetry()
        {
            FakeValidator validator = new FakeValidator { ThrowOnEvaluate = true };
            _runner.Start(_sequences.Create("seq", Step("a", validator, failure: StepFailurePolicy.Retry)));
            _runner.TickUntilWaiting();

            LogAssert.Expect(LogType.Error, new Regex("not retryable"));
            _runner.TickFrames(1);

            Assert.AreEqual(1, validator.PrepareCount);
            Assert.AreEqual(TaskSequenceStatus.Failed, _runner.Status);
        }

        [Test]
        public void ValidatorThrowingInPrepare_FailsStep_InsteadOfPassingAsPresentationOnly()
        {
            FakeValidator validator = new FakeValidator { ThrowOnPrepare = true };

            LogAssert.Expect(LogType.Error, new Regex("not retryable"));
            _runner.Start(_sequences.Create("seq", Step("a", validator)));

            Assert.AreEqual(TaskSequenceStatus.Failed, _runner.Status);
            Assert.IsEmpty(_completedSteps);
            Assert.AreEqual(1, validator.CleanupCount, "cleanup still runs after a throwing Prepare");
        }

        #endregion

        #region Failure policy

        [Test]
        public void Timeout_WithRetry_RearmsSameStep_AndCountsAttempts()
        {
            FakeValidator validator = new FakeValidator();
            List<int> attemptsSeen = new List<int>();
            _runner.StepStarted += (r, step) => attemptsSeen.Add(r.StepAttempt);

            _runner.Start(_sequences.Create("seq", Step("a", validator, timeout: 1f, failure: StepFailurePolicy.Retry)));
            _runner.TickUntilWaiting();
            _runner.TickFrames(11, 0.1f);

            Assert.IsTrue(_runner.IsRunning);
            Assert.AreEqual("a", _runner.StepId);
            Assert.AreEqual(2, _runner.StepAttempt);
            Assert.AreEqual(2, validator.PrepareCount);
            Assert.AreEqual(1, validator.CleanupCount);
            CollectionAssert.AreEqual(new[] { 1, 2 }, attemptsSeen);

            _runner.TickUntilWaiting();
            validator.Satisfied = true;
            _runner.TickFrames(1);

            Assert.AreEqual(TaskSequenceStatus.Completed, _runner.Status, "a retried-then-passed step is a full pass");
        }

        [Test]
        public void Timeout_WithSkip_MovesOn_AndSequenceEndsFailed()
        {
            _runner.Start(_sequences.Create("seq",
                Step("a", new FakeValidator(), timeout: 0.5f, failure: StepFailurePolicy.Skip),
                Step("b")));
            _runner.TickUntilWaiting();
            _runner.TickFrames(10, 0.1f);

            Assert.AreEqual(TaskSequenceStatus.Failed, _runner.Status);
            CollectionAssert.AreEqual(new[] { "a" }, _failedSteps);
            CollectionAssert.AreEqual(new[] { "b" }, _completedSteps);
        }

        [Test]
        public void Timeout_WithFailSequence_StopsWithoutPreparingLaterSteps()
        {
            FakeValidator later = new FakeValidator();
            _runner.Start(_sequences.Create("seq",
                Step("a", new FakeValidator(), timeout: 0.5f, failure: StepFailurePolicy.FailSequence),
                Step("b", later)));
            _runner.TickUntilWaiting();
            _runner.TickFrames(10, 0.1f);

            Assert.AreEqual(TaskSequenceStatus.Failed, _runner.Status);
            Assert.AreEqual(0, later.PrepareCount);
        }

        [Test]
        public void AfterFeedback_Failure_ShowsFeedbackBeforeApplyingPolicy()
        {
            _runner.Start(_sequences.Create("seq",
                Step("a", new FakeValidator(), StepCompletionMode.AfterFeedback, feedback: 1f, timeout: 0.5f,
                    failure: StepFailurePolicy.Skip)));
            _runner.TickUntilWaiting();
            _runner.TickFrames(6, 0.1f); // timed out -> Feedback

            Assert.AreEqual(TaskStepPhase.Feedback, _runner.Phase);
            Assert.IsEmpty(_failedSteps);

            _runner.TickFrames(11, 0.1f);
            Assert.AreEqual(TaskSequenceStatus.Failed, _runner.Status);
        }

        [Test]
        public void CompleteCurrentStep_False_GoesThroughFailurePolicy()
        {
            FakeValidator validator = new FakeValidator();
            _runner.Start(_sequences.Create("seq", Step("a", validator, failure: StepFailurePolicy.Retry)));
            _runner.TickUntilWaiting();

            _runner.CompleteCurrentStep(false);

            Assert.AreEqual(2, _runner.StepAttempt);
            Assert.IsTrue(_runner.IsRunning);
        }

        #endregion

        #region Entities

        [Test]
        public void RegisteredEntity_IsResolvedIntoStepContext()
        {
            FakeEntity entity = new FakeEntity("tutorial.target");
            _entities.RegisterEntity(entity);
            FakeValidator validator = new FakeValidator();

            _runner.Start(_sequences.Create("seq", Step("a", validator, entity: "Tutorial.Target")));

            Assert.AreSame(entity, validator.LastContext.Entity, "ids are case-insensitive");
        }

        [Test]
        public void MissingEntity_SkipStep_ContinuesAndFailsRun()
        {
            LogAssert.Expect(LogType.Error, new Regex("is not registered"));
            _runner.Start(_sequences.Create("seq",
                Step("a", new FakeValidator(), entity: "ghost", missing: MissingEntityPolicy.SkipStep),
                Step("b")));

            Assert.AreEqual("b", _runner.StepId);
            _runner.TickFrames(2);
            Assert.AreEqual(TaskSequenceStatus.Failed, _runner.Status);
        }

        [Test]
        public void MissingEntity_FailSequence_StopsImmediately()
        {
            LogAssert.Expect(LogType.Error, new Regex("is not registered"));
            _runner.Start(_sequences.Create("seq",
                Step("a", new FakeValidator(), entity: "ghost", missing: MissingEntityPolicy.FailSequence),
                Step("b")));

            Assert.AreEqual(TaskSequenceStatus.Failed, _runner.Status);
        }

        [Test]
        public void MissingEntity_ContinueWithoutEntity_RunsStepWithNullEntity()
        {
            FakeValidator validator = new FakeValidator();
            LogAssert.Expect(LogType.Error, new Regex("is not registered"));
            _runner.Start(_sequences.Create("seq",
                Step("a", validator, entity: "ghost", missing: MissingEntityPolicy.ContinueWithoutEntity)));

            Assert.AreEqual("a", _runner.StepId);
            Assert.IsNull(validator.LastContext.Entity);
        }

        #endregion

        #region Context requirements

        private static ContextRequirement[] RequiresEscooter() =>
            new[] { new ContextRequirement("vehicle", "escooter") };

        [Test]
        public void ContextRequirements_WithNoLookupWired_AreIgnored()
        {
            _runner.ContextLookup = null;
            TaskSequenceSO sequence = _sequences.Create("seq", RestartPolicy.FromBeginning, SkipPolicy.NotSkippable,
                RequiresEscooter(), Step("a", new FakeValidator()));

            Assert.IsTrue(_runner.Start(sequence));
        }

        [Test]
        public void ContextRequirements_Mismatch_BlocksStart()
        {
            _runner.ContextLookup = key => key == "vehicle" ? "bicycle" : null;
            TaskSequenceSO sequence = _sequences.Create("seq", RestartPolicy.FromBeginning, SkipPolicy.NotSkippable,
                RequiresEscooter(), Step("a", new FakeValidator()));

            Assert.IsFalse(_runner.Start(sequence));
        }

        [Test]
        public void ContextRequirements_Match_AllowsStart()
        {
            _runner.ContextLookup = key => key == "vehicle" ? "EScooter" : null;
            TaskSequenceSO sequence = _sequences.Create("seq", RestartPolicy.FromBeginning, SkipPolicy.NotSkippable,
                RequiresEscooter(), Step("a", new FakeValidator()));

            Assert.IsTrue(_runner.Start(sequence));
        }

        #endregion

        #region Restart and skip policies

        [Test]
        public void RunOnce_RefusesSecondRunAfterCompletion()
        {
            TaskSequenceSO sequence = _sequences.Create("once", RestartPolicy.RunOnce, SkipPolicy.NotSkippable, null, Step("a"));
            _runner.Start(sequence);
            _runner.TickFrames(3);
            Assert.AreEqual(TaskSequenceStatus.Completed, _runner.Status);

            Assert.IsFalse(_runner.Start(sequence));
        }

        [Test]
        public void RunOnce_AllowsRetryAfterAbort()
        {
            TaskSequenceSO sequence = _sequences.Create("once", RestartPolicy.RunOnce, SkipPolicy.NotSkippable, null,
                Step("a", new FakeValidator()));
            _runner.Start(sequence);
            _runner.Abort();

            Assert.IsTrue(_runner.Start(sequence));
        }

        [Test]
        public void Resume_RestartsAtStepThatWasAborted()
        {
            TaskSequenceSO sequence = _sequences.Create("resume", RestartPolicy.Resume, SkipPolicy.NotSkippable, null,
                Step("a"), Step("b", new FakeValidator()), Step("c"));
            _runner.Start(sequence);
            _runner.TickFrames(3);
            Assert.AreEqual("b", _runner.StepId);
            _runner.Abort();

            _runner.Start(sequence);

            Assert.AreEqual("b", _runner.StepId);
        }

        [Test]
        public void FromBeginning_RestartsAtFirstStep()
        {
            TaskSequenceSO sequence = _sequences.Create("seq", Step("a"), Step("b", new FakeValidator()));
            _runner.Start(sequence);
            _runner.TickFrames(3);
            _runner.Abort();

            _runner.Start(sequence);

            Assert.AreEqual("a", _runner.StepId);
        }

        [Test]
        public void SkipSequence_RefusedWhenNotSkippable_AllowedOtherwise()
        {
            _runner.Start(_sequences.Create("locked", Step("a", new FakeValidator())));
            Assert.IsFalse(_runner.SkipSequence());
            _runner.Abort();

            _runner.Start(_sequences.Create("open", RestartPolicy.FromBeginning, SkipPolicy.SkipWholeSequence, null,
                Step("a", new FakeValidator())));
            Assert.IsTrue(_runner.SkipSequence());
            Assert.AreEqual(TaskSequenceStatus.Skipped, _runner.Status);
        }

        [Test]
        public void SkipCurrentStep_FollowsStepPolicy()
        {
            _runner.Start(_sequences.Create("seq",
                Step("locked", new FakeValidator(), skip: SkipPolicy.NotSkippable),
                Step("b")));
            Assert.IsFalse(_runner.SkipCurrentStep());
            _runner.Abort();

            _runner.Start(_sequences.Create("seq2", Step("a", new FakeValidator(), skip: SkipPolicy.SkipStep), Step("b", new FakeValidator())));
            Assert.IsTrue(_runner.SkipCurrentStep());
            Assert.AreEqual("b", _runner.StepId);
            _runner.Abort();

            _runner.Start(_sequences.Create("seq3", Step("a", new FakeValidator(), skip: SkipPolicy.SkipWholeSequence), Step("b")));
            Assert.IsTrue(_runner.SkipCurrentStep());
            Assert.AreEqual(TaskSequenceStatus.Skipped, _runner.Status);
        }

        #endregion

        #region Re-entrancy

        [Test]
        public void AbortFromStepStarted_StopsCleanly()
        {
            FakeValidator validator = new FakeValidator();
            _runner.StepStarted += (r, step) => r.Abort("from callback");

            _runner.Start(_sequences.Create("seq", Step("a", validator), Step("b")));

            Assert.AreEqual(TaskSequenceStatus.Aborted, _runner.Status);
            Assert.AreEqual(1, validator.CleanupCount);
            Assert.AreEqual(TaskStepPhase.None, _runner.Phase);
            _runner.TickFrames(3);
            Assert.IsEmpty(_completedSteps);
        }

        [Test]
        public void CompleteFromStepStarted_AdvancesOnce()
        {
            _runner.StepStarted += (r, step) =>
            {
                if (step.StepId == "a")
                    r.CompleteCurrentStep();
            };

            _runner.Start(_sequences.Create("seq", Step("a", new FakeValidator()), Step("b", new FakeValidator())));

            Assert.AreEqual("b", _runner.StepId);
            CollectionAssert.AreEqual(new[] { "a" }, _completedSteps);
        }

        [Test]
        public void StartingNextSequenceFromSequenceFinished_Works()
        {
            TaskSequenceSO second = _sequences.Create("second", Step("x", new FakeValidator()));
            _runner.SequenceFinished += (r, status) =>
            {
                if (r.SequenceId == "first")
                    r.Start(second);
            };

            _runner.Start(_sequences.Create("first", Step("a")));
            _runner.TickFrames(3);

            Assert.IsTrue(_runner.IsRunning);
            Assert.AreEqual("second", _runner.SequenceId);
            Assert.AreEqual("x", _runner.StepId);
        }

        #endregion
    }
}
