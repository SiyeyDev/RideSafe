using System;
using UnityEngine;

namespace RideSafe.Tutorial
{
    /// <summary>
    /// Satisfied when a named signal is raised on the <see cref="TutorialSignalBus"/>.
    /// <para>
    /// This is the escape hatch that keeps module-specific validators from ever being
    /// written. Instead of a Module01PanelOpenedValidator, the module bridge raises
    /// "module01.panel.opened" and the step waits for that string. The tutorial core stays
    /// ignorant of what the signal means.
    /// </para>
    /// </summary>
    [Serializable]
    public class CustomEventValidator : TutorialValidatorBase
    {
        [Tooltip("Signal name raised by gameplay, e.g. module01.panel.opened")]
        [SerializeField] private string _signal;

        [Tooltip("Also accept the signal if it fired at most this many seconds BEFORE the step armed. 0 = only after.")]
        [SerializeField, Min(0f)] private float _latchWindowSeconds;

        private TutorialSignalBus _bus;
        private bool _satisfied;

        public CustomEventValidator() { }

        internal CustomEventValidator(string signal, float latchWindowSeconds = 0f)
        {
            _signal = signal;
            _latchWindowSeconds = latchWindowSeconds;
        }

        protected override void OnPrepare()
        {
            _satisfied = false;

            if (string.IsNullOrWhiteSpace(_signal))
            {
                Break("no signal name configured.");
                return;
            }

            if (Context == null)
                return;

            _bus = Context.GetService<TutorialSignalBus>();
            if (_bus == null)
            {
                Break("no TutorialSignalBus is registered.");
                return;
            }

            if (_bus.WasRaisedWithin(_signal, _latchWindowSeconds))
                _satisfied = true;

            _bus.Subscribe(_signal, HandleSignal);
        }

        protected override bool OnEvaluate(float deltaTime) => _satisfied;

        protected override void OnCleanup()
        {
            if (_bus != null)
                _bus.Unsubscribe(_signal, HandleSignal);
            _bus = null;
            _satisfied = false;
        }

        private void HandleSignal(string signal) => _satisfied = true;

        public override string Describe() =>
            "signal '" + (string.IsNullOrEmpty(_signal) ? "<unset>" : _signal) + "'";
    }
}
