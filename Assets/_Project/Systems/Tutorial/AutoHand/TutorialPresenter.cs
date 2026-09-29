using Autohand;
using Autohand.Demo;
using Cachacos;
using RideSafe.TaskSequence;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.Tutorial.AutoHand
{
    /// <summary>
    /// Small in-world presentation for Sprint 1: instruction, target feedback, controller
    /// hint, haptic/audio feedback and Clear. Driven by the runner's step events.
    /// <para>
    /// <b>First use</b> (the step's <see cref="TaskStepData.HintActionId"/> is not learned yet):
    /// instruction, target highlight and the animated controller hint all appear at once;
    /// when the step succeeds the action is marked learned in <see cref="TutorialProgress"/>.
    /// </para>
    /// <para>
    /// <b>Reminder</b> (already learned, or a step without an action): instruction at once,
    /// highlight after <c>_highlightAfter</c> seconds of waiting, controller hint after
    /// <c>_inputHintAfter</c>.
    /// </para>
    /// <para>
    /// Every piece of help asks <see cref="TutorialAssistanceGuard"/> first. World feedback
    /// (the target reacting, the pressed button, haptic, sound) is the result of the
    /// learner's own action, not help, so it is not gated.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/Tutorial/Tutorial Presenter")]
    [RequireComponent(typeof(AudioSource))]
    public class TutorialPresenter : MonoBehaviour
    {
        [SerializeField] private TaskSequenceService _service;
        [SerializeField] private AutoHandTutorialInputService _input;

        [Tooltip("XRPlayer's head camera. Highlight and controller hint face it.")]
        [SerializeField] private Transform _viewer;

        [Header("Views")]
        [Tooltip("Short instruction at a comfortable fixed spot in the garage.")]
        [SerializeField] private TMP_Text _instructionLabel;

        [Tooltip("Ring drawn around the target. Local-space circle of radius 1.")]
        [SerializeField] private LineRenderer _highlight;

        [SerializeField] private ControllerHintView _controllerHint;

        [Header("Reminder ladder (seconds waiting, learned actions)")]
        [SerializeField, Min(0f)] private float _highlightAfter = 2.75f;
        [SerializeField, Min(0f)] private float _inputHintAfter = 5f;

        [Header("Feedback")]
        [SerializeField] private float _focusScale = 1.06f;
        [SerializeField] private float _popScale = 1.14f;
        [SerializeField, Min(0.05f)] private float _popSeconds = 0.3f;
        [Tooltip("Short confirmation sound. A soft tick is generated when empty.")]
        [SerializeField] private AudioClip _successClip;

        [Header("Look")]
        [SerializeField, Min(0.01f)] private float _minHighlightRadius = 0.08f;

        private TaskSequenceRunner _runner;
        private TutorialAssistanceGuard _guard;
        private ILocalizationProvider _localization;
        private AudioSource _audio;

        // Current step
        private TaskStepData _step;
        private ITaskEntity _entity;
        private ActionId _action;
        private bool _firstUse;
        private bool _reacted;
        private float _waited;
        private int _level = -1;

        // Views state
        private ITaskEntity _highlighted;
        private Transform _hintAnchor;
        private bool _hintAnchoredToHand;

        // Target feedback (outlives the step so the pop can finish)
        private Transform _feedbackTarget;
        private Vector3 _feedbackBaseScale;
        private bool _focused;
        private float _popTime = -1f;

        #region Lifecycle

        protected virtual void Awake()
        {
            _audio = GetComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            if (_successClip == null)
                _successClip = CreateTick();
        }

        protected virtual void OnEnable()
        {
            _runner = _service != null ? _service.Runner : null;
            if (_runner == null)
            {
                Debug.LogError("[Tutorial] TutorialPresenter needs an initialized TaskSequenceService.", this);
                enabled = false;
                return;
            }

            _runner.StepStarted += HandleStepStarted;
            _runner.StepCompleted += HandleStepCompleted;
            _runner.SequenceFinished += HandleSequenceFinished;

            _localization = ServiceLocator.Instance.RequestService<ILocalizationProvider>();
            if (_localization != null)
                _localization.LanguageChanged += HandleLanguageChanged;

            HideAll();
        }

        protected virtual void OnDisable()
        {
            if (_runner != null)
            {
                _runner.StepStarted -= HandleStepStarted;
                _runner.StepCompleted -= HandleStepCompleted;
                _runner.SequenceFinished -= HandleSequenceFinished;
            }
            if (_localization != null)
                _localization.LanguageChanged -= HandleLanguageChanged;
            _runner = null;
            _localization = null;

            UnbindEntity();
            SettleFeedbackTarget();
            ReleaseGuard();
            _step = null;
            HideAll();
        }

        protected virtual void Update()
        {
            if (_step == null || _firstUse || _runner == null || _runner.Phase != TaskStepPhase.WaitForInteraction)
                return;

            _waited += Time.deltaTime;
            int level = _waited >= _inputHintAfter ? 2 : (_waited >= _highlightAfter ? 1 : 0);
            if (level > _level)
            {
                _level = level;
                Refresh();
            }
        }

        protected virtual void LateUpdate()
        {
            if (_highlighted != null && _highlighted.Transform != null)
            {
                Vector3 center;
                Vector2 half;
                GetExtents(_highlighted.Transform, out center, out half);
                Transform ring = _highlight.transform;
                ring.position = center;
                FaceViewer(ring);
                float pulse = 1f + 0.06f * Mathf.Sin(Time.time * 4f);
                ring.localScale = new Vector3(half.x * pulse, half.y * pulse, 1f);
            }

            if (_hintAnchor != null && _controllerHint.IsVisible)
                PlaceControllerHint();

            AnimateFeedbackTarget();
        }

        #endregion

        #region Runner events

        private void HandleStepStarted(TaskSequenceRunner runner, TaskStepData step)
        {
            UnbindEntity();
            Clear();

            _step = step;
            _entity = ResolveEntity(step);
            _action = new ActionId(step.HintActionId);
            TutorialProgress progress = ServiceLocator.Instance.RequestService<TutorialProgress>();
            _firstUse = _action.IsValid && (progress == null || !progress.IsActionLearned(_action));
            _reacted = false;
            _waited = 0f;
            _level = _firstUse ? 2 : 0;

            BindEntity(_entity);
            Refresh();
        }

        private void HandleStepCompleted(TaskSequenceRunner runner, TaskStepData step, bool success)
        {
            if (step != _step)
                return;

            if (success)
            {
                if (!_reacted)
                    React();
                if (_firstUse)
                {
                    TutorialProgress progress = ServiceLocator.Instance.RequestService<TutorialProgress>();
                    if (progress != null)
                        progress.MarkActionLearned(_action);
                }
            }

            UnbindEntity();
            Clear();
            _step = null;
        }

        private void HandleSequenceFinished(TaskSequenceRunner runner, TaskSequenceStatus status)
        {
            UnbindEntity();
            _step = null;
            Clear();
        }

        private void HandleLanguageChanged()
        {
            if (_step != null && _instructionLabel != null && _instructionLabel.gameObject.activeSelf)
                ShowInstruction(_step.InstructionKey);
        }

        #endregion

        #region Presentation API

        public void ShowInstruction(string localizationKey)
        {
            if (!AssistanceAllowed() || _instructionLabel == null || string.IsNullOrEmpty(localizationKey))
                return;
            _instructionLabel.text = Translate(localizationKey);
            _instructionLabel.gameObject.SetActive(true);
        }

        public void Highlight(ITaskEntity entity)
        {
            if (!AssistanceAllowed() || _highlight == null || entity == null || entity.Transform == null)
                return;
            _highlighted = entity;
            _highlight.gameObject.SetActive(true);
        }

        /// <summary>
        /// Shows the controller with the button bound to <paramref name="action"/>. Next to the
        /// step's target when there is one (where the learner is looking), otherwise over the hand.
        /// </summary>
        public void ShowInputHint(ActionId action)
        {
            if (!AssistanceAllowed() || _controllerHint == null || _input == null || !action.IsValid)
                return;

            HandControllerLink link;
            CommonButton button;
            if (!_input.TryGetPhysicalBinding(action, out link, out button) || link.hand == null)
                return;
            if (!_controllerHint.Show(button, link.hand.left))
                return;

            _hintAnchoredToHand = _entity == null || _entity.Transform == null;
            _hintAnchor = _hintAnchoredToHand ? link.hand.transform : _entity.Transform;
            PlaceControllerHint();
        }

        /// <summary>Removes instruction, highlight and hint. A hint already playing its pressed pose finishes on its own.</summary>
        public void Clear()
        {
            _highlighted = null;
            if (_highlight != null)
                _highlight.gameObject.SetActive(false);

            if (_controllerHint != null && !_controllerHint.IsReleasing)
                _controllerHint.Hide();
            _hintAnchor = null;

            if (_instructionLabel != null)
                _instructionLabel.gameObject.SetActive(false);
        }

        #endregion

        #region Help ladder

        /// <summary>Re-draws the current level from scratch, e.g. after the guard flips.</summary>
        private void Refresh()
        {
            Clear();
            if (_step == null || _level < 0)
                return;

            ShowInstruction(_step.InstructionKey);
            if (_level >= 1)
                Highlight(_entity);
            if (_level >= 2)
                ShowInputHint(_action);
        }

        private bool AssistanceAllowed()
        {
            if (_guard == null)
            {
                _guard = ServiceLocator.Instance.RequestService<TutorialAssistanceGuard>();
                if (_guard == null)
                    return false; // Default deny: no guard, no help.
                _guard.AssistanceAllowedChanged += HandleAssistanceChanged;
            }
            return _guard.AssistanceAllowed;
        }

        private void HandleAssistanceChanged(bool allowed)
        {
            if (allowed)
                Refresh();
            else
                Clear();
        }

        private void ReleaseGuard()
        {
            if (_guard != null)
                _guard.AssistanceAllowedChanged -= HandleAssistanceChanged;
            _guard = null;
        }

        #endregion

        #region Target and haptic feedback

        private void BindEntity(ITaskEntity entity)
        {
            if (entity == null)
                return;

            // Scale feedback only for a single element; a whole group of options reacting
            // to hover would fight the cards' own states.
            if (entity.Transform != null && entity.Transform.GetComponent<ToggleGroup>() == null)
            {
                SettleFeedbackTarget();
                _feedbackTarget = entity.Transform;
                _feedbackBaseScale = _feedbackTarget.localScale;
            }

            if (entity is IFocusableTaskEntity focusable)
                focusable.FocusChanged += HandleFocusChanged;
            if (entity is ISelectableTaskEntity selectable)
                selectable.SelectionChanged += HandleSelectionChanged;
            if (entity is IConfirmableTaskEntity confirmable)
                confirmable.SelectionConfirmed += HandleConfirmed;
        }

        private void UnbindEntity()
        {
            if (_entity is IFocusableTaskEntity focusable)
                focusable.FocusChanged -= HandleFocusChanged;
            if (_entity is ISelectableTaskEntity selectable)
                selectable.SelectionChanged -= HandleSelectionChanged;
            if (_entity is IConfirmableTaskEntity confirmable)
                confirmable.SelectionConfirmed -= HandleConfirmed;
            _entity = null;
            _focused = false;
        }

        private void HandleFocusChanged(ITaskEntity entity, bool focused) => _focused = focused;

        private void HandleSelectionChanged(ITaskEntity entity, bool selected)
        {
            if (selected)
                React();
        }

        private void HandleConfirmed(ITaskEntity entity) => React();

        /// <summary>Action -> world reaction: target pop, pressed button, haptic, short sound.</summary>
        private void React()
        {
            _reacted = true;
            if (_feedbackTarget != null)
                _popTime = 0f;
            if (_controllerHint != null)
                _controllerHint.Release();

            HandControllerLink link;
            CommonButton button;
            ActionId hapticAction = _action.IsValid ? _action : new ActionId(ActionIds.PrimarySelect);
            if (_input != null && _input.TryGetPhysicalBinding(hapticAction, out link, out button))
                link.TryHapticImpulse(0.06f, 0.35f);

            if (_successClip != null)
                _audio.PlayOneShot(_successClip, 0.5f);
        }

        private void AnimateFeedbackTarget()
        {
            if (_feedbackTarget == null)
                return;

            float scale = _focused ? _focusScale : 1f;
            if (_popTime >= 0f)
            {
                _popTime += Time.deltaTime;
                float t = Mathf.Clamp01(_popTime / _popSeconds);
                scale *= 1f + (_popScale - 1f) * Mathf.Sin(t * Mathf.PI);
                if (t >= 1f)
                    _popTime = -1f;
            }
            _feedbackTarget.localScale = _feedbackBaseScale * scale;

            // Once the step is over and the pop has played, give the object its scale back.
            if (_entity == null && !_focused && _popTime < 0f)
                SettleFeedbackTarget();
        }

        private void SettleFeedbackTarget()
        {
            if (_feedbackTarget != null)
                _feedbackTarget.localScale = _feedbackBaseScale;
            _feedbackTarget = null;
            _popTime = -1f;
        }

        #endregion

        #region Helpers

        private void HideAll()
        {
            Clear();
            if (_controllerHint != null)
                _controllerHint.Hide();
        }

        private ITaskEntity ResolveEntity(TaskStepData step)
        {
            ITaskEntity entity;
            return step.EntityId.IsValid && _service.Entities != null && _service.Entities.TryGetEntity(step.EntityId, out entity)
                ? entity
                : null;
        }

        private void PlaceControllerHint()
        {
            Transform hint = _controllerHint.transform;
            if (_hintAnchoredToHand || _viewer == null)
            {
                hint.position = _hintAnchor.position + Vector3.up * 0.12f;
            }
            else
            {
                // Beside the target, on the viewer's right, so it never covers what to point at.
                Vector3 center;
                Vector2 half;
                GetExtents(_hintAnchor, out center, out half);
                Vector3 right = Vector3.Cross(Vector3.up, center - _viewer.position).normalized;
                hint.position = center + right * (half.x + 0.12f);
            }
            FaceViewer(hint);
        }

        private string Translate(string key)
        {
            string text = _localization != null ? _localization.GetTranslation(key) : null;
            return string.IsNullOrEmpty(text) ? key : text;
        }

        private void FaceViewer(Transform target)
        {
            if (_viewer != null)
                target.rotation = Quaternion.LookRotation(target.position - _viewer.position, Vector3.up);
        }

        /// <summary>
        /// Visible centre and half size (plus a margin) of a target. For UI this is the union
        /// of its children, so a stretched options group resolves to its cards, not the panel.
        /// </summary>
        private void GetExtents(Transform target, out Vector3 center, out Vector2 half)
        {
            RectTransform rect = target as RectTransform;
            if (rect != null)
            {
                Bounds local = RectTransformUtility.CalculateRelativeRectTransformBounds(rect);
                Vector3 scale = rect.lossyScale;
                center = rect.TransformPoint(local.center);
                half = new Vector2(local.extents.x * scale.x, local.extents.y * scale.y) * 1.15f;
            }
            else
            {
                Renderer renderer = target.GetComponentInChildren<Renderer>();
                center = renderer != null ? renderer.bounds.center : target.position;
                float radius = renderer != null ? renderer.bounds.extents.magnitude : 0f;
                half = new Vector2(radius, radius);
            }
            half = Vector2.Max(half, new Vector2(_minHighlightRadius, _minHighlightRadius));
        }

        /// <summary>Soft 70 ms tick so the slice has a click without an audio asset.</summary>
        private static AudioClip CreateTick()
        {
            const int rate = 44100;
            int samples = rate * 7 / 100;
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate;
                data[i] = Mathf.Sin(2f * Mathf.PI * 1320f * t) * Mathf.Exp(-t * 60f);
            }
            AudioClip clip = AudioClip.Create("TutorialTick", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        #endregion
    }
}
