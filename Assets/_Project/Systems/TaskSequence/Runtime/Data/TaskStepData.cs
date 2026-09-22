using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace RideSafe.TaskSequence
{
    /// <summary>
    /// Authored definition of one step. Pure data: no scene references, no MonoBehaviour,
    /// no module knowledge. The only link to the world is <see cref="EntityId"/>.
    /// </summary>
    [System.Serializable]
    [InlineProperty]
    public class TaskStepData
    {
        [BoxGroup("Step")]
        [Tooltip("Unique within the sequence. Shows up in every log line for this step.")]
        [SerializeField] private string _stepId = "step";

        [BoxGroup("Step")]
        [Tooltip("Entity this step acts on. Leave empty for input-only steps.")]
        [SerializeField] private EntityId _entityId;

        [BoxGroup("Content")]
        [Tooltip("Localization key. Never put literal text here.")]
        [SerializeField] private string _instructionKey;

        [BoxGroup("Content")]
        [Tooltip("Audio key, resolved independently from the instruction key.")]
        [SerializeField] private string _audioKey;

        [BoxGroup("Validation")]
        [Tooltip("What must happen for this step to pass.")]
        [HideReferenceObjectPicker]
        [OdinSerialize] private ITaskValidator _validator;

        [BoxGroup("Validation")]
        [SerializeField] private StepCompletionMode _completionMode = StepCompletionMode.Immediate;

        [BoxGroup("Validation")]
        [Tooltip("Seconds held on Feedback before advancing. Used by AfterFeedback.")]
        [SerializeField, Min(0f)] private float _feedbackDuration = 0.5f;

        [BoxGroup("Validation")]
        [Tooltip("Seconds before the step gives up. 0 disables the timeout.")]
        [SerializeField, Min(0f)] private float _timeout;

        [BoxGroup("Validation")]
        [SerializeField] private MissingEntityPolicy _missingEntityPolicy = MissingEntityPolicy.SkipStep;

        [BoxGroup("Validation")]
        [SerializeField] private SkipPolicy _skipPolicy = SkipPolicy.SkipStep;

        [BoxGroup("Assistance")]
        [Tooltip("Opaque to the core. Phase 3 hint ladder reads this key to pick a policy.")]
        [SerializeField] private string _hintPolicyKey;

        [BoxGroup("Assistance")]
        [Tooltip("Opaque to the core. Presentation layer decides what this means.")]
        [SerializeField] private string _visualCueKey;

        public string StepId => string.IsNullOrWhiteSpace(_stepId) ? "step" : _stepId.Trim();
        public EntityId EntityId => _entityId;
        public string InstructionKey => _instructionKey;
        public string AudioKey => _audioKey;
        public ITaskValidator Validator => _validator;
        public StepCompletionMode CompletionMode => _completionMode;
        public float FeedbackDuration => Mathf.Max(0f, _feedbackDuration);
        public float Timeout => Mathf.Max(0f, _timeout);
        public MissingEntityPolicy MissingEntityPolicy => _missingEntityPolicy;
        public SkipPolicy SkipPolicy => _skipPolicy;
        public string HintPolicyKey => _hintPolicyKey;
        public string VisualCueKey => _visualCueKey;

        /// <summary>Row label in the sequence's step list. Resolved by Odin.</summary>
        public string GetEditorLabel()
        {
            string validator = _validator == null ? "no validator" : _validator.GetType().Name;
            string entity = _entityId.IsValid ? _entityId.Value : "no entity";
            return $"{StepId}  ({validator} | {entity})";
        }
    }
}
