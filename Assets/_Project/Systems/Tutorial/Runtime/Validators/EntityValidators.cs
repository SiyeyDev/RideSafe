using System;
using RideSafe.TaskSequence;
using UnityEngine;

namespace RideSafe.Tutorial
{
    /// <summary>
    /// Satisfied when the step's entity is focused (pointed at / gazed at) for
    /// <c>_dwellSeconds</c>. Dwell defaults to 0, i.e. focus is enough.
    /// <para>Teaches the "Point" verb of the handheld interaction grammar.</para>
    /// </summary>
    [Serializable]
    public class EntityFocusedValidator : TutorialValidatorBase
    {
        [Tooltip("Continuous seconds of focus required. 0 means any focus passes.")]
        [SerializeField, Min(0f)] private float _dwellSeconds;

        private IFocusableTaskEntity _entity;
        private float _focusedFor;

        protected override void OnPrepare()
        {
            _focusedFor = 0f;
            _entity = ResolveEntity<IFocusableTaskEntity>();
        }

        protected override bool OnEvaluate(float deltaTime)
        {
            if (_entity == null)
                return false;

            if (!_entity.IsFocused)
            {
                _focusedFor = 0f;
                return false;
            }

            _focusedFor += deltaTime;
            return _focusedFor >= _dwellSeconds;
        }

        protected override void OnCleanup()
        {
            _entity = null;
            _focusedFor = 0f;
        }

        public override string Describe() =>
            _dwellSeconds > 0f ? "focus entity for " + _dwellSeconds.ToString("0.##") + "s" : "focus entity";
    }

    /// <summary>
    /// Satisfied when the step's entity becomes selected.
    /// <para>
    /// Latches on the transition, not the state, so an entity that was already selected
    /// when the step armed does not pass it for free. Teaches "Select".
    /// </para>
    /// </summary>
    [Serializable]
    public class EntitySelectedValidator : TutorialValidatorBase
    {
        [Tooltip("Accept an entity that was ALREADY selected when this step armed.")]
        [SerializeField] private bool _acceptPreexistingSelection;

        private ISelectableTaskEntity _entity;
        private bool _satisfied;

        protected override void OnPrepare()
        {
            _satisfied = false;
            _entity = ResolveEntity<ISelectableTaskEntity>();
            if (_entity == null)
                return;

            if (_acceptPreexistingSelection && _entity.IsSelected)
                _satisfied = true;

            _entity.SelectionChanged += HandleSelectionChanged;
        }

        protected override bool OnEvaluate(float deltaTime) => _satisfied;

        protected override void OnCleanup()
        {
            if (_entity != null)
                _entity.SelectionChanged -= HandleSelectionChanged;
            _entity = null;
            _satisfied = false;
        }

        private void HandleSelectionChanged(ITaskEntity entity, bool selected)
        {
            if (selected)
                _satisfied = true;
        }

        public override string Describe() => "select entity";
    }

    /// <summary>
    /// Satisfied when the entity goes selected -> deselected.
    /// <para>
    /// Requires the selection to have been observed first, so the step genuinely teaches
    /// the toggle rather than passing because nothing was ever selected. Teaches
    /// "Deselect".
    /// </para>
    /// </summary>
    [Serializable]
    public class EntityDeselectedValidator : TutorialValidatorBase
    {
        [Tooltip("Require the entity to be selected first. Turning this off accepts any deselect event.")]
        [SerializeField] private bool _requireSelectionFirst = true;

        private ISelectableTaskEntity _entity;
        private bool _sawSelection;
        private bool _satisfied;

        protected override void OnPrepare()
        {
            _satisfied = false;
            _entity = ResolveEntity<ISelectableTaskEntity>();
            if (_entity == null)
                return;

            _sawSelection = _entity.IsSelected;
            _entity.SelectionChanged += HandleSelectionChanged;
        }

        protected override bool OnEvaluate(float deltaTime) => _satisfied;

        protected override void OnCleanup()
        {
            if (_entity != null)
                _entity.SelectionChanged -= HandleSelectionChanged;
            _entity = null;
            _sawSelection = false;
            _satisfied = false;
        }

        private void HandleSelectionChanged(ITaskEntity entity, bool selected)
        {
            if (selected)
            {
                _sawSelection = true;
                return;
            }
            if (!_requireSelectionFirst || _sawSelection)
                _satisfied = true;
        }

        public override string Describe() => "deselect entity";
    }

    /// <summary>
    /// Satisfied when the learner commits their choice.
    /// <para>
    /// Accepts either route: the entity raising SelectionConfirmed, or the abstract
    /// Confirm action being performed. Teaches "Review / Confirm".
    /// </para>
    /// </summary>
    [Serializable]
    public class SelectionConfirmedValidator : TutorialValidatorBase
    {
        [Tooltip("Also accept the abstract confirm action, for confirm buttons outside the entity.")]
        [SerializeField] private bool _acceptConfirmAction = true;

        [SerializeField] private string _confirmActionId = ActionIds.Confirm;

        [Tooltip("Require the entity to be selected at the moment of confirmation.")]
        [SerializeField] private bool _requireSelection;

        private IConfirmableTaskEntity _confirmable;
        private ISelectableTaskEntity _selectable;
        private ITutorialInputService _input;
        private bool _satisfied;

        public ActionId ConfirmAction => new ActionId(_confirmActionId);

        protected override void OnPrepare()
        {
            _satisfied = false;

            // The entity is optional here: a step may confirm a whole panel via the action.
            if (Context != null && Context.Entity != null)
            {
                _confirmable = Context.Entity as IConfirmableTaskEntity;
                _selectable = Context.Entity as ISelectableTaskEntity;
                if (_confirmable != null)
                    _confirmable.SelectionConfirmed += HandleConfirmed;
            }

            if (_acceptConfirmAction)
            {
                _input = ResolveInput();
                RequireMappedAction(_input, ConfirmAction);
            }
            else if (_confirmable == null)
            {
                Break("no IConfirmableTaskEntity and the confirm action is disabled.");
            }
        }

        protected override bool OnEvaluate(float deltaTime)
        {
            if (_satisfied)
                return true;

            if (_acceptConfirmAction && _input != null && _input.WasPerformedThisFrame(ConfirmAction))
                return Accept();

            return false;
        }

        protected override void OnCleanup()
        {
            if (_confirmable != null)
                _confirmable.SelectionConfirmed -= HandleConfirmed;
            _confirmable = null;
            _selectable = null;
            _input = null;
            _satisfied = false;
        }

        private void HandleConfirmed(ITaskEntity entity) => Accept();

        private bool Accept()
        {
            if (_requireSelection && _selectable != null && !_selectable.IsSelected)
            {
                if (Context != null)
                    Context.LogWarn("Confirm ignored: nothing is selected yet.");
                return false;
            }
            _satisfied = true;
            return true;
        }

        public override string Describe() => "confirm selection";
    }
}
