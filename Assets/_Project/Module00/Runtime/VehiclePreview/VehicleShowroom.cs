using System;
using System.Collections.Generic;
using RideSafe.UI;
using UnityEngine;

namespace RideSafe.Module00
{
    /// <summary>
    /// Screen 04 shows the vehicles under their name cards. In RideSafe_Garaje_v5 the real
    /// vehicles wait inside the garage for module 1, so module 0 has its own display copies
    /// (each on a copy of the particle platform). When the Vehicle screen opens, each slot is
    /// put on the ground on the learner's line of sight through its card, turned to the
    /// learner, and the stage appears; it hides when the screen closes. Placed once per
    /// opening, so it does not ride on head movement. VehiclePreviewController turns the chosen one.
    /// </summary>
    [AddComponentMenu("RideSafe/Module 00/Vehicle Showroom")]
    public class VehicleShowroom : MonoBehaviour
    {
        [Serializable]
        public class Slot
        {
            [Tooltip("Positioned under the card; holds the display vehicle and its platform.")]
            public Transform Anchor;
            [Tooltip("Name of the card on the Vehicle screen, e.g. Option_ebike.")]
            public string Label;
        }

        [SerializeField] private ModuleUI _module;
        [SerializeField] private string _vehiclePanelId = "Vehicle";
        [Tooltip("Parent of the slots. Shown only while the Vehicle screen is open.")]
        [SerializeField] private GameObject _stage;
        [SerializeField] private List<Slot> _slots = new List<Slot>();
        [Tooltip("Horizontal metres from the learner to the vehicles.")]
        [SerializeField, Min(0.5f)] private float _distance = 3.5f;
        [Tooltip("World height of the ground the vehicles stand on.")]
        [SerializeField] private float _groundHeight;

        private GameObject _panel;
        private bool _open;

        internal void Configure(GameObject panel, GameObject stage, float distance, float groundHeight,
                                params (Transform Anchor, string Label)[] slots)
        {
            _panel = panel;
            _stage = stage;
            _distance = distance;
            _groundHeight = groundHeight;
            _slots.Clear();
            foreach (var slot in slots)
                _slots.Add(new Slot { Anchor = slot.Anchor, Label = slot.Label });
            _open = false;
        }

        private void Start()
        {
            if (_panel == null && _module != null)
            {
                RectTransform panel = _module.Get<RectTransform>(_vehiclePanelId);
                _panel = panel != null ? panel.gameObject : null;
            }
            if (_panel == null)
                Debug.LogError("[Module00] Vehicle showroom found no '" + _vehiclePanelId + "' screen.", this);
            if (_stage != null)
                _stage.SetActive(false);
        }

        private void Update()
        {
            Camera camera = Camera.main;
            if (camera != null)
                Tick(new Pose(camera.transform.position, camera.transform.rotation));
        }

        internal void Tick(Pose viewer)
        {
            bool open = _panel != null && _panel.activeInHierarchy;
            if (open && !_open)
                Place(viewer);
            if (open != _open && _stage != null)
                _stage.SetActive(open);
            _open = open;
        }

        private void Place(Pose viewer)
        {
            foreach (Slot slot in _slots)
            {
                Transform label = FindLabel(slot.Label);
                if (slot.Anchor == null || label == null)
                    continue;
                // The card's visible centre: the kit pivots cards top-left, and aiming at their
                // position put each vehicle under its card's corner instead of under its name.
                Vector3 centre = label is RectTransform card ? card.TransformPoint(card.rect.center) : label.position;
                Vector3 toLabel = centre - viewer.position;
                toLabel.y = 0f;
                if (toLabel.sqrMagnitude < 1e-6f)
                    continue;
                Vector3 direction = toLabel.normalized;
                Vector3 ground = new Vector3(viewer.position.x, _groundHeight, viewer.position.z) + direction * _distance;
                slot.Anchor.SetPositionAndRotation(ground, Quaternion.LookRotation(direction, Vector3.up));
            }
        }

        private Transform FindLabel(string name)
        {
            foreach (Transform t in _panel.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                    return t;
            }
            return null;
        }
    }
}
