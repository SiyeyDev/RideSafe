using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace RideSafe.TaskSequence
{
    /// <summary>
    /// The authored sequence. This is the whole point of the architecture: steps live in
    /// an asset, addressed by id, so the same sequence runs in any scene that registers
    /// the entity ids it names.
    /// <para>
    /// Uses Odin's <see cref="SerializedScriptableObject"/> so the polymorphic
    /// <see cref="ITaskValidator"/> on each step serializes without a wrapper type.
    /// </para>
    /// </summary>
    [CreateAssetMenu(fileName = "SO_TaskSequence", menuName = "RideSafe/Task Sequence", order = 0)]
    public class TaskSequenceSO : SerializedScriptableObject
    {
        [Tooltip("Global id, e.g. Initial.ControllerBasics or Module01.InteractionIntro.")]
        [SerializeField] private string _sequenceId;

        [Tooltip("Conditions the running context must meet. Empty means runs anywhere.")]
        [SerializeField] private List<ContextRequirement> _contextRequirements = new List<ContextRequirement>();

        [SerializeField] private RestartPolicy _restartPolicy = RestartPolicy.FromBeginning;
        [SerializeField] private SkipPolicy _skipPolicy = SkipPolicy.NotSkippable;

        [PropertySpace(SpaceBefore = 8)]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = false, ListElementLabelName = "GetEditorLabel")]
        [OdinSerialize] private List<TaskStepData> _steps = new List<TaskStepData>();

        public string SequenceId => string.IsNullOrWhiteSpace(_sequenceId) ? name : _sequenceId.Trim();
        public IReadOnlyList<TaskStepData> Steps => _steps;
        public IReadOnlyList<ContextRequirement> ContextRequirements => _contextRequirements;
        public RestartPolicy RestartPolicy => _restartPolicy;
        public SkipPolicy SkipPolicy => _skipPolicy;
        public int StepCount => _steps?.Count ?? 0;

        /// <summary>
        /// Checks requirements against a context lookup. The lookup is a delegate so the
        /// core never depends on the tutorial layer's context type.
        /// </summary>
        public bool MatchesContext(System.Func<string, string> contextLookup, out string firstUnmet)
        {
            firstUnmet = null;
            if (_contextRequirements == null || _contextRequirements.Count == 0)
                return true;
            if (contextLookup == null)
                return true;

            for (int i = 0; i < _contextRequirements.Count; i++)
            {
                ContextRequirement requirement = _contextRequirements[i];
                if (string.IsNullOrEmpty(requirement.Key))
                    continue;
                if (requirement.IsSatisfiedBy(contextLookup(requirement.Key)))
                    continue;
                firstUnmet = requirement.ToString();
                return false;
            }
            return true;
        }

        /// <summary>Authoring guard: duplicate step ids make logs ambiguous.</summary>
        public bool TryFindDuplicateStepId(out string duplicateId)
        {
            duplicateId = null;
            if (_steps == null)
                return false;

            HashSet<string> seen = new HashSet<string>();
            for (int i = 0; i < _steps.Count; i++)
            {
                if (_steps[i] == null)
                    continue;
                if (seen.Add(_steps[i].StepId))
                    continue;
                duplicateId = _steps[i].StepId;
                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (TryFindDuplicateStepId(out string duplicate))
                TaskLog.Warn(SequenceId, duplicate, "Duplicate StepId in this sequence. Logs will be ambiguous.", this);
        }
#endif
    }
}
