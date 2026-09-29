using System;
using System.Collections.Generic;
using RideSafe.TaskSequence;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    /// also runs automatically when the OWNING scene unloads (other additive scenes coming
    /// and going do not end the session) and on application quit.
    /// </para>
    /// </summary>
    public sealed class TutorialSessionScope : IDisposable
    {
        private readonly List<Action> _teardown = new List<Action>();
        private readonly Scene _owningScene;
        private readonly bool _hasOwningScene;
        private readonly string _label;
        private bool _disposed;

        /// <param name="owningScene">
        /// Scene whose unload ends the session, normally <c>gameObject.scene</c> of the owner.
        /// An invalid scene means the session only ends on explicit Dispose or quit.
        /// </param>
        public TutorialSessionScope(Scene owningScene, string label = "tutorial-session")
        {
            _owningScene = owningScene;
            // Captured now: an unloaded scene may no longer report IsValid() in the callback.
            _hasOwningScene = owningScene.IsValid();
            _label = string.IsNullOrWhiteSpace(label) ? "tutorial-session" : label;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
            Application.quitting += HandleQuitting;
        }

        public bool IsDisposed => _disposed;
        public int PendingTeardowns => _teardown.Count;

        /// <summary>
        /// Registers a service (instance-safe) and schedules its deregistration. A slot
        /// already owned by another live instance is left alone and nothing is scheduled.
        /// </summary>
        public TutorialSessionScope AddService<T>(T service) where T : class
        {
            if (service == null || _disposed)
                return this;

            if (ServiceRegistration.TryRegister(service))
                _teardown.Add(() => ServiceRegistration.Deregister(service));
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

            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
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

        /// <summary>Exposed for tests: the scene-unload path without loading real scenes.</summary>
        internal void HandleSceneUnloaded(Scene scene)
        {
            if (_hasOwningScene && scene.handle == _owningScene.handle)
                Dispose();
        }

        private void HandleQuitting() => Dispose();
    }
}
