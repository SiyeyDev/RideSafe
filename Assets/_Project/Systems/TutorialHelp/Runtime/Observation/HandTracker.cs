using System;
using System.Collections.Generic;
using RideSafe.UI;

namespace RideSafe.Help
{
    /// <summary>
    /// The dominant hand chosen on the comfort screen (shared UI, no module code), heard from
    /// <see cref="ComfortSettingsPanel.onStart"/>. Right until then.
    /// </summary>
    public sealed class HandTracker : IDisposable
    {
        private readonly List<ComfortSettingsPanel> _panels = new List<ComfortSettingsPanel>();

        public DominantHand Hand { get; private set; } = DominantHand.Right;

        public void Listen(IEnumerable<ComfortSettingsPanel> panels)
        {
            foreach (ComfortSettingsPanel panel in panels)
            {
                if (panel == null || _panels.Contains(panel))
                    continue;
                panel.onStart.AddListener(OnStart);
                _panels.Add(panel);
            }
        }

        public void Dispose()
        {
            foreach (ComfortSettingsPanel panel in _panels)
            {
                if (panel != null)
                    panel.onStart.RemoveListener(OnStart);
            }
            _panels.Clear();
        }

        private void OnStart(ComfortSettings settings) => Hand = settings.Hand;
    }
}
