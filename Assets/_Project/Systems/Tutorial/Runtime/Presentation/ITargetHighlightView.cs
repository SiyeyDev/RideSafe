using UnityEngine;

namespace RideSafe.Tutorial
{
    /// <summary>
    /// Draws the "this is where" mark around a step's target. Presentation decides when it shows
    /// and what it frames; the view only draws, so the look can change (a sprite ring, a line)
    /// without touching that decision.
    /// </summary>
    public interface ITargetHighlightView
    {
        void Show();
        void Hide();

        /// <param name="center">World centre of the target.</param>
        /// <param name="halfSize">World half width / height of the area to frame.</param>
        /// <param name="facing">Rotation facing away from the viewer.</param>
        /// <param name="focused">The learner is pointing at the target right now.</param>
        void Frame(Vector3 center, Vector2 halfSize, Quaternion facing, bool focused);
    }
}
