using System.Collections.Generic;
using UnityEngine;

namespace RideSafe.Help
{
    /// <summary>
    /// Where the controller hint goes: of the candidate spots (centre first, then the sides),
    /// the one that covers the least of the module's panels, in viewport space. Ties keep the
    /// earlier candidate, so with nothing in the way the hint stays centred.
    /// </summary>
    public static class HintSpot
    {
        public static int Choose(IReadOnlyList<Rect> candidates, IReadOnlyList<Rect> panels)
        {
            int best = 0;
            float bestCovered = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                float covered = 0f;
                foreach (Rect panel in panels)
                    covered += Overlap(candidates[i], panel);
                if (covered < bestCovered - 1e-5f)
                {
                    best = i;
                    bestCovered = covered;
                }
            }
            return best;
        }

        public static float Overlap(Rect a, Rect b)
        {
            float w = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
            float h = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            return w > 0f && h > 0f ? w * h : 0f;
        }
    }
}
