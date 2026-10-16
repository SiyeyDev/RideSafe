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
        /// <summary>Overlap under this share of the card is a graze, not a reason to move aside.</summary>
        public const float Tolerance = 0.05f;

        public static int Choose(IReadOnlyList<Rect> candidates, IReadOnlyList<Rect> panels)
        {
            int best = 0;
            float bestCovered = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                float covered = 0f;
                for (int p = 0; p < panels.Count; p++)
                    covered += Overlap(candidates[i], panels[p]);
                // Welcome: centred, the card's edge grazed «Comenzar» by 1% of the screen and the
                // hint jumped aside, half off the view. Only a real overlap moves it.
                if (covered < Tolerance * candidates[i].width * candidates[i].height)
                    covered = 0f;
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
