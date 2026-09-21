using Cachacos;
using UnityEngine;

namespace RideSafe.TaskSequence
{
    /// <summary>
    /// Scene-side host: owns the entity registry, ticks the runner and publishes itself
    /// as <see cref="ITaskSequenceService"/>.
    /// <para>
    /// Registration is undone in <see cref="OnDestroy"/>. That matters because
    /// <c>ServiceLocator</c> is a plain static singleton with no scene awareness, so a
    /// service that never deregisters would be handed to the next scene as a destroyed
    /// object (CASE 09).
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [AddComponentMenu("RideSafe/Task Sequence Service")]
    public class TaskSequenceService : MonoBehaviour, ITaskSequenceService
    {
        [Tooltip("Catalog used to resolve sequences by string id.")]
        [SerializeField] private TaskSequenceCatalogSO _catalog;

        [Tooltip("Log every phase transition. Noisy, but the fastest way to debug a sequence.")]
        [SerializeField] private bool _verboseLogging = true;

        private TaskContextService _entities;
        private TaskSequenceRunner _runner;
        private TaskSequenceHandle _current;

        /// <summary>
        /// Supplies context values to sequence requirements. The tutorial layer assigns
        /// this so the core never references TutorialContext.
        /// </summary>
        public System.Func<string, string> ContextLookup { get; set; }

        public TaskContextService Entities => _entities;
        public bool IsRunning => _runner != null && _runner.IsRunning;
        public string CurrentSequenceId => _runner != null ? _runner.SequenceId : null;

        /// <summary>Raw runner, for presentation and hint systems added in Phase 3.</summary>
        public TaskSequenceRunner Runner => _runner;

        protected virtual void Awake()
        {
            TaskLog.Verbose = _verboseLogging;

            _entities = new TaskContextService();
            _runner = new TaskSequenceRunner(_entities, ResolveContextValue);

            ServiceLocator.Instance.RegisterService<TaskContextService>(_entities);
            ServiceLocator.Instance.RegisterService<ITaskSequenceService>(this);

            // Entities that woke up before this service existed could not register, so
            // sweep the loaded scenes once the registry is available.
            SweepSceneRegistrars();
        }

        protected virtual void OnDestroy()
        {
            if (_runner != null)
            {
                _runner.Abort("service destroyed");
                _runner.ClearSubscribers();
                _runner = null;
            }

            if (_entities != null)
            {
                _entities.Clear();
                ServiceLocator.Instance.TryDeregisterService<TaskContextService>(_entities);
                _entities = null;
            }

            ServiceLocator.Instance.TryDeregisterService<ITaskSequenceService>(this);
            _current = null;
        }

        protected virtual void Update()
        {
            if (_runner != null)
                _runner.Tick(Time.deltaTime);
        }

        #region ITaskSequenceService

        public TaskSequenceHandle Run(TaskSequenceSO sequence)
        {
            if (_runner == null)
            {
                TaskLog.Error(null, null, "TaskSequenceService is not initialized.");
                return null;
            }
            if (_runner.IsRunning)
            {
                TaskLog.Error(sequence != null ? sequence.SequenceId : null, null,
                    "Refusing to start: '" + _runner.SequenceId + "' is still running.");
                return null;
            }
            if (!_runner.Start(sequence))
                return null;

            _current = new TaskSequenceHandle(_runner);
            return _current;
        }

        public TaskSequenceHandle RunById(string sequenceId)
        {
            if (string.IsNullOrWhiteSpace(sequenceId))
            {
                TaskLog.Error(null, null, "RunById called with an empty id.");
                return null;
            }
            if (_catalog == null)
            {
                TaskLog.Error(sequenceId, null,
                    "No TaskSequenceCatalog assigned on '" + name + "'; cannot resolve sequences by id.", this);
                return null;
            }

            TaskSequenceSO sequence;
            if (!_catalog.TryGet(sequenceId, out sequence))
            {
                TaskLog.Error(sequenceId, null,
                    "SequenceId not found in catalog '" + _catalog.name + "'. Add the asset to the catalog.", this);
                return null;
            }
            return Run(sequence);
        }

        public void AbortCurrent(string reason = null)
        {
            if (_runner != null)
                _runner.Abort(reason);
        }

        #endregion

        private string ResolveContextValue(string key)
        {
            System.Func<string, string> lookup = ContextLookup;
            return lookup == null ? null : lookup(key);
        }

        private void SweepSceneRegistrars()
        {
#if UNITY_2023_1_OR_NEWER
            TaskEntitySceneRegistrar[] registrars =
                Object.FindObjectsByType<TaskEntitySceneRegistrar>(FindObjectsInactive.Include);
#else
            TaskEntitySceneRegistrar[] registrars = Object.FindObjectsOfType<TaskEntitySceneRegistrar>(true);
#endif
            for (int i = 0; i < registrars.Length; i++)
            {
                if (registrars[i] != null)
                    registrars[i].Scan();
            }
        }
    }
}
