using System.Collections.Generic;
using UnityEngine;

namespace RideSafe.TaskSequence.Tests
{
    /// <summary>Scriptable validator: tests flip its flags to drive the runner.</summary>
    public sealed class FakeValidator : ITaskValidator
    {
        public bool Satisfied;
        public bool BreakOnPrepare;
        public bool ThrowOnPrepare;
        public bool ThrowOnEvaluate;

        public int PrepareCount { get; private set; }
        public int EvaluateCount { get; private set; }
        public int CleanupCount { get; private set; }
        public TaskStepContext LastContext { get; private set; }

        public bool IsBroken { get; set; }

        public void Prepare(TaskStepContext context)
        {
            PrepareCount++;
            LastContext = context;
            Satisfied = false;
            IsBroken = BreakOnPrepare;
            if (ThrowOnPrepare)
                throw new System.InvalidOperationException("prepare boom");
        }

        public bool Evaluate(float deltaTime)
        {
            EvaluateCount++;
            if (ThrowOnEvaluate)
                throw new System.InvalidOperationException("evaluate boom");
            return Satisfied;
        }

        public void Cleanup()
        {
            CleanupCount++;
            LastContext = null;
        }

        public string Describe() => "fake";
    }

    /// <summary>Plain entity without a GameObject.</summary>
    public sealed class FakeEntity : ITaskEntity
    {
        public FakeEntity(string id) => Id = new EntityId(id);
        public EntityId Id { get; }
        public Transform Transform => null;
        public bool IsAvailable => true;
    }

    /// <summary>Builds sequences in code and destroys them on <see cref="DestroyAll"/>.</summary>
    public sealed class SequenceFactory
    {
        private readonly List<TaskSequenceSO> _created = new List<TaskSequenceSO>();

        public TaskSequenceSO Create(string id, params TaskStepData[] steps) =>
            Create(id, RestartPolicy.FromBeginning, SkipPolicy.NotSkippable, null, steps);

        public TaskSequenceSO Create(string id, RestartPolicy restart, SkipPolicy skip,
            IEnumerable<ContextRequirement> requirements, params TaskStepData[] steps)
        {
            TaskSequenceSO sequence = ScriptableObject.CreateInstance<TaskSequenceSO>();
            sequence.Configure(id, steps, restart, skip, requirements);
            _created.Add(sequence);
            return sequence;
        }

        public void DestroyAll()
        {
            for (int i = 0; i < _created.Count; i++)
            {
                if (_created[i] != null)
                    Object.DestroyImmediate(_created[i]);
            }
            _created.Clear();
        }
    }

    public static class RunnerTestExtensions
    {
        /// <summary>Advances the runner <paramref name="frames"/> times.</summary>
        public static void TickFrames(this TaskSequenceRunner runner, int frames, float deltaTime = 0.1f)
        {
            for (int i = 0; i < frames; i++)
                runner.Tick(deltaTime);
        }

        /// <summary>Ticks until the step leaves Present and is actually waiting.</summary>
        public static void TickUntilWaiting(this TaskSequenceRunner runner)
        {
            for (int i = 0; i < 5 && runner.Phase != TaskStepPhase.WaitForInteraction; i++)
                runner.Tick(0.1f);
        }
    }
}
