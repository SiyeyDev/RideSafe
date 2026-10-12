using RideSafe.UI;
using UnityEngine;

namespace RideSafe.Module00
{
    /// <summary>
    /// Glue between the Vehicle panel of PF_Module00_UI and <see cref="VehiclePreviewController"/>:
    /// a choice presents that vehicle, leaving the Vehicle screen ends the presentation.
    /// <para>
    /// Listens to the panel's own events, the same ones the tutorial's entities observe, so
    /// neither the tutorial nor the preview knows the other exists. The panel is found through
    /// <see cref="ModuleUI.Get{T}"/> by id, not by a scene override on the generated prefab.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/Module 00/Vehicle Panel Preview Binding")]
    public class VehiclePanelPreviewBinding : MonoBehaviour
    {
        [Tooltip("PF_Module00_UI instance whose Vehicle panel drives the preview.")]
        [SerializeField] private ModuleUI _module;
        [SerializeField] private string _vehiclePanelId = "Vehicle";
        [SerializeField] private VehiclePreviewController _preview;

        private ChoicePanel _panel;

        protected virtual void OnEnable() => Subscribe();

        protected virtual void OnDisable() => Unsubscribe();

        internal void Subscribe()
        {
            if (_panel != null)
                return;
            _panel = _module != null ? _module.Get<ChoicePanel>(_vehiclePanelId) : null;
            if (_panel == null || _preview == null)
            {
                Debug.LogError("[Module00] Vehicle preview binding needs a ModuleUI with a '" + _vehiclePanelId +
                               "' ChoicePanel and a VehiclePreviewController.", this);
                _panel = null;
                return;
            }
            _panel.onSelectionChanged.AddListener(_preview.Select);
            _module.onPanelShown.AddListener(HandlePanelShown);
        }

        internal void Unsubscribe()
        {
            if (_panel == null)
                return;
            _panel.onSelectionChanged.RemoveListener(_preview.Select);
            _module.onPanelShown.RemoveListener(HandlePanelShown);
            _panel = null;
        }

        private void HandlePanelShown(string panelId)
        {
            if (panelId != _vehiclePanelId)
                _preview.ClearSelection();
        }
    }
}
