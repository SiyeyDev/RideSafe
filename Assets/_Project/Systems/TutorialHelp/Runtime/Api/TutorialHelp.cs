using System;
using System.Collections.Generic;
using Cachacos;
using UnityEngine;

namespace RideSafe.Help
{
    /// <summary>
    /// The only door modules use, and only when the automatic help cannot see what they need.
    /// With no tutorial in the scene (tests, other scenes) every call does nothing, so a module
    /// never fails because of the tutorial.
    /// <code>
    /// IDisposable help = TutorialHelp.Show(gesture, handlebar);  ...  help.Dispose();
    /// TutorialHelp.Stop();  ...  TutorialHelp.Resume();
    /// using (TutorialHelp.Pause()) { ... }
    /// </code>
    /// </summary>
    public static class TutorialHelp
    {
        private static ITutorialHelpService _service;

        public static bool IsAvailable => Service != null;
        public static bool IsObserving => Service?.Observing ?? false;

        /// <summary>Shows the gesture on these targets until the returned handle is disposed. Works while stopped.</summary>
        public static IDisposable Show(TutorialGestureSO gesture, params Transform[] targets) =>
            Service?.Show(gesture, targets ?? Array.Empty<Transform>()) ?? NoHelp.Instance;

        /// <summary>From here on the tutorial does not observe (no first-time or stuck help).</summary>
        public static void Stop() => Service?.Stop();

        /// <summary>The tutorial observes again; the stuck count starts over.</summary>
        public static void Resume() => Service?.Resume();

        /// <summary>Stop now, Resume when the returned handle is disposed (for a using block).</summary>
        public static IDisposable Pause()
        {
            ITutorialHelpService service = Service;
            if (service == null)
                return NoHelp.Instance;
            service.Stop();
            return new Resumer(service);
        }

        internal static void Register(ITutorialHelpService service)
        {
            _service = service;
            ServiceLocator.Instance.RegisterService(service);
        }

        internal static void Unregister(ITutorialHelpService service)
        {
            if (!ReferenceEquals(_service, service))
                return;
            _service = null;
            ServiceLocator.Instance.TryDeregisterService(service);
        }

        private static ITutorialHelpService Service
        {
            get
            {
                if (_service is UnityEngine.Object unityObject && unityObject == null)
                    _service = null; // destroyed with its scene
                return _service;
            }
        }

        private sealed class NoHelp : IDisposable
        {
            public static readonly NoHelp Instance = new NoHelp();
            public void Dispose() { }
        }

        private sealed class Resumer : IDisposable
        {
            private ITutorialHelpService _service;
            public Resumer(ITutorialHelpService service) => _service = service;

            public void Dispose()
            {
                ITutorialHelpService service = _service;
                _service = null;
                service?.Resume();
            }
        }
    }
}
