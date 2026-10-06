using System;
using System.Collections.Generic;
using Autohand;
using UnityEngine;
using UnityEngine.Events;

namespace RideSafe.UI
{
    /// <summary>
    /// Root of a module's UI prefab. Owns the module's panels and shows one at a time.
    /// <para>
    /// Also keeps the world-space canvas clickable by AutoHand's XRPlayer: HandCanvasPointer
    /// hands its UI camera only to canvases that exist when it starts, so a module prefab
    /// loaded later (additive scene, runtime spawn) would ignore the ray without this.
    /// Panels need no EventSystem of their own; AutoHand's AutoInputModule provides it.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    [DisallowMultipleComponent]
    public class ModuleUI : MonoBehaviour
    {
        [Serializable]
        public struct Panel
        {
            public string Id;
            public GameObject Root;
        }

        [SerializeField] private List<Panel> _panels = new List<Panel>();
        [Tooltip("Panel shown on enable. Empty keeps the current state.")]
        [SerializeField] private string _startPanel;

        public UnityEvent<string> onPanelShown = new UnityEvent<string>();

        public string Current { get; private set; }

        private void OnEnable()
        {
            BindToHandPointer();
            if (!string.IsNullOrEmpty(_startPanel))
                Show(_startPanel);
        }

        /// <summary>Shows one panel by id and hides the rest.</summary>
        public void Show(string panelId)
        {
            bool found = false;
            for (int i = 0; i < _panels.Count; i++)
            {
                bool match = _panels[i].Id == panelId;
                found |= match;
                if (_panels[i].Root != null)
                    _panels[i].Root.SetActive(match);
            }

            if (!found)
            {
                Debug.LogError("[RideSafe.UI] Module '" + name + "' has no panel '" + panelId + "'.", this);
                return;
            }
            Current = panelId;
            onPanelShown.Invoke(panelId);
        }

        public void HideAll()
        {
            for (int i = 0; i < _panels.Count; i++)
            {
                if (_panels[i].Root != null)
                    _panels[i].Root.SetActive(false);
            }
            Current = null;
        }

        /// <summary>Returns a panel's component, e.g. Get&lt;ChecklistView&gt;("Preparation").</summary>
        public T Get<T>(string panelId) where T : Component
        {
            for (int i = 0; i < _panels.Count; i++)
            {
                if (_panels[i].Id == panelId && _panels[i].Root != null)
                    return _panels[i].Root.GetComponent<T>();
            }
            return null;
        }

        private void BindToHandPointer()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas.renderMode != RenderMode.WorldSpace || canvas.worldCamera != null)
                return;
            if (FindAnyObjectByType<HandCanvasPointer>(FindObjectsInactive.Include) == null)
                return;
            canvas.worldCamera = HandCanvasPointer.UICamera;
        }
    }
}
