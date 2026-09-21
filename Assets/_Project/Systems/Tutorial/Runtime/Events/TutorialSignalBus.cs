using System;
using System.Collections.Generic;
using UnityEngine;

namespace RideSafe.Tutorial
{
    /// <summary>
    /// Lightweight named-signal bus for gameplay-to-tutorial messages.
    /// <para>
    /// A module bridge raises a signal such as "module01.panel.opened" and a step waits on
    /// it with <see cref="CustomEventValidator"/>. The tutorial never references the
    /// module and the module never references the tutorial, which is what keeps the
    /// dependency acyclic.
    /// </para>
    /// <para>
    /// This is a plain object owned by the tutorial session rather than a ScriptableObject
    /// channel on purpose: an asset-backed channel survives scene unload with its
    /// subscriber list intact, which is exactly the duplicate-listener trap this sprint is
    /// trying to avoid. Register it through <see cref="TutorialSessionScope"/>.
    /// </para>
    /// </summary>
    public sealed class TutorialSignalBus
    {
        private readonly Dictionary<string, Action<string>> _handlers =
            new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> _latched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Raised for every signal, for logging and analytics.</summary>
        public event Action<string> SignalRaised;

        public void Raise(string signal)
        {
            if (string.IsNullOrWhiteSpace(signal))
                return;

            string key = signal.Trim();
            _latched.Add(key);

            Action<string> handlers;
            if (_handlers.TryGetValue(key, out handlers) && handlers != null)
                handlers.Invoke(key);

            Action<string> global = SignalRaised;
            if (global != null)
                global.Invoke(key);
        }

        public void Subscribe(string signal, Action<string> handler)
        {
            if (string.IsNullOrWhiteSpace(signal) || handler == null)
                return;

            string key = signal.Trim();
            Action<string> existing;
            _handlers.TryGetValue(key, out existing);
            _handlers[key] = existing + handler;
        }

        public void Unsubscribe(string signal, Action<string> handler)
        {
            if (string.IsNullOrWhiteSpace(signal) || handler == null)
                return;

            string key = signal.Trim();
            Action<string> existing;
            if (!_handlers.TryGetValue(key, out existing) || existing == null)
                return;

            Action<string> reduced = existing - handler;
            if (reduced == null)
                _handlers.Remove(key);
            else
                _handlers[key] = reduced;
        }

        /// <summary>
        /// True if the signal fired at any point since the last <see cref="ClearLatched"/>.
        /// Lets a step accept a signal that arrived a frame or two before it armed.
        /// </summary>
        public bool WasRaised(string signal) =>
            !string.IsNullOrWhiteSpace(signal) && _latched.Contains(signal.Trim());

        public void ClearLatched() => _latched.Clear();

        public void Clear()
        {
            _handlers.Clear();
            _latched.Clear();
            SignalRaised = null;
            Debug.Log("[Tutorial] Signal bus cleared.");
        }
    }
}
