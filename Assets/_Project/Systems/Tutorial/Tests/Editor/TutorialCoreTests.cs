using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cachacos;
using NUnit.Framework;
using RideSafe.TaskSequence;
using RideSafe.TaskSequence.Tests;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using EntityId = RideSafe.TaskSequence.EntityId;

namespace RideSafe.Tutorial.Tests
{
    /// <summary>Input service with explicit frames: Press() raises an edge until EndFrame().</summary>
    internal sealed class FakeInput : ITutorialInputService
    {
        private readonly HashSet<ActionId> _mapped = new HashSet<ActionId>();
        private readonly HashSet<ActionId> _pressedThisFrame = new HashSet<ActionId>();

        public FakeInput(params string[] mapped)
        {
            foreach (string action in mapped)
                _mapped.Add(new ActionId(action));
        }

        public string ProfileId => "fake";
        public bool IsMapped(ActionId action) => _mapped.Contains(action);
        public bool WasPerformedThisFrame(ActionId action) => _pressedThisFrame.Contains(action);
        public bool IsHeld(ActionId action) => false;
        public float ReadValue(ActionId action) => 0f;
        public event Action<ActionId> ActionPerformed { add { } remove { } }

        public void Press(string action) => _pressedThisFrame.Add(new ActionId(action));
        public void EndFrame() => _pressedThisFrame.Clear();
    }

    internal sealed class FakeConfirmableEntity : IConfirmableTaskEntity
    {
        public FakeConfirmableEntity(string id) => Id = new EntityId(id);
        public EntityId Id { get; }
        public Transform Transform => null;
        public bool IsAvailable => true;
        public event Action<ITaskEntity> SelectionConfirmed;
        public void Confirm() => SelectionConfirmed?.Invoke(this);
    }

    /// <summary>Runs tutorial validators through the real runner, with services in the ServiceLocator.</summary>
    public abstract class TutorialRunnerFixture
    {
        private readonly List<Action> _deregister = new List<Action>();
        protected SequenceFactory Sequences;
        protected TaskContextService Entities;
        protected TaskSequenceRunner Runner;

        [SetUp]
        public void BaseSetUp()
        {
            TaskLog.Verbose = false;
            Assert.IsNull(ServiceLocator.Instance.RequestService<ITutorialInputService>(),
                "a previous test leaked an input service");
            Sequences = new SequenceFactory();
            Entities = new TaskContextService();
            Runner = new TaskSequenceRunner(Entities);
        }

        [TearDown]
        public void BaseTearDown()
        {
            Runner.Abort();
            for (int i = _deregister.Count - 1; i >= 0; i--)
                _deregister[i]();
            _deregister.Clear();
            Sequences.DestroyAll();
            TaskLog.Verbose = true;
        }

        protected void Provide<T>(T service) where T : class
        {
            Assert.IsTrue(ServiceRegistration.TryRegister(service));
            _deregister.Add(() => ServiceRegistration.Deregister(service));
        }

        protected void StartSingleStep(ITaskValidator validator, string entity = null)
        {
            Assert.IsTrue(Runner.Start(Sequences.Create("seq",
                new TaskStepData("step", validator, entity, failurePolicy: StepFailurePolicy.Skip))));
        }
    }

    public class CustomEventValidatorTests : TutorialRunnerFixture
    {
        private float _now;
        private TutorialSignalBus _bus;

        [SetUp]
        public void SetUp()
        {
            _now = 100f;
            _bus = new TutorialSignalBus(() => _now);
            Provide(_bus);
        }

        [Test]
        public void SignalAfterArming_Satisfies()
        {
            StartSingleStep(new CustomEventValidator("garage.door.opened"));
            Runner.TickUntilWaiting();
            Runner.TickFrames(2);
            Assert.IsTrue(Runner.IsRunning);

            _bus.Raise("garage.door.opened");
            Runner.TickFrames(1);

            Assert.AreEqual(TaskSequenceStatus.Completed, Runner.Status);
        }

