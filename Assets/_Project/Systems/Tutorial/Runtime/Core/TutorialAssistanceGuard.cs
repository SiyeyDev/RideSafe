using System;
using UnityEngine;

namespace RideSafe.Tutorial
{
    /// <summary>
    /// Which kind of activity the learner is in. Drives whether help is legal.
    /// </summary>
    public enum TutorialMode
    {
        /// <summary>Guided teaching. Help allowed.</summary>
        Tutorial = 0,
        /// <summary>Unscored rehearsal. Help allowed.</summary>
        Practice = 1,
        /// <summary>Scored. NO help of any kind.</summary>
        Assessment = 2,
        /// <summary>Post-assessment review. Help allowed; nothing is being scored.</summary>
        Feedback = 3,
        /// <summary>Changed-situation transfer test. NO help of any kind.</summary>
        Transfer = 4
    }

    /// <summary>
    /// Single authority on whether tutorial assistance may be shown.
    /// <para>
    /// The point is that no programmer has to remember to switch hints off. Assessment
    /// and Transfer force <see cref="AssistanceAllowed"/> to false structurally, and the
    /// presentation layer is required to ask this guard before drawing anything. A
    /// highlight, controller ghost, arrow or target marker that leaks into a scored state
    /// would reveal answers, so the default is deny.
    /// </para>
    /// </summary>
    public sealed class TutorialAssistanceGuard
    {
        private TutorialMode _mode = TutorialMode.Tutorial;
        private int _suppressionCount;

        /// <summary>Raised whenever the effective permission flips.</summary>
        public event Action<bool> AssistanceAllowedChanged;

        /// <summary>Raised on every mode change, for logging and analytics.</summary>
        public event Action<TutorialMode> ModeChanged;

        public TutorialMode Mode => _mode;

        /// <summary>
        /// True only in Tutorial, Practice or Feedback, and only while nothing holds a
        /// suppression. Everything that draws help must check this.
        /// </summary>
        public bool AssistanceAllowed => _suppressionCount == 0 && ModeAllowsAssistance(_mode);

        public static bool ModeAllowsAssistance(TutorialMode mode) =>
            mode == TutorialMode.Tutorial || mode == TutorialMode.Practice || mode == TutorialMode.Feedback;

        public void SetMode(TutorialMode mode)
        {
            if (_mode == mode)
                return;

            bool wasAllowed = AssistanceAllowed;
            _mode = mode;

            Debug.Log("[Tutorial] Mode -> " + mode + " (assistance " +
                      (AssistanceAllowed ? "ALLOWED" : "BLOCKED") + ")");

            Action<TutorialMode> modeHandler = ModeChanged;
            if (modeHandler != null)
                modeHandler.Invoke(mode);

            RaiseIfChanged(wasAllowed);
        }

        /// <summary>
        /// Temporarily blocks assistance regardless of mode, for example during a
        /// cutscene. Reference counted; always pair with <see cref="ReleaseSuppression"/>.
        /// </summary>
        public void PushSuppression()
        {
            bool wasAllowed = AssistanceAllowed;
            _suppressionCount++;
            RaiseIfChanged(wasAllowed);
        }

        public void ReleaseSuppression()
        {
            if (_suppressionCount == 0)
            {
                Debug.LogWarning("[Tutorial] ReleaseSuppression called with no active suppression.");
                return;
            }
            bool wasAllowed = AssistanceAllowed;
            _suppressionCount--;
            RaiseIfChanged(wasAllowed);
        }

        /// <summary>Drops all suppressions. Used by the session scope on teardown.</summary>
        public void Reset()
        {
            bool wasAllowed = AssistanceAllowed;
            _suppressionCount = 0;
            _mode = TutorialMode.Tutorial;
            RaiseIfChanged(wasAllowed);
            AssistanceAllowedChanged = null;
            ModeChanged = null;
        }

        private void RaiseIfChanged(bool wasAllowed)
        {
            bool isAllowed = AssistanceAllowed;
            if (isAllowed == wasAllowed)
                return;

            Action<bool> handler = AssistanceAllowedChanged;
            if (handler != null)
                handler.Invoke(isAllowed);
        }
    }
}
