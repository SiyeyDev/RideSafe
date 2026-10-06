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
        [Tooltip("Seconds before the step fails. 0 disables the timeout.")]
        [SerializeField, Min(0f)] private float _timeout;

        [BoxGroup("Validation")]
        [Tooltip("What a failed step does. Retry re-arms it (timeouts only; a broken validator falls back to Skip).")]
        [SerializeField] private StepFailurePolicy _failurePolicy = StepFailurePolicy.Retry;

        [BoxGroup("Validation")]
        [SerializeField] private MissingEntityPolicy _missingEntityPolicy = MissingEntityPolicy.SkipStep;

        [BoxGroup("Validation")]
        [SerializeField] private SkipPolicy _skipPolicy = SkipPolicy.SkipStep;

        [BoxGroup("Assistance")]
        [Tooltip("Abstract action shown as the level-2 input hint, e.g. primaryselect. Empty = no input hint. Opaque to the core.")]
        [SerializeField] private string _hintActionId;

        public TaskStepData() { }

        /// <summary>Code construction, used by EditMode tests. Authoring goes through the inspector.</summary>
        internal TaskStepData(
            string stepId,
            ITaskValidator validator = null,
            string entityId = null,
            StepCompletionMode completionMode = StepCompletionMode.Immediate,
            float feedbackDuration = 0f,
            float timeout = 0f,
            StepFailurePolicy failurePolicy = StepFailurePolicy.Retry,
            MissingEntityPolicy missingEntityPolicy = MissingEntityPolicy.SkipStep,
            SkipPolicy skipPolicy = SkipPolicy.SkipStep,
            string instructionKey = null,
            string hintActionId = null)
        {
            _instructionKey = instructionKey;
            _hintActionId = hintActionId;
            _stepId = stepId;
            _validator = validator;
            _entityId = new EntityId(entityId);
            _completionMode = completionMode;
            _feedbackDuration = feedbackDuration;
            _timeout = timeout;
            _failurePolicy = failurePolicy;
            _missingEntityPolicy = missingEntityPolicy;
            _skipPolicy = skipPolicy;
        }

        public string StepId => string.IsNullOrWhiteSpace(_stepId) ? "step" : _stepId.Trim();
        public EntityId EntityId => _entityId;
        public string InstructionKey => _instructionKey;
        public string AudioKey => _audioKey;
        public ITaskValidator Validator => _validator;
        public StepCompletionMode CompletionMode => _completionMode;
        public float FeedbackDuration => Mathf.Max(0f, _feedbackDuration);
        public float Timeout => Mathf.Max(0f, _timeout);
        public StepFailurePolicy FailurePolicy => _failurePolicy;
        public MissingEntityPolicy MissingEntityPolicy => _missingEntityPolicy;
        public SkipPolicy SkipPolicy => _skipPolicy;
        public string HintActionId => _hintActionId;

        /// <summary>Row label in the sequence's step list. Resolved by Odin.</summary>
        public string GetEditorLabel()
        {
            string validator = _validator == null ? "no validator" : _validator.GetType().Name;
            string entity = _entityId.IsValid ? _entityId.Value : "no entity";
            return $"{StepId}  ({validator} | {entity})";
        }
    }
}
