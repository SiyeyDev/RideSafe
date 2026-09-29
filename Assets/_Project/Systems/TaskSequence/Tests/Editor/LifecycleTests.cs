using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cachacos;
using NUnit.Framework;
using RideSafe.TaskSequence.Bridge;
using StepCommand;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace RideSafe.TaskSequence.Tests
{
    /// <summary>ITaskSequenceService over a real runner, ticked by the test instead of Update.</summary>
    internal sealed class RunnerBackedService : ITaskSequenceService
    {
        public readonly TaskSequenceRunner Runner;

        public RunnerBackedService()
        {
            Entities = new TaskContextService();
            Runner = new TaskSequenceRunner(Entities);
        }

        public TaskContextService Entities { get; }
        public bool IsRunning => Runner.IsRunning;
        public string CurrentSequenceId => Runner.SequenceId;
        public TaskSequenceHandle Run(TaskSequenceSO sequence) => TaskSequenceHandle.Start(Runner, sequence);
        public TaskSequenceHandle RunById(string sequenceId) => null;
        public void AbortCurrent(string reason = null) => Runner.Abort(reason);
    }

    public class TaskSequenceHandleTests
    {
        private SequenceFactory _sequences;
        private TaskSequenceRunner _runner;

        [SetUp]
        public void SetUp()
        {
            TaskLog.Verbose = false;
            _sequences = new SequenceFactory();
            _runner = new TaskSequenceRunner(new TaskContextService());
        }

        [TearDown]
        public void TearDown()
        {
            _runner.Abort();
            _sequences.DestroyAll();
            TaskLog.Verbose = true;
        }

        [Test]
        public void SequenceFinishingInsideStart_StillNotifiesHandle()
        {
            TaskSequenceSO sequence = _sequences.Create("seq",
                new TaskStepData("a", new FakeValidator(), "ghost", missingEntityPolicy: MissingEntityPolicy.SkipStep));

            LogAssert.Expect(LogType.Error, new Regex("is not registered"));
            TaskSequenceHandle handle = TaskSequenceHandle.Start(_runner, sequence);

            TaskSequenceStatus? reported = null;
            handle.OnFinished(status => reported = status);

            Assert.AreEqual(TaskSequenceStatus.Failed, reported);
            Assert.IsFalse(handle.IsRunning);
        }

        [Test]
        public void StaleHandle_KeepsItsOwnResult_AndCannotAbortTheNextRun()
        {
            TaskSequenceHandle first = TaskSequenceHandle.Start(_runner, _sequences.Create("first", new TaskStepData("a")));
            _runner.TickFrames(3);
            TaskSequenceHandle second = TaskSequenceHandle.Start(_runner,
                _sequences.Create("second", new TaskStepData("b", new FakeValidator())));

            first.Abort("stale");

            Assert.AreEqual(TaskSequenceStatus.Completed, first.Status);
            Assert.IsFalse(first.IsRunning);
            Assert.AreEqual("first", first.SequenceId);
            Assert.IsTrue(second.IsRunning, "a stale handle must not abort someone else's run");
        }

        [Test]
        public void RefusedStart_ReturnsNull()
        {
            TaskSequenceHandle.Start(_runner, _sequences.Create("first", new TaskStepData("a", new FakeValidator())));

            LogAssert.Expect(LogType.Error, new Regex("already running"));
            Assert.IsNull(TaskSequenceHandle.Start(_runner, _sequences.Create("second", new TaskStepData("b"))));
        }
    }

    public class CommandSystemBridgeTests
    {
        private SequenceFactory _sequences;
        private RunnerBackedService _service;
        private List<bool> _results;

        [SetUp]
        public void SetUp()
        {
            TaskLog.Verbose = false;
            _sequences = new SequenceFactory();
            _service = new RunnerBackedService();
            _results = new List<bool>();
            Assert.IsTrue(ServiceRegistration.TryRegister<ITaskSequenceService>(_service));
        }

        [TearDown]
        public void TearDown()
        {
            _service.Runner.Abort();
            ServiceRegistration.Deregister<ITaskSequenceService>(_service);
            _sequences.DestroyAll();
            TaskLog.Verbose = true;
        }

        private void Record(bool success, IStepCommand command) => _results.Add(success);

        [Test]
        public void CompletedSequence_ReportsTrueOnce()
        {
            TaskSequenceStepCommand command = new TaskSequenceStepCommand(_sequences.Create("seq", new TaskStepData("a")));
            command.Execute(Record);
            _service.Runner.TickFrames(3);

            CollectionAssert.AreEqual(new[] { true }, _results);
        }

        [Test]
        public void Exit_AbortsRun_WithoutCallingComplete()
        {
            TaskSequenceStepCommand command = new TaskSequenceStepCommand(
                _sequences.Create("seq", new TaskStepData("a", new FakeValidator())));
            command.Execute(Record);

            command.Exit();

            Assert.AreEqual(TaskSequenceStatus.Aborted, _service.Runner.Status);
            Assert.IsEmpty(_results, "Complete must never be raised from inside Exit");
        }

        [Test]
        public void SequenceFinishingInsideRun_ReportsInsteadOfHanging()
        {
            TaskSequenceStepCommand command = new TaskSequenceStepCommand(_sequences.Create("seq",
                new TaskStepData("a", new FakeValidator(), "ghost", missingEntityPolicy: MissingEntityPolicy.SkipStep)));

            LogAssert.Expect(LogType.Error, new Regex("is not registered"));
            command.Execute(Record);

            CollectionAssert.AreEqual(new[] { false }, _results);
        }

        [Test]
        public void ReExecute_AbortsPreviousRun_AndReportsOnlyTheNewOne()
        {
            TaskSequenceStepCommand command = new TaskSequenceStepCommand(
                _sequences.Create("seq", new TaskStepData("a", new FakeValidator())));
            command.Execute(Record);

            command.Execute(Record);
            Assert.IsTrue(_service.Runner.IsRunning, "second run is live");
            Assert.IsEmpty(_results, "the aborted first run is not reported");

            _service.Runner.CompleteCurrentStep();
            CollectionAssert.AreEqual(new[] { true }, _results);
        }

        [Test]
        public void BusyService_ReportsFailure()
        {
            _service.Run(_sequences.Create("other", new TaskStepData("x", new FakeValidator())));
            TaskSequenceStepCommand command = new TaskSequenceStepCommand(_sequences.Create("seq", new TaskStepData("a")));

            LogAssert.Expect(LogType.Error, new Regex("still running"));
            command.Execute(Record);

            CollectionAssert.AreEqual(new[] { false }, _results);
            Assert.AreEqual("other", _service.CurrentSequenceId);
        }
    }

    public class ServiceRegistrationTests
    {
        private sealed class DummyService { }

        [Test]
        public void SecondLiveInstance_IsRejected_AndCannotDeregisterTheFirst()
        {
            DummyService first = new DummyService();
            DummyService second = new DummyService();
            try
            {
                Assert.IsTrue(ServiceRegistration.TryRegister(first));

                LogAssert.Expect(LogType.Error, new Regex("already registered"));
                Assert.IsFalse(ServiceRegistration.TryRegister(second));

                ServiceRegistration.Deregister(second);
                Assert.AreSame(first, ServiceLocator.Instance.RequestService<DummyService>());
            }
            finally
            {
                ServiceRegistration.Deregister(first);
            }
            Assert.IsNull(ServiceLocator.Instance.RequestService<DummyService>());
        }

        [Test]
        public void DestroyedUnityObject_IsTreatedAsStaleAndReplaced()
        {
            TestServiceObject stale = ScriptableObject.CreateInstance<TestServiceObject>();
            TestServiceObject fresh = ScriptableObject.CreateInstance<TestServiceObject>();
            try
            {
                Assert.IsTrue(ServiceRegistration.TryRegister(stale));
                Object.DestroyImmediate(stale);

                Assert.IsTrue(ServiceRegistration.TryRegister(fresh));
                Assert.AreSame(fresh, ServiceLocator.Instance.RequestService<TestServiceObject>());
            }
            finally
            {
                ServiceRegistration.Deregister(fresh);
                Object.DestroyImmediate(fresh);
            }
        }
    }

    public class TaskEntityRegistrationTests
    {
        private GameObject _go;
        private TaskEntity _entity;
        private readonly List<TaskContextService> _registries = new List<TaskContextService>();

        [SetUp]
        public void SetUp()
        {
            TaskLog.Verbose = false;
            _go = new GameObject("entity");
            _entity = _go.AddComponent<TaskEntity>();

            SerializedObject serialized = new SerializedObject(_entity);
            serialized.FindProperty("_entityId._value").stringValue = "tutorial.target";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            _entity.Unregister();
            foreach (TaskContextService registry in _registries)
                ServiceRegistration.Deregister(registry);
            _registries.Clear();
            Object.DestroyImmediate(_go);
            TaskLog.Verbose = true;
        }

        private TaskContextService MakeLiveRegistry()
        {
            foreach (TaskContextService previous in _registries)
                ServiceRegistration.Deregister(previous);

            TaskContextService registry = new TaskContextService();
            Assert.IsTrue(ServiceRegistration.TryRegister(registry));
            _registries.Add(registry);
            return registry;
        }

        [Test]
        public void ClearedRegistry_EntityRegistersAgain()
        {
            TaskContextService registry = MakeLiveRegistry();
            _entity.EnsureRegistered();
            Assert.IsTrue(registry.Contains(_entity));

            registry.Clear();
            _entity.EnsureRegistered();

            Assert.IsTrue(registry.Contains(_entity));
        }

        [Test]
        public void NewRegistry_EntityMovesToIt_AndUnregisterLeavesTheOneItJoined()
        {
            TaskContextService oldRegistry = MakeLiveRegistry();
            _entity.EnsureRegistered();

            TaskContextService newRegistry = MakeLiveRegistry();
            _entity.EnsureRegistered();
            Assert.IsTrue(newRegistry.Contains(_entity));

            _entity.Unregister();
            Assert.IsFalse(newRegistry.Contains(_entity));
            Assert.IsTrue(oldRegistry.Contains(_entity), "the old registry is not touched; its owner clears it");
        }

        [Test]
        public void EnsureRegistered_IsIdempotent()
        {
            TaskContextService registry = MakeLiveRegistry();
            _entity.EnsureRegistered();
            _entity.EnsureRegistered();

            Assert.AreEqual(1, registry.Count);
        }
    }
}
