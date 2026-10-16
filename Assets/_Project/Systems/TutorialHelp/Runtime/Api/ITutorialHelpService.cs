using System;
using System.Collections.Generic;
using UnityEngine;

namespace RideSafe.Help
{
    /// <summary>What a scene's tutorial offers to modules (through <see cref="TutorialHelp"/>).</summary>
    public interface ITutorialHelpService
    {
        IDisposable Show(TutorialGestureSO gesture, IReadOnlyList<Transform> targets);
        void Stop();
        void Resume();
        bool Observing { get; }
    }
}
