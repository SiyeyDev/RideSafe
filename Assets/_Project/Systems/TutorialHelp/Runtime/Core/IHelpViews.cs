using System.Collections.Generic;
using RideSafe.Tutorial;
using UnityEngine;

namespace RideSafe.Help
{
    /// <summary>The Touch Plus controller with the gesture's control lit and pressed.</summary>
    public interface IGestureHintView
    {
        /// <summary>Idempotent: called every frame while help is on; only a change restarts the animation.</summary>
        void Show(TutorialGestureSO gesture, XRNodeRole hand, string text, Vector3? focus);
        void Hide();
        bool IsVisible { get; }
    }

    /// <summary>Marks where to act: soft pulse on canvas controls, mint halo on objects.</summary>
    public interface ITargetHighlighter
    {
        /// <summary>Highlights exactly these targets; any other one highlighted before is restored.</summary>
        void Highlight(IReadOnlyList<HelpTarget> targets);
        void Clear();
    }
}
