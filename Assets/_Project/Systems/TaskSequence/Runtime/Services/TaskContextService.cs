using System;
using System.Collections.Generic;
using UnityEngine;

namespace RideSafe.TaskSequence
{
    /// <summary>
    /// The EntityId registry. This is the indirection that keeps sequence assets free of
    /// scene references: assets name ids, scenes register objects under those ids.
    /// <para>
    /// Owned by a <c>TutorialSessionScope</c> (or the host component) so it is disposed
    /// deterministically instead of surviving a scene change inside the ServiceLocator.
    /// </para>
    /// </summary>
    public sealed class TaskContextService
    {
        private readonly Dictionary<EntityId, ITaskEntity> _entities = new Dictionary<EntityId, ITaskEntity>();

        public event Action<ITaskEntity> EntityRegistered;
        public event Action<ITaskEntity> EntityUnregistered;

        public int Count => _entities.Count;
        public IEnumerable<EntityId> RegisteredIds => _entities.Keys;

        /// <summary>
        /// Registers an entity. Duplicate ids are rejected with a clear error naming both
        /// objects (CASE 03) rather than silently overwriting, which would make the
        /// winning object depend on scene load order.
        /// </summary>
        public bool RegisterEntity(ITaskEntity entity)
        {
            if (entity == null)
            {
                TaskLog.Error(null, null, "RegisterEntity called with null.");
                return false;
            }
            if (!entity.Id.IsValid)
            {
                TaskLog.Error(null, null,
                    $"Entity on '{Describe(entity)}' has an empty EntityId and cannot be registered.",
                    entity.Transform);
                return false;
            }
            if (_entities.TryGetValue(entity.Id, out ITaskEntity existing))
            {
                if (ReferenceEquals(existing, entity))
                    return true;

                TaskLog.Error(null, null,
                    $"DUPLICATE EntityId '{entity.Id}'. Already registered by '{Describe(existing)}', " +
                    $"rejected '{Describe(entity)}'. Ids must be unique per loaded scene set.",
                    entity.Transform);
                return false;
            }

            _entities.Add(entity.Id, entity);
            EntityRegistered?.Invoke(entity);
            return true;
        }

        /// <summary>Removes the entity only if it still owns the id (avoids stealing).</summary>
        public bool UnregisterEntity(ITaskEntity entity)
        {
            if (entity == null || !entity.Id.IsValid)
                return false;
            if (!_entities.TryGetValue(entity.Id, out ITaskEntity existing) || !ReferenceEquals(existing, entity))
                return false;

            _entities.Remove(entity.Id);
            EntityUnregistered?.Invoke(entity);
            return true;
        }

        public bool UnregisterEntity(EntityId id)
        {
            if (!id.IsValid || !_entities.TryGetValue(id, out ITaskEntity existing))
                return false;

            _entities.Remove(id);
            EntityUnregistered?.Invoke(existing);
            return true;
        }

        public bool TryGetEntity(EntityId id, out ITaskEntity entity)
        {
            entity = null;
            return id.IsValid && _entities.TryGetValue(id, out entity) && entity != null;
        }

        public bool TryGetEntity<T>(EntityId id, out T entity) where T : class, ITaskEntity
        {
            entity = null;
            if (!TryGetEntity(id, out ITaskEntity raw))
                return false;
            entity = raw as T;
            return entity != null;
        }

        /// <summary>Every registered id, newline separated. Used by the missing-id error.</summary>
        public string DumpIds()
        {
            if (_entities.Count == 0)
                return "(no entities registered)";

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            foreach (KeyValuePair<EntityId, ITaskEntity> pair in _entities)
                builder.AppendLine($"  - {pair.Key}  ->  {Describe(pair.Value)}");
            return builder.ToString();
        }

        public void Clear()
        {
            if (_entities.Count > 0)
                TaskLog.Info(null, null, $"Clearing entity registry ({_entities.Count} entries).");
            _entities.Clear();
            EntityRegistered = null;
            EntityUnregistered = null;
        }

        private static string Describe(ITaskEntity entity)
        {
            if (entity == null)
                return "<null>";
            Transform transform = entity.Transform;
            return transform == null ? entity.GetType().Name : transform.name;
        }
    }
}
