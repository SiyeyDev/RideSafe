using System;
using System.Collections.Generic;
using Cachacos;
using UnityEngine;

namespace RideSafe.Tutorial
{
    /// <summary>
    /// Deterministic teardown for one tutorial session.
    /// <para>
    /// This exists because of two concrete hazards in the existing codebase:
    /// <c>ServiceLocator</c> is a static singleton that never clears, and
    /// <c>BaseEventChannelSO</c> exposes a raw <c>Action</c> on an asset that outlives the
    /// scene. Either one turns a forgotten unsubscribe into duplicated listeners on the
    /// next scene load (CASE 09).
    /// </para>
    /// <para>
    /// Anything registered here is undone in reverse order on <see cref="Dispose"/>, which
    /// also runs automatically on scene unload and on application quit.
    /// </para>
    /// </summary>
    public sealed class TutorialSessionScope : IDisposable
    {
        private readonly List<Action> _teardown = new List<Action>();
        private readonly string _label;
        private bool _disposed;

        public TutorialSessionScope(string label = "tutorial-session")
        {
            _label = string.IsNullOrWhiteSpace(label) ? "tutorial-session" : label;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += HandleSceneUnloaded;
            Application.quitting += HandleQuitting;
        }

        public bool IsDisposed => _disposed;
        public int PendingTeardowns => _teardown.Count;

        /// <summary>Registers a service and schedules its deregistration.</summary>
        public TutorialSessionScope AddService<T>(T service) where T : class
        {
            if (service == null || _disposed)
                return this;

            ServiceLocator.Instance.RegisterService<T>(service);
            _teardown.Add(() => ServiceLocator.Instance.TryDeregisterService<T>(service));
            return this;
        }

        /// <summary>
        /// Subscribes and schedules the matching unsubscribe. Use this instead of a bare
        /// <c>+=</c> anywhere a tutorial object listens to something that outlives it.
        /// </summary>
        public TutorialSessionScope AddSubscription(Action subscribe, Action unsubscribe)
        {
            if (_disposed)
                return this;

            if (subscribe != null)
                subscribe();
            if (unsubscribe != null)
                _teardown.Add(unsubscribe);
            return this;
        }

        /// <summary>Schedules arbitrary cleanup (clearing a registry, stopping audio).</summary>
        public TutorialSessionScope AddCleanup(Action cleanup)
        {
            if (cleanup != null && !_disposed)
                _teardown.Add(cleanup);
            return this;
        }

        /// <summary>
        /// Runs every teardown in reverse registration order. Safe to call twice; a
        /// throwing teardown is logged and does not prevent the rest from running.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            Application.quitting -= HandleQuitting;

            int count = _teardown.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                try
                {
                    Action action = _teardown[i];
                    if (action != null)
                        action.Invoke();
                }
                catch (Exception exception)
                {
                    Debug.LogError("[Tutorial] Teardown " + i + " of scope '" + _label + "' threw: " + exception);
                }
            }
            _teardown.Clear();

            Debug.Log("[Tutorial] Session scope '" + _label + "' disposed (" + count + " teardowns).");
        }

        private void HandleSceneUnloaded(UnityEngine.SceneManagement.Scene scene) => Dispose();

        private void HandleQuitting() => Dispose();
    }
}