        [Test]
        public void SignalJustBeforeArming_SatisfiesWithinLatchWindow()
        {
            _bus.Raise("garage.door.opened");
            _now += 1f;

            StartSingleStep(new CustomEventValidator("garage.door.opened", latchWindowSeconds: 2f));
            Runner.TickUntilWaiting();
            Runner.TickFrames(1);

            Assert.AreEqual(TaskSequenceStatus.Completed, Runner.Status);
        }

        [Test]
        public void OldSignal_OutsideLatchWindow_IsIgnored()
        {
            _bus.Raise("garage.door.opened");
            _now += 60f;

            StartSingleStep(new CustomEventValidator("garage.door.opened", latchWindowSeconds: 2f));
            Runner.TickUntilWaiting();
            Runner.TickFrames(3);

            Assert.IsTrue(Runner.IsRunning, "a signal from a minute ago must not complete a new step");
        }

        [Test]
        public void EarlierSignal_WithZeroWindow_IsIgnored()
        {
            _bus.Raise("garage.door.opened");

            StartSingleStep(new CustomEventValidator("garage.door.opened"));
            Runner.TickUntilWaiting();
            Runner.TickFrames(3);

            Assert.IsTrue(Runner.IsRunning);
        }
    }

    public class SelectionConfirmedValidatorTests : TutorialRunnerFixture
    {
        [Test]
        public void EntityOnly_WithoutInputService_IsNotBroken_AndConfirmsViaEntity()
        {
            FakeConfirmableEntity entity = new FakeConfirmableEntity("garage.option.neutral");
            Entities.RegisterEntity(entity);

            StartSingleStep(new SelectionConfirmedValidator(acceptConfirmAction: true), "garage.option.neutral");
            Runner.TickUntilWaiting();
            Runner.TickFrames(2);
            Assert.IsTrue(Runner.IsRunning, "not broken, waiting for the entity");

            entity.Confirm();
            Runner.TickFrames(1);

            Assert.AreEqual(TaskSequenceStatus.Completed, Runner.Status);
        }

        [Test]
        public void EntityOnly_WithUnmappedConfirmAction_StillConfirmsViaEntity()
        {
            Provide<ITutorialInputService>(new FakeInput());
            FakeConfirmableEntity entity = new FakeConfirmableEntity("garage.option.neutral");
            Entities.RegisterEntity(entity);

            StartSingleStep(new SelectionConfirmedValidator(acceptConfirmAction: true), "garage.option.neutral");
            Runner.TickUntilWaiting();
            entity.Confirm();
            Runner.TickFrames(1);

            Assert.AreEqual(TaskSequenceStatus.Completed, Runner.Status);
        }

        [Test]
        public void ActionRoute_ConfirmsWithoutEntity()
        {
            FakeInput input = new FakeInput(ActionIds.Confirm);
            Provide<ITutorialInputService>(input);

            StartSingleStep(new SelectionConfirmedValidator(acceptConfirmAction: true));
            Runner.TickUntilWaiting();
            input.Press(ActionIds.Confirm);
            Runner.TickFrames(1);

            Assert.AreEqual(TaskSequenceStatus.Completed, Runner.Status);
        }

        [Test]
        public void NeitherRoute_BreaksAndFailsFast()
        {
            LogAssert.Expect(LogType.Error, new Regex("cannot run"));
            LogAssert.Expect(LogType.Error, new Regex("not retryable"));

            StartSingleStep(new SelectionConfirmedValidator(acceptConfirmAction: true));

            Assert.AreEqual(TaskSequenceStatus.Failed, Runner.Status);
        }
    }

    public class InputPressedValidatorTests : TutorialRunnerFixture
    {
        [Test]
        public void UnmappedAction_BreaksAndFailsFast()
        {
            Provide<ITutorialInputService>(new FakeInput());

            LogAssert.Expect(LogType.Error, new Regex("cannot run"));
            LogAssert.Expect(LogType.Error, new Regex("not retryable"));
            StartSingleStep(new InputPressedValidator(ActionIds.PrimarySelect));

            Assert.AreEqual(TaskSequenceStatus.Failed, Runner.Status);
        }

