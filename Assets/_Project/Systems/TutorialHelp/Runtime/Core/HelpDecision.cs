using System;

namespace RideSafe.Help
{
    public enum HelpMode
    {
        None = 0,
        /// <summary>First time this kind of interaction appears in the session: controller, highlight and text.</summary>
        FirstTime = 1,
        /// <summary>No click for a while: controller and highlight, no text.</summary>
        Stuck = 2
    }

    /// <summary>What the automatic help should show right now.</summary>
    public readonly struct HelpDecision : IEquatable<HelpDecision>
    {
        public static readonly HelpDecision None = new HelpDecision(HelpMode.None, InteractionKind.Interface);

        public HelpDecision(HelpMode mode, InteractionKind kind)
        {
            Mode = mode;
            Kind = kind;
        }

        public HelpMode Mode { get; }
        public InteractionKind Kind { get; }

        public bool Equals(HelpDecision other) => Mode == other.Mode && (Mode == HelpMode.None || Kind == other.Kind);
        public override bool Equals(object obj) => obj is HelpDecision other && Equals(other);
        public override int GetHashCode() => Mode == HelpMode.None ? 0 : (int)Mode * 31 + (int)Kind;
        public override string ToString() => Mode == HelpMode.None ? "None" : Mode + "(" + Kind + ")";
    }
}
