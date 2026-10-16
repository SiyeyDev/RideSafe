using System.Collections.Generic;

namespace RideSafe.Help
{
    /// <summary>
    /// When the automatic help appears. The first time a kind of interaction is on screen it
    /// teaches (after the screen settles); a click on that kind learns it for the session.
    /// Ten seconds without any click is "stuck", learned or not. Narration or video ("quiet")
    /// freezes the count and shows nothing. Stopped: nothing, and clicks teach nothing.
    /// </summary>
    public sealed class HelpRules
    {
        public const float SettleSeconds = 1f;
        public const float StuckSeconds = 10f;

        private readonly HashSet<InteractionKind> _learned = new HashSet<InteractionKind>();
        private float _settled;
        private float _idle;

        public bool Observing { get; private set; } = true;

        public bool IsLearned(InteractionKind kind) => _learned.Contains(kind);

        public void Stop()
        {
            Observing = false;
            _settled = 0f;
            _idle = 0f;
        }

        public void Resume()
        {
            Observing = true;
            _settled = 0f;
            _idle = 0f;
        }

        public void Clicked(InteractionKind kind)
        {
            if (!Observing)
                return;
            _learned.Add(kind);
            _idle = 0f;
        }

        public HelpDecision Tick(float deltaTime, bool interfaceVisible, bool objectVisible, bool quiet)
        {
            if (!Observing || (!interfaceVisible && !objectVisible))
            {
                _settled = 0f;
                _idle = 0f;
                return HelpDecision.None;
            }
            if (quiet)
                return HelpDecision.None;

            _settled += deltaTime;
            _idle += deltaTime;
            if (_settled >= SettleSeconds)
            {
                if (interfaceVisible && !IsLearned(InteractionKind.Interface))
                    return new HelpDecision(HelpMode.FirstTime, InteractionKind.Interface);
                if (objectVisible && !IsLearned(InteractionKind.Object3D))
                    return new HelpDecision(HelpMode.FirstTime, InteractionKind.Object3D);
            }
            if (_idle >= StuckSeconds)
                return new HelpDecision(HelpMode.Stuck, interfaceVisible ? InteractionKind.Interface : InteractionKind.Object3D);
            return HelpDecision.None;
        }
    }
}
