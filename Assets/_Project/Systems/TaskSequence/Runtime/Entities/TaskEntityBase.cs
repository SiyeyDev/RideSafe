using System;
using Cachacos;
using UnityEngine;

namespace RideSafe.TaskSequence
{
    /// <summary>
    /// Base for anything a sequence can address. Subclasses describe a TYPE of
    /// interaction (selectable, target, world-space button), never a piece of content.
    /// <para>
    /// Registration is idempotent and happens from three directions so that objects which
    /// start disabled still resolve: <see cref="Awake"/> for active objects, an explicit
    /// <see cref="EnsureRegistered"/> call from <c>TaskEntitySceneRegistrar</c> for
    /// inactive ones, and <see cref="OnEnable"/> as a late safety net.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class TaskEntityBase : MonoBehaviour, ITaskEntity
    {
        [Tooltip("Scene-agnostic id referenced by sequence assets, e.g. tutorial.target.circle")]
        [SerializeField] private EntityId _entityId;

        [Tooltip("Uncheck to keep the entity registered but unavailable to steps.")]
        [SerializeField] private bool _availableOnStart = true;

        private bool _registered;
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
            if (_registered)
                return;

            TaskContextService registry = ServiceLocator.Instance.RequestService<TaskContextService>();
            if (registry == null)
            {
                // No registry yet. The scene registrar (or the tutorial session) will call
                // back into us once one exists, so this is not an error.
                return;
            }

            _registered = registry.RegisterEntity(this);
        }

        public void Unregister()
        {
            if (!_registered)
                return;

            TaskContextService registry = ServiceLocator.Instance.RequestService<TaskContextService>();
            registry?.UnregisterEntity(this);
            _registered = false;
        }

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
            if (!_entityId.IsValid)
                TaskLog.Warn(null, null, $"'{name}' has a {GetType().Name} with no EntityId set.", this);
        }
#endif
    }
}
