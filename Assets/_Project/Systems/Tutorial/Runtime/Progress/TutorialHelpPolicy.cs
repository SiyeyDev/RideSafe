using System;
using UnityEngine;

namespace RideSafe.Tutorial
{
    /// <summary>How much a learner has practised the action a step asks for.</summary>
    public enum HelpStage
    {
        /// <summary>The step asks to look/point somewhere, no physical control involved.</summary>
        Locate = 0,
        /// <summary>First time the action is asked for: everything at once.</summary>
        Teach = 1,
        /// <summary>Just learned: short text and the controller again as a reminder.</summary>
        Reinforce = 2,
        /// <summary>Practised: text and where; the controller only if the learner hesitates.</summary>
        Light = 3,
        /// <summary>Known: text only; help appears only if the learner is clearly stuck.</summary>
        Known = 4
    }

    /// <summary>
    /// When each piece of help appears, by stage. The instruction always appears at once;
    /// highlight and controller hint follow after their delays. A negative delay means never.
    /// </summary>
    [Serializable]
    public struct HelpTiming
    {
        [Tooltip("Seconds waiting before the target is highlighted. Negative = never.")]
        public float Highlight;

        [Tooltip("Seconds waiting before the controller hint appears. Negative = never.")]
        public float ControllerHint;

        public HelpTiming(float highlight, float controllerHint)
        {
            Highlight = highlight;
            ControllerHint = controllerHint;
        }

        public bool ShowHighlight(float waited) => Highlight >= 0f && waited >= Highlight;
        public bool ShowControllerHint(float waited) => ControllerHint >= 0f && waited >= ControllerHint;
    }

    /// <summary>
    /// Fading help, driven by how often the learner has already performed the step's action
    /// (<see cref="TutorialProgress.PracticeCount"/>). Pure data and rules: presentation asks
    /// it what to show, so the same fading applies to any module and any action.
    /// <para>
    /// The defaults reproduce the original first-use / reminder ladder, so a scene that
    /// does not configure it behaves exactly as before.
    /// </para>
    /// </summary>
    [Serializable]
    public class TutorialHelpPolicy
    {
        [Tooltip("Practice count below which the stage is Reinforce (after Teach at 0).")]
        [SerializeField, Min(1)] private int _reinforceBelow = 2;

        [Tooltip("Practice count below which the stage is Light; at or above it, Known.")]
        [SerializeField, Min(1)] private int _lightBelow = 4;

        [SerializeField] private HelpTiming _locate = new HelpTiming(2.75f, -1f);
        [SerializeField] private HelpTiming _teach = new HelpTiming(0f, 0f);
        [SerializeField] private HelpTiming _reinforce = new HelpTiming(2.75f, 5f);
        [SerializeField] private HelpTiming _light = new HelpTiming(2.75f, 5f);
        [SerializeField] private HelpTiming _known = new HelpTiming(2.75f, 5f);

        public TutorialHelpPolicy() { }

        public TutorialHelpPolicy(int reinforceBelow, int lightBelow, HelpTiming locate, HelpTiming teach,
            HelpTiming reinforce, HelpTiming light, HelpTiming known)
        {
            _reinforceBelow = reinforceBelow;
            _lightBelow = lightBelow;
            _locate = locate;
            _teach = teach;
            _reinforce = reinforce;
            _light = light;
            _known = known;
        }

        public HelpStage StageFor(bool stepHasAction, int practiceCount)
        {
            if (!stepHasAction)
                return HelpStage.Locate;
            if (practiceCount <= 0)
                return HelpStage.Teach;
            if (practiceCount < _reinforceBelow)
                return HelpStage.Reinforce;
            return practiceCount < _lightBelow ? HelpStage.Light : HelpStage.Known;
        }

        public HelpTiming TimingFor(HelpStage stage)
        {
            switch (stage)
            {
                case HelpStage.Locate: return _locate;
                case HelpStage.Teach: return _teach;
                case HelpStage.Reinforce: return _reinforce;
                case HelpStage.Light: return _light;
                default: return _known;
            }
        }
    }
}
