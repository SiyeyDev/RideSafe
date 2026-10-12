using System.Collections.Generic;
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
    /// highlight and controller hint after the delays of <see cref="TutorialHelpPolicy"/>,
    /// which fades help as <see cref="TutorialProgress.PracticeCount"/> grows. Its defaults
    /// keep the original ladder (highlight 2.75 s, controller 5 s).
    /// </para>
    /// <para>
    /// Every piece of help asks <see cref="TutorialAssistanceGuard"/> first. World feedback
    /// (the target reacting, the pressed button, haptic, sound) is the result of the
    /// learner's own action, not help, so it is not gated.
    /// </para>
    /// <para>
    /// Hardware stays behind contracts: the action's hand and control come from an
    /// <see cref="IPhysicalBindingResolver"/>, vibration from <see cref="IHapticFeedback"/>,
    /// hand poses from <see cref="IXRPoseProvider"/>, and the controller drawing is any
    /// <see cref="IControllerHintView"/> (3D Touch Plus first, the 2D drawing as fallback).
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/Tutorial/Tutorial Presenter")]
    [RequireComponent(typeof(AudioSource))]
    public class TutorialPresenter : MonoBehaviour
    {
        [SerializeField] private TaskSequenceService _service;

        [Tooltip("Resolves an action to hand + control (IPhysicalBindingResolver), and optionally vibrates " +
                 "(IHapticFeedback). E.g. AutoHandTutorialInputService.")]
        [SerializeField] private MonoBehaviour _input;

        [Tooltip("XRPlayer's head camera. Highlight and controller hint face it.")]
        [SerializeField] private Transform _viewer;

        [Header("Views")]
        [Tooltip("Short instruction at a comfortable fixed spot in the garage.")]
        [SerializeField] private TMP_Text _instructionLabel;

        [Tooltip("Place the instruction just above the step's target, facing the viewer. Off keeps it where the " +
                 "scene put it. Needed when the label must be readable before its authored parent is shown.")]
        [SerializeField] private bool _instructionAboveTarget;

        [Tooltip("With the instruction above the target: distance at which it keeps its authored size. Farther " +
                 "targets get a proportionally larger label, so it reads the same. 0 = never rescale.")]
        [SerializeField, Min(0f)] private float _instructionReferenceDistance;

        [Tooltip("With the instruction above the target: metres between the target's top and the label, at the " +
                 "authored size. Room for the panel's own title.")]
        [SerializeField, Min(0f)] private float _instructionGap = 0.08f;

        [Tooltip("With the instruction above the target: when the target is this many metres or more below eye " +
                 "level (e.g. a Continue/Confirm bar), the instruction goes under it instead, clear of the panel's " +
                 "own content above. 0 = always above.")]
        [SerializeField, Min(0f)] private float _instructionBelowUnderEyeBy;

        [Tooltip("Measure UI targets by their visible graphics only, so highlight and instruction hug the real " +
                 "cards instead of a stretched container.")]
        [SerializeField] private bool _boundsFromVisibleGraphics;

        [Tooltip("Ring drawn around the target. Local-space circle of radius 1.")]
        [SerializeField] private LineRenderer _highlight;

        [Tooltip("Optional ITargetHighlightView that draws the target mark instead of the line ring, e.g. a " +
                 "SpriteTargetHighlight with RideSafe's Focus_Ring_Mint. This presenter still decides when it shows.")]
        [SerializeField] private MonoBehaviour _targetHighlight;
        [Tooltip("Also highlight UI controls that have their own hover/selected states (cards, buttons). " +
                 "Off: those steps rely on the UI's own states and only 3D/neutral targets get the mark. " +
                 "A step's Target Highlight (Show/Hide) overrides this.")]
        [SerializeField] private bool _highlightSelfStyledUI;

        [Tooltip("IControllerHintView tried first, e.g. ControllerHint3DView.")]
        [SerializeField] private MonoBehaviour _controllerHint;

        [Tooltip("IControllerHintView used when the first one cannot draw the control, e.g. the 2D ControllerHintView.")]
        [SerializeField] private MonoBehaviour _fallbackControllerHint;

        [Header("Help fading")]
        [Tooltip("When highlight and controller appear, by how often the action was already practised.")]
        [SerializeField] private TutorialHelpPolicy _help = new TutorialHelpPolicy();

        [Header("Focus feedback")]
        [Tooltip("The ring stops pulsing and changes colour while the learner points at the target.")]
        [SerializeField] private bool _ringReactsToFocus;
        [SerializeField] private Color _focusedRingColor = new Color(0.55f, 0.95f, 0.75f, 1f);

        [Header("Feedback")]
        [SerializeField] private float _focusScale = 1.06f;
        [SerializeField] private float _popScale = 1.14f;
        [SerializeField, Min(0.05f)] private float _popSeconds = 0.3f;
        [Tooltip("Short confirmation sound. A soft tick is generated when empty.")]
        [SerializeField] private AudioClip _successClip;

        [Header("Look")]
        [SerializeField, Min(0.01f)] private float _minHighlightRadius = 0.08f;

        private TaskSequenceRunner _runner;
        private IPhysicalBindingResolver _bindings;
        private IHapticFeedback _haptics;
        private IXRPoseProvider _poses;
        private readonly List<IControllerHintView> _hintViews = new List<IControllerHintView>(2);
        private TutorialAssistanceGuard _guard;
        private ILocalizationProvider _localization;
        private AudioSource _audio;

        // Current step
        private TaskStepData _step;
        private ITaskEntity _entity;
        private ActionId _action;
        private HelpStage _stage;
        private HelpTiming _timing;
        private bool _firstUse;
        private bool _reacted;
        private float _waited;
        private bool _showHighlight;
        private bool _showHint;

        // Views state
        private ITaskEntity _highlighted;
        private IControllerHintView _activeHint;
        private ITargetHighlightView _targetHighlightView;
        private Transform _hintAnchor;
        private XRNodeRole _hintHand;
        private bool _hintFollows;
        private bool _hintAnchoredToHand;

        // Target feedback (outlives the step so the pop can finish)
        private Transform _feedbackTarget;
        private Vector3 _feedbackBaseScale;
        private bool _focused;
        private float _popTime = -1f;
        private Color _ringStartColor;
        private Color _ringEndColor;
        private Vector3 _instructionBaseScale = Vector3.one;
        private float _instructionBottom;
        private float _instructionTop;
        private readonly List<Graphic> _graphics = new List<Graphic>();
        private readonly Vector3[] _corners = new Vector3[4];

        #region Lifecycle

        protected virtual void Awake()
        {
            _bindings = _input as IPhysicalBindingResolver;
            _haptics = _input as IHapticFeedback;
            AddHintView(_controllerHint);
            AddHintView(_fallbackControllerHint);

            _targetHighlightView = _targetHighlight as ITargetHighlightView;
            if (_targetHighlight != null && _targetHighlightView == null)
                Debug.LogError("[Tutorial] '" + _targetHighlight.name + "' is not an ITargetHighlightView.", this);
            if (_instructionLabel != null)
                _instructionBaseScale = _instructionLabel.transform.localScale;
            if (_highlight != null)
            {
                _ringStartColor = _highlight.startColor;
                _ringEndColor = _highlight.endColor;
            }

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
            if (_step == null || _runner == null || _runner.Phase != TaskStepPhase.WaitForInteraction)
                return;

            _waited += Time.deltaTime;
            bool highlight = _showHighlight || _timing.ShowHighlight(_waited);
            bool hint = _showHint || (_action.IsValid && _timing.ShowControllerHint(_waited));
            if (highlight != _showHighlight || hint != _showHint)
            {
                _showHighlight = highlight;
                _showHint = hint;
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
                bool focused = _ringReactsToFocus && _focused && _highlighted == _entity;
                if (_targetHighlightView != null)
                {
                    Pose viewer;
                    Quaternion facing = TryGetViewer(out viewer)
                        ? Quaternion.LookRotation(center - viewer.position, Vector3.up)
                        : _highlighted.Transform.rotation;
                    _targetHighlightView.Frame(center, half, facing, focused);
                }
                else
                {
                    Transform ring = _highlight.transform;
                    ring.position = center;
                    FaceViewer(ring);
                    float pulse = focused ? 1.04f : 1f + 0.06f * Mathf.Sin(Time.time * 4f);
                    ring.localScale = new Vector3(half.x * pulse, half.y * pulse, 1f);
                    _highlight.startColor = focused ? _focusedRingColor : _ringStartColor;
                    _highlight.endColor = focused ? _focusedRingColor : _ringEndColor;
                }
            }

            if (_hintFollows && _activeHint != null && _activeHint.IsVisible)
                PlaceControllerHint();

            if (_instructionLabel != null && _instructionLabel.gameObject.activeSelf)
                PlaceInstruction();

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
            int practice = progress != null ? progress.PracticeCount(_action) : 0;
            _stage = _help.StageFor(_action.IsValid, practice);
            _timing = _help.TimingFor(_stage);
            _firstUse = _stage == HelpStage.Teach;
            _reacted = false;
            _waited = 0f;
            _showHighlight = _timing.ShowHighlight(0f);
            _showHint = _action.IsValid && _timing.ShowControllerHint(0f);

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
                TutorialProgress progress = ServiceLocator.Instance.RequestService<TutorialProgress>();
                if (progress != null && _action.IsValid)
                    progress.RecordPractice(_action);
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
            _instructionLabel.ForceMeshUpdate();
            _instructionBottom = _instructionLabel.textBounds.min.y;
            _instructionTop = _instructionLabel.textBounds.max.y;
            PlaceInstruction();
        }

        public void Highlight(ITaskEntity entity)
        {
            if (!AssistanceAllowed() || (_highlight == null && _targetHighlightView == null) ||
                entity == null || entity.Transform == null)
                return;
            _highlighted = entity;
            if (_targetHighlightView != null)
                _targetHighlightView.Show();
            else
                _highlight.gameObject.SetActive(true);
        }

        /// <summary>
        /// Shows the controller with the control bound to <paramref name="action"/>. Next to the
        /// step's target when there is one (where the learner is looking), otherwise over the hand.
        /// The first view able to draw the control wins; with none, no hint is shown.
        /// </summary>
        public void ShowInputHint(ActionId action)
        {
            PhysicalBinding binding;
            if (!AssistanceAllowed() || _bindings == null || !_bindings.TryResolve(action, out binding))
                return;

            _activeHint = null;
            for (int i = 0; i < _hintViews.Count && _activeHint == null; i++)
            {
                if (_hintViews[i].Show(binding))
                    _activeHint = _hintViews[i];
            }
            if (_activeHint == null)
                return;

            _hintHand = binding.Hand;
            _hintAnchoredToHand = _entity == null || _entity.Transform == null;
            _hintAnchor = _hintAnchoredToHand ? null : _entity.Transform;
            _hintFollows = true;
            PlaceControllerHint();
        }

        /// <summary>Removes instruction, highlight and hint. A hint already playing its pressed pose finishes on its own.</summary>
        public void Clear()
        {
            _highlighted = null;
            if (_targetHighlightView != null)
                _targetHighlightView.Hide();
            if (_highlight != null)
                _highlight.gameObject.SetActive(false);

            if (_activeHint != null && !_activeHint.IsReleasing)
                _activeHint.Hide();
            _hintAnchor = null;
            _hintFollows = false;

            if (_instructionLabel != null)
                _instructionLabel.gameObject.SetActive(false);
        }

        #endregion

        #region Help ladder

        /// <summary>Re-draws the current level from scratch, e.g. after the guard flips.</summary>
        private void Refresh()
        {
            Clear();
            if (_step == null)
                return;

            ShowInstruction(_step.InstructionKey);
            if (_showHighlight && WantsHighlight())
                Highlight(_entity);
            if (_showHint)
                ShowInputHint(_action);
        }

        private bool WantsHighlight()
        {
            Transform target = _entity != null ? _entity.Transform : null;
            return TargetHighlightRule.ShouldHighlight(_step.TargetHighlight, target, _highlightSelfStyledUI);
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
            if (_activeHint != null && _activeHint.IsVisible)
                _activeHint.Release();

            PhysicalBinding binding;
            ActionId hapticAction = _action.IsValid ? _action : new ActionId(ActionIds.PrimarySelect);
            if (_haptics != null && _bindings != null && _bindings.TryResolve(hapticAction, out binding))
                _haptics.TryPulse(binding.Hand, 0.35f, 0.06f);

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

        protected virtual void OnValidate()
        {
            if (_input != null && !(_input is IPhysicalBindingResolver))
                Debug.LogWarning("[Tutorial] '" + _input.name + "' does not implement IPhysicalBindingResolver.", this);
        }

        private void HideAll()
        {
            Clear();
            for (int i = 0; i < _hintViews.Count; i++)
                _hintViews[i].Hide();
            _activeHint = null;
        }

        private void AddHintView(MonoBehaviour candidate)
        {
            IControllerHintView view = candidate as IControllerHintView;
            if (view != null && !_hintViews.Contains(view))
                _hintViews.Add(view);
            else if (candidate != null && view == null)
                Debug.LogError("[Tutorial] '" + candidate.name + "' is not an IControllerHintView.", this);
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
            Pose viewer;
            if (_activeHint is IHeadRelativeHintView headRelative && TryGetViewer(out viewer))
            {
                Vector3 focus;
                if (_hintAnchoredToHand)
                {
                    if (!TryGetHandPosition(_hintHand, out focus))
                        focus = viewer.position + viewer.rotation * Vector3.forward;
                }
                else
                {
                    Vector2 targetHalf;
                    GetExtents(_hintAnchor, out focus, out targetHalf);
                }
                headRelative.Place(viewer, focus);
                return;
            }

            Transform hint = _activeHint.transform;
            if (_hintAnchoredToHand)
            {
                Vector3 hand;
                if (TryGetHandPosition(_hintHand, out hand))
                    hint.position = hand + Vector3.up * 0.12f;
                else if (_viewer != null)
                    hint.position = _viewer.position + _viewer.forward * 0.45f - Vector3.up * 0.15f;
            }
            else if (_viewer == null)
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

        /// <summary>Above the current target, facing the viewer, when <c>_instructionAboveTarget</c> is on.</summary>
        private void PlaceInstruction()
        {
            if (!_instructionAboveTarget || _entity == null || _entity.Transform == null)
                return;

            Vector3 center;
            Vector2 half;
            GetExtents(_entity.Transform, out center, out half);
            Transform label = _instructionLabel.transform;
            Pose viewer;
            float scale = _instructionReferenceDistance > 0f && TryGetViewer(out viewer)
                ? Mathf.Clamp(Vector3.Distance(viewer.position, center) / _instructionReferenceDistance, 1f, 3f)
                : 1f;
            label.localScale = _instructionBaseScale * scale;
            bool below = _instructionBelowUnderEyeBy > 0f && TryGetViewer(out viewer) &&
                         viewer.position.y - center.y >= _instructionBelowUnderEyeBy;
            float gap = _instructionGap * scale;
            // The text's near edge (however many lines) sits the gap above, or below, the target.
            label.position = below
                ? center - Vector3.up * (half.y + gap * 0.4f + _instructionTop * label.lossyScale.y)
                : center + Vector3.up * (half.y + gap - _instructionBottom * label.lossyScale.y);
            FaceViewer(label);
        }

        private bool TryGetHandPosition(XRNodeRole hand, out Vector3 position)
        {
            if (_poses == null)
                _poses = ServiceLocator.Instance.RequestService<IXRPoseProvider>();

            Pose pose = default(Pose);
            bool found = _poses != null && _poses.TryGetPose(hand, out pose);
            position = found ? pose.position : Vector3.zero;
            return found;
        }

        private string Translate(string key)
        {
            string text = _localization != null ? _localization.GetTranslation(key) : null;
            return string.IsNullOrEmpty(text) ? key : text;
        }

        private void FaceViewer(Transform target)
        {
            Pose viewer;
            if (TryGetViewer(out viewer))
                target.rotation = Quaternion.LookRotation(target.position - viewer.position, Vector3.up);
        }

        /// <summary>
        /// The learner's head: from the pose provider when there is one (the XR head in VR,
        /// the desktop camera when simulating without a headset), else the serialized viewer.
        /// </summary>
        private bool TryGetViewer(out Pose viewer)
        {
            if (_poses == null)
                _poses = ServiceLocator.Instance.RequestService<IXRPoseProvider>();
            if (_poses != null && _poses.TryGetPose(XRNodeRole.Head, out viewer))
                return true;
            viewer = _viewer != null ? new Pose(_viewer.position, _viewer.rotation) : default(Pose);
            return _viewer != null;
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
                Bounds local = _boundsFromVisibleGraphics ? VisibleGraphicsBounds(rect)
                    : RectTransformUtility.CalculateRelativeRectTransformBounds(rect);
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

        /// <summary>Union of the visible graphics under <paramref name="rect"/>, in its local space.</summary>
        private Bounds VisibleGraphicsBounds(RectTransform rect)
        {
            rect.GetComponentsInChildren(false, _graphics);
            bool any = false;
            Bounds bounds = default(Bounds);
            for (int i = 0; i < _graphics.Count; i++)
            {
                Graphic graphic = _graphics[i];
                if (!graphic.enabled || graphic.color.a < 0.01f)
                    continue;
                graphic.rectTransform.GetWorldCorners(_corners);
                for (int c = 0; c < 4; c++)
                {
                    Vector3 point = rect.InverseTransformPoint(_corners[c]);
                    if (!any)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        any = true;
                    }
                    else
                    {
                        bounds.Encapsulate(point);
                    }
                }
            }
            return any ? bounds : RectTransformUtility.CalculateRelativeRectTransformBounds(rect);
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
