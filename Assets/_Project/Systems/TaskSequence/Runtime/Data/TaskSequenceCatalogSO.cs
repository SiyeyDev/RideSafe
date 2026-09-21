using System.Collections.Generic;
using UnityEngine;

namespace RideSafe.TaskSequence
{
    /// <summary>
    /// Maps SequenceId -> asset so callers can start a sequence by string, which is what
    /// lets a module bridge raise "start Module01.InteractionIntro" without holding a
    /// reference to the tutorial system.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_TaskSequenceCatalog", menuName = "RideSafe/Task Sequence Catalog", order = 1)]
    public class TaskSequenceCatalogSO : ScriptableObject
    {
        [SerializeField] private List<TaskSequenceSO> _sequences = new List<TaskSequenceSO>();

        private Dictionary<string, TaskSequenceSO> _lookup;

        public IReadOnlyList<TaskSequenceSO> Sequences => _sequences;

        public bool TryGet(string sequenceId, out TaskSequenceSO sequence)
        {
            sequence = null;
            if (string.IsNullOrWhiteSpace(sequenceId))
                return false;

            BuildLookup();
            return _lookup.TryGetValue(Key(sequenceId), out sequence) && sequence != null;
        }

        /// <summary>Rebuild after editing the list at runtime.</summary>
        public void Invalidate() => _lookup = null;

        private void BuildLookup()
        {
            if (_lookup != null)
                return;

            _lookup = new Dictionary<string, TaskSequenceSO>();
            if (_sequences == null)
                return;

            for (int i = 0; i < _sequences.Count; i++)
            {
                TaskSequenceSO sequence = _sequences[i];
                if (sequence == null)
                    continue;

                string key = Key(sequence.SequenceId);
                if (_lookup.ContainsKey(key))
                {
                    TaskLog.Error(sequence.SequenceId, null,
                        $"Duplicate SequenceId in catalog '{name}'. Only the first asset will ever resolve.", this);
                    continue;
                }
                _lookup.Add(key, sequence);
            }
        }

        private static string Key(string id) => id.Trim().ToLowerInvariant();
    }
}
