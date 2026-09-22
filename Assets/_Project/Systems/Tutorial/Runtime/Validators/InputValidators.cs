using System;
using UnityEngine;

namespace RideSafe.Tutorial
{
    /// <summary>
    /// Satisfied when the abstract action is performed.
    /// <para>
    /// Only presses that happen AFTER Prepare count. A press made while the previous step
    /// was still running is deliberately ignored so the learner cannot skip ahead by
    /// mashing a button (CASE 05). Set <c>_graceWindow</c> above zero to accept a press
    /// made just before the step armed, for steps where that feels unfair.
    /// </para>
    /// </summary>
    [Serializable]
    public class InputPressedValidator : TutorialValidatorBase
    {
        [Tooltip("Abstract action, e.g. primaryselect. Never a physical button.")]
        [SerializeField] private string _actionId = ActionIds.PrimarySelect;

        [Tooltip("Seconds of leniency for a press made just before this step armed. 0 = strict.")]
        [SerializeField, Min(0f)] private float _graceWindow;

        private ITutorialInputService _input;
        private float _armedFor;

        public ActionId Action => new ActionId(_actionId);

        protected override void OnPrepare()
        {
            _armedFor = 0f;
            _input = ResolveInput();
            RequireMappedAction(_input, Action);
        }

        protected override bool OnEvaluate(float deltaTime)
        {
            _armedFor += deltaTime;
            if (_input == null)
                return false;

            // Only edges observed after Prepare count, so a press made during the previous
            // step never carries over. _graceWindow is reserved for a future pre-arm
            // buffer in the input service; it intentionally does nothing yet.
            return _input.WasPerformedThisFrame(Action);
        }

        protected override void OnCleanup() => _input = null;

        public override string Describe() => "press " + Action;
    }

    /// <summary>
    /// Satisfied when the action is held continuously for <c>_holdSeconds</c>.
    /// Releasing early resets the timer, so a series of taps never passes.
    /// </summary>
    [Serializable]
    public class InputHeldValidator : TutorialValidatorBase
    {
        [SerializeField] private string _actionId = ActionIds.PrimarySelect;

        [Tooltip("Continuous seconds required.")]
        [SerializeField, Min(0.01f)] private float _holdSeconds = 1f;

        private ITutorialInputService _input;
        private float _held;

        public ActionId Action => new ActionId(_actionId);

        /// <summary>0..1 progress. Presentation can drive a radial fill from this.</summary>
        public float Progress => _holdSeconds <= 0f ? 0f : Mathf.Clamp01(_held / _holdSeconds);

        protected override void OnPrepare()
        {
            _held = 0f;
            _input = ResolveInput();
            RequireMappedAction(_input, Action);
        }

        protected override bool OnEvaluate(float deltaTime)
        {
            if (_input == null)
                return false;

            if (!_input.IsHeld(Action))
            {
                _held = 0f;
                return false;
            }

            _held += deltaTime;
            return _held >= _holdSeconds;
        }

        protected override void OnCleanup()
        {
            _input = null;
            _held = 0f;
        }

        public override string Describe() => "hold " + Action + " for " + _holdSeconds.ToString("0.##") + "s";
    }
}
