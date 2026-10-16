using System;
using UnityEngine;

namespace RideSafe.Help
{
    /// <summary>No-code explicit help: shows the gesture on the targets while this object is active.</summary>
    [AddComponentMenu("RideSafe/Tutorial Help/Tutorial Cue")]
    public sealed class TutorialCue : MonoBehaviour
    {
        [SerializeField] private TutorialGestureSO _gesture;
        [SerializeField] private Transform[] _targets = Array.Empty<Transform>();

        private IDisposable _help;

        internal void Configure(TutorialGestureSO gesture, params Transform[] targets)
        {
            _gesture = gesture;
            _targets = targets;
        }

        private void OnEnable() => Begin();
        private void OnDisable() => End();

        internal void Begin()
        {
            End();
            _help = TutorialHelp.Show(_gesture, _targets);
        }

        internal void End()
        {
            _help?.Dispose();
            _help = null;
        }
    }
}