        [Test]
        public void PressThatCompletesOneStep_DoesNotCarryIntoTheNext()
        {
            FakeInput input = new FakeInput(ActionIds.PrimarySelect);
            Provide<ITutorialInputService>(input);
            Runner.Start(Sequences.Create("seq",
                new TaskStepData("first", new InputPressedValidator(ActionIds.PrimarySelect)),
                new TaskStepData("second", new InputPressedValidator(ActionIds.PrimarySelect))));
            Runner.TickUntilWaiting();

            input.Press(ActionIds.PrimarySelect);
            Runner.TickFrames(1);
            input.EndFrame();
            Assert.AreEqual("second", Runner.StepId);

            Runner.TickFrames(3);
            Assert.AreEqual("second", Runner.StepId, "a single press must complete a single step (CASE 05)");

            input.Press(ActionIds.PrimarySelect);
            Runner.TickFrames(1);
            Assert.AreEqual(TaskSequenceStatus.Completed, Runner.Status);
        }
    }

    public class HeadLookAtEntityValidatorTests : TutorialRunnerFixture
    {
        private sealed class FakePose : IXRPoseProvider
        {
            public Pose Head = new Pose(Vector3.zero, Quaternion.identity);
            public bool IsTracked(XRNodeRole role) => true;

            public bool TryGetPose(XRNodeRole role, out Pose pose)
            {
                pose = Head;
                return role == XRNodeRole.Head;
            }
        }

        private sealed class TransformEntity : ITaskEntity
        {
            public TransformEntity(string id, Transform transform)
            {
                Id = new EntityId(id);
                Transform = transform;
            }

            public EntityId Id { get; }
            public Transform Transform { get; }
            public bool IsAvailable => true;
        }

        private GameObject _lamp;
        private FakePose _pose;

        [SetUp]
        public void SetUp()
        {
            _pose = new FakePose();
            Provide<IXRPoseProvider>(_pose);
            _lamp = new GameObject("lamp");
            _lamp.transform.position = new Vector3(-2f, 0f, 1f); // ~63 degrees to the left
            Entities.RegisterEntity(new TransformEntity("tutorial.look.left", _lamp.transform));
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_lamp);

        [Test]
        public void LookingAhead_DoesNotPass_LookingAtTargetForHoldTime_Passes()
        {
            StartSingleStep(new HeadLookAtEntityValidator(maxAngle: 20f, holdSeconds: 0.3f), "tutorial.look.left");
            Runner.TickUntilWaiting();
            Runner.TickFrames(10);
            Assert.IsTrue(Runner.IsRunning, "facing forward is not looking at the lamp");

            _pose.Head = new Pose(Vector3.zero, Quaternion.LookRotation(_lamp.transform.position));
            Runner.TickFrames(2);
            Assert.IsTrue(Runner.IsRunning, "must hold the look, not just sweep past");

            Runner.TickFrames(3);
            Assert.AreEqual(TaskSequenceStatus.Completed, Runner.Status);
        }

        [Test]
        public void LookingAway_ResetsTheHoldTimer()
        {
            StartSingleStep(new HeadLookAtEntityValidator(maxAngle: 20f, holdSeconds: 0.3f), "tutorial.look.left");
            Runner.TickUntilWaiting();

            _pose.Head = new Pose(Vector3.zero, Quaternion.LookRotation(_lamp.transform.position));
            Runner.TickFrames(2);
            _pose.Head = new Pose(Vector3.zero, Quaternion.identity);
            Runner.TickFrames(1);
            _pose.Head = new Pose(Vector3.zero, Quaternion.LookRotation(_lamp.transform.position));
            Runner.TickFrames(2);

            Assert.IsTrue(Runner.IsRunning);
        }
    }

