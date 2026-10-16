using UnityEngine;

namespace RideSafe.Help
{
    /// <summary>
    /// No-code Stop/Resume. Alone, the tutorial does not observe while this object is active.
    /// With a watched object, it does not observe while THAT object is active, so the pause can
    /// live under TutorialHelp and leave the module's own objects untouched.
    /// </summary>
    [AddComponentMenu("RideSafe/Tutorial Help/Tutorial Pause")]
    public sealed class TutorialPause : MonoBehaviour
    {
        [Tooltip("Optional: pause while this object (e.g. a module's screen) is active. Empty: while this one is.")]
        [SerializeField] private GameObject _whileActive;

        private bool _paused;

        internal void Watch(GameObject target) => _whileActive = target;

        private void OnEnable()
        {
            if (_whileActive == null)
                Begin();
        }

        private void OnDisable()
        {
            if (_paused)
                End();
        }

        private void Update() => Tick();

        internal void Tick()
        {
            if (_whileActive == null)
                return;
            bool open = _whileActive.activeInHierarchy;
            if (open && !_paused)
                Begin();
            else if (!open && _paused)
                End();
        }

        internal void Begin()
        {
            TutorialHelp.Stop();
            _paused = true;
        }

        internal void End()
        {
            TutorialHelp.Resume();
            _paused = false;
        }
    }
}
