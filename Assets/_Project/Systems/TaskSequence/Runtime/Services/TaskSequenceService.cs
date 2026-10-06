using UnityEngine;

namespace RideSafe.TaskSequence
{
    /// <summary>
    /// Scene-side host: owns the entity registry, ticks the runner and publishes itself
    /// as <see cref="ITaskSequenceService"/>.
    /// <para>
    /// Registration is instance-safe and undone in <see cref="OnDestroy"/>. That matters
    /// because <c>ServiceLocator</c> is a plain static singleton with no scene awareness:
    /// a service that never deregisters would be handed to the next scene as a destroyed
    /// object (CASE 09), and a type-wide deregister would remove a newer instance.
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
        private System.Func<string, string> _contextLookup;
        private bool _registered;

        /// <summary>
        /// Supplies context values to sequence requirements. The tutorial layer assigns
        /// this so the core never references TutorialContext. Can be set before or after Awake.
        /// </summary>
        public System.Func<string, string> ContextLookup
        {
            get { return _contextLookup; }
            set
            {
                _contextLookup = value;
                if (_runner != null)
                    _runner.ContextLookup = value;
            }
        }

        public TaskContextService Entities => _entities;
        public bool IsRunning => _runner != null && _runner.IsRunning;
        public string CurrentSequenceId => _runner != null ? _runner.SequenceId : null;

        /// <summary>Raw runner, for presentation.</summary>
        public TaskSequenceRunner Runner => _runner;

        protected virtual void Awake()
        {
            TaskLog.Verbose = _verboseLogging;

            TaskContextService entities = new TaskContextService();
            if (!ServiceRegistration.TryRegister<ITaskSequenceService>(this, this))
            {
                enabled = false;
                return;
            }
            if (!ServiceRegistration.TryRegister(entities, this))
            {
                ServiceRegistration.Deregister<ITaskSequenceService>(this);
                enabled = false;
                return;
            }

            _registered = true;
            _entities = entities;
            _runner = new TaskSequenceRunner(_entities) { ContextLookup = _contextLookup };

            // Entities that woke up before this registry existed (or registered with a
            // previous one) could not register here, so sweep the loaded scenes once.
            SweepEntities();
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
                ServiceRegistration.Deregister(_entities);
                _entities = null;
            }

            if (_registered)
                ServiceRegistration.Deregister<ITaskSequenceService>(this);
            _registered = false;
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
                TaskLog.Error(sequence != null ? sequence.SequenceId : null, null,
                    "TaskSequenceService is not initialized.", this);
                return null;
            }
            if (sequence == null)
            {
                TaskLog.Error(null, null, "Run called with a null sequence.", this);
                return null;
            }
            if (_runner.IsRunning)
            {
                TaskLog.Error(sequence.SequenceId, null,
                    "Refusing to start: '" + _runner.SequenceId + "' is still running.", this);
                return null;
            }
            return TaskSequenceHandle.Start(_runner, sequence);
        }

        public TaskSequenceHandle RunById(string sequenceId)
        {
            if (string.IsNullOrWhiteSpace(sequenceId))
            {
                TaskLog.Error(null, null, "RunById called with an empty id.", this);
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

        private static void SweepEntities()
        {
            TaskEntity[] entities = Object.FindObjectsByType<TaskEntity>(FindObjectsInactive.Include);
            for (int i = 0; i < entities.Length; i++)
            {
                if (entities[i] != null)
                    entities[i].EnsureRegistered();
            }
        }
    }
}