    public class LearnedActionsTests
    {
        [Test]
        public void NothingIsLearnedUntilMarked()
        {
            TutorialProgress progress = new TutorialProgress();

            Assert.IsFalse(progress.IsActionLearned(new ActionId(ActionIds.PrimarySelect)));
            Assert.IsFalse(progress.IsActionLearned(new ActionId(ActionIds.Confirm)));
            Assert.IsFalse(progress.IsActionLearned(new ActionId(ActionIds.Back)));
        }

        [Test]
        public void MarkedAction_IsLearned_OthersStayNotIntroduced()
        {
            TutorialProgress progress = new TutorialProgress();

            progress.MarkActionLearned(new ActionId(ActionIds.PrimarySelect));

            Assert.IsTrue(progress.IsActionLearned(new ActionId(ActionIds.PrimarySelect)));
            Assert.IsTrue(progress.IsActionLearned(new ActionId("PrimarySelect")), "ids are case-insensitive");
            Assert.IsFalse(progress.IsActionLearned(new ActionId(ActionIds.Confirm)));
        }

        [Test]
        public void InvalidActionId_IsIgnored()
        {
            TutorialProgress progress = new TutorialProgress();

            progress.MarkActionLearned(ActionId.None);

            Assert.IsFalse(progress.IsActionLearned(ActionId.None));
        }

        [Test]
        public void Clear_ForgetsLearnedActions()
        {
            TutorialProgress progress = new TutorialProgress();
            progress.MarkActionLearned(new ActionId(ActionIds.Confirm));

            progress.Clear();

            Assert.IsFalse(progress.IsActionLearned(new ActionId(ActionIds.Confirm)));
        }
    }

    public class TutorialSessionScopeTests
    {
        private sealed class DummyService { }

        [Test]
        public void OnlyTheOwningSceneUnload_DisposesTheScope()
        {
            int teardowns = 0;
            TutorialSessionScope scope = new TutorialSessionScope(SceneManager.GetActiveScene());
            scope.AddCleanup(() => teardowns++);

            scope.HandleSceneUnloaded(default(Scene));
            Assert.IsFalse(scope.IsDisposed, "another scene unloading must not end the session");

            scope.HandleSceneUnloaded(SceneManager.GetActiveScene());
            Assert.IsTrue(scope.IsDisposed);
            Assert.AreEqual(1, teardowns);
        }

        [Test]
        public void WithoutOwningScene_OnlyExplicitDisposeEndsIt()
        {
            TutorialSessionScope scope = new TutorialSessionScope(default(Scene));

            scope.HandleSceneUnloaded(SceneManager.GetActiveScene());
            Assert.IsFalse(scope.IsDisposed);

            scope.Dispose();
            Assert.IsTrue(scope.IsDisposed);
        }

        [Test]
        public void AddService_DoesNotStealOrLaterRemoveAnotherOwnersService()
        {
            DummyService owner = new DummyService();
            Assert.IsTrue(ServiceRegistration.TryRegister(owner));
            TutorialSessionScope scope = new TutorialSessionScope(default(Scene));
            try
            {
                LogAssert.Expect(LogType.Error, new Regex("already registered"));
                scope.AddService(new DummyService());

                scope.Dispose();

                Assert.AreSame(owner, ServiceLocator.Instance.RequestService<DummyService>());
            }
            finally
            {
                scope.Dispose();
                ServiceRegistration.Deregister(owner);
            }
        }

        [Test]
        public void Dispose_DeregistersServicesItRegistered()
        {
            TutorialSessionScope scope = new TutorialSessionScope(default(Scene));
            DummyService service = new DummyService();
            scope.AddService(service);
            Assert.AreSame(service, ServiceLocator.Instance.RequestService<DummyService>());

            scope.Dispose();

            Assert.IsNull(ServiceLocator.Instance.RequestService<DummyService>());
        }
    }
}
