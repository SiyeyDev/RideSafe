using System;
using Cachacos;
using UnityEngine;

namespace RideSafe.TaskSequence
{
    /// <summary>
    /// Anything a sequence can address by id. Used as-is for plain targets (something to
    /// look at); subclasses add a TYPE of interaction (focus, select, confirm), never a
    /// piece of content.
    /// <para>
    /// Registration is idempotent: <see cref="Awake"/> for active objects,
    /// <see cref="OnEnable"/> as a late safety net, and TaskSequenceService sweeps every
    /// loaded entity (inactive included) when it wakes up.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("RideSafe/Task Entity")]
    public class TaskEntity : MonoBehaviour, ITaskEntity
    {
        [Tooltip("Scene-agnostic id referenced by sequence assets, e.g. tutorial.target.circle")]
        [SerializeField] private EntityId _entityId;

        [Tooltip("Uncheck to keep the entity registered but unavailable to steps.")]
        [SerializeField] private bool _availableOnStart = true;

        // The registry this entity is in. Compared against the live one so an entity that
        // outlives its registry (scene reload, service recreated) registers again.
        private TaskContextService _registry;
        private bool _available;

        public EntityId Id => _entityId;
        public Transform Transform => transform;

        public virtual bool IsAvailable => _available && isActiveAndEnabled;

        /// <summary>Raised when availability flips, so presentation can drop a highlight.</summary>
        public event Action<ITaskEntity, bool> AvailabilityChanged;

        protected virtual void Awake()
        {
            _available = _availableOnStart;
            EnsureRegistered();
        }

        protected virtual void OnEnable() => EnsureRegistered();

        protected virtual void OnDestroy() => Unregister();

        /// <summary>
        /// Registers against the active registry if it has not happened yet. Safe to call
        /// any number of times and from any lifecycle point.
        /// </summary>
        public void EnsureRegistered()
        {
            TaskContextService registry = ServiceLocator.Instance.RequestService<TaskContextService>();
            if (registry == null)
            {
                // No registry yet. TaskSequenceService sweeps every entity when it wakes up,
                // so this is not an error.
                return;
            }

            // Already in the live registry. A registry that was Clear()ed no longer
            // contains us, so we fall through and register again.
            if (ReferenceEquals(registry, _registry) && registry.Contains(this))
                return;

            _registry = registry.RegisterEntity(this) ? registry : null;
        }

        public void Unregister()
        {
            // Leave the registry we actually joined, not whichever one is live now.
            if (_registry != null)
                _registry.UnregisterEntity(this);
            _registry = null;
        }

        /// <summary>
        /// For entities added at runtime by code (not authored in the scene). Must run before
        /// the entity registers, i.e. while its GameObject is still inactive.
        /// </summary>
        protected void SetEntityId(EntityId id) => _entityId = id;

        public void SetAvailable(bool value)
        {
            if (_available == value)
                return;
            _available = value;
            AvailabilityChanged?.Invoke(this, IsAvailable);
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            // Authoring check only: entities added by code at runtime get their id right after.
            if (Application.isPlaying)
                return;
            if (!_entityId.IsValid)
                TaskLog.Warn(null, null, $"'{name}' has a {GetType().Name} with no EntityId set.", this);
        }
#endif
    }
}
