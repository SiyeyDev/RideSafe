using RideSafe.TaskSequence;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.Tutorial.AutoHand
{
    /// <summary>
    /// Decides whether a step's target gets the extra tutorial highlight. A uGUI control
    /// (card, toggle, button) already shows hover, selected and pressed through its own
    /// sprites, so a ring around it would only repeat that; a 3D or neutral tutorial target
    /// has no such states and keeps the highlight. A step can override with Show or Hide.
    /// </summary>
    public static class TargetHighlightRule
    {
        public static bool ShouldHighlight(TargetHighlightMode mode, Transform target, bool highlightSelfStyledUI)
        {
            switch (mode)
            {
                case TargetHighlightMode.Show: return true;
                case TargetHighlightMode.Hide: return false;
                default: return highlightSelfStyledUI || !HasOwnStates(target);
            }
        }

        /// <summary>True when the target is, or contains, an active uGUI Selectable.</summary>
        public static bool HasOwnStates(Transform target)
        {
            return target != null && target.GetComponentInChildren<Selectable>(false) != null;
        }
    }
}
