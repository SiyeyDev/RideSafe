using System;
using System.Collections.Generic;
using RideSafe.TaskSequence;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using EntityId = RideSafe.TaskSequence.EntityId;

namespace RideSafe.Tutorial.AutoHand
{
    /// <summary>
    /// Task entity for a world-space uGUI element operated with XRPlayer's UI pointer
    /// (AutoHand <c>HandCanvasPointer</c> + <c>AutoInputModule</c>).
    /// <para>
    /// No raycasting or hover logic lives here: AutoHand's input module already raycasts
    /// the canvas and delivers standard EventSystem events. This component only translates
    /// them into the tutorial vocabulary:
    /// </para>
    /// <list type="bullet">
    /// <item>focus = pointer enter/exit (any AutoHand pointer, either hand);</item>
    /// <item>select/deselect = the Toggle turning on/off, or "any toggle on" for a ToggleGroup;</item>
    /// <item>confirm = the Button being clicked.</item>
    /// </list>
    /// <para>
    /// Visual reaction to focus and selection is the Selectable's own transition, so it
    /// is authored like any other RideSafe UI.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/Tutorial/AutoHand UI Task Entity")]
    public class AutoHandUITaskEntity : TaskEntity,
        IFocusableTaskEntity, ISelectableTaskEntity, IConfirmableTaskEntity,
        IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("Single toggle: selected while it is on.")]
        [SerializeField] private Toggle _toggle;

        [Tooltip("Group of options: selected while any of its toggles is on.")]
        [SerializeField] private ToggleGroup _toggleGroup;

        [Tooltip("Button whose click confirms.")]
        [SerializeField] private Button _button;

        // One entry per pointer currently inside this element; AutoHand gives each hand
        // its own PointerEventData.
        private readonly HashSet<PointerEventData> _pointersInside = new HashSet<PointerEventData>();
        private readonly List<Toggle> _groupToggles = new List<Toggle>();
        private bool _wasSelected;

        public event Action<ITaskEntity, bool> FocusChanged;
        public event Action<ITaskEntity, bool> SelectionChanged;
        public event Action<ITaskEntity> SelectionConfirmed;

        public bool IsFocused => _pointersInside.Count > 0;

        public bool IsSelected
        {
            get
            {
                if (_toggle != null)
                    return _toggle.isOn;
                return _toggleGroup != null && _toggleGroup.AnyTogglesOn();
            }
        }

        /// <summary>
        /// Runtime setup for UI generated elsewhere (e.g. a regenerated prefab), so the
        /// integration never depends on scene overrides. Call on an inactive object, then
        /// <see cref="TaskEntity.EnsureRegistered"/>.
        /// </summary>
        public void Configure(EntityId id, Toggle toggle, ToggleGroup toggleGroup, Button button)
        {
            SetEntityId(id);
            _toggle = toggle;
            _toggleGroup = toggleGroup;
            _button = button;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (_toggle != null)
                _toggle.onValueChanged.AddListener(HandleToggleChanged);

            if (_toggleGroup != null)
            {
                _groupToggles.Clear();
                foreach (Toggle toggle in _toggleGroup.GetComponentsInChildren<Toggle>(true))
                {
                    if (toggle.group != _toggleGroup)
                        continue;
                    _groupToggles.Add(toggle);
                    toggle.onValueChanged.AddListener(HandleToggleChanged);
                }
            }

            if (_button != null)
                _button.onClick.AddListener(HandleClicked);

            _wasSelected = IsSelected;
        }

        protected virtual void OnDisable()
        {
            if (_toggle != null)
                _toggle.onValueChanged.RemoveListener(HandleToggleChanged);
            foreach (Toggle toggle in _groupToggles)
            {
                if (toggle != null)
                    toggle.onValueChanged.RemoveListener(HandleToggleChanged);
            }
            _groupToggles.Clear();
            if (_button != null)
                _button.onClick.RemoveListener(HandleClicked);

            bool wasFocused = IsFocused;
            _pointersInside.Clear();
            if (wasFocused)
                FocusChanged?.Invoke(this, false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            bool wasFocused = IsFocused;
            _pointersInside.Add(eventData);
            if (!wasFocused)
                FocusChanged?.Invoke(this, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_pointersInside.Remove(eventData) || IsFocused)
                return;
            FocusChanged?.Invoke(this, false);
        }

        private void HandleToggleChanged(bool _)
        {
            // Raise on the entity's state, not the raw toggle: switching between two
            // options in a group keeps the group selected and must not look like a deselect.
            bool selected = IsSelected;
            if (selected == _wasSelected)
                return;
            _wasSelected = selected;
            SelectionChanged?.Invoke(this, selected);
        }

        private void HandleClicked() => SelectionConfirmed?.Invoke(this);

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (Application.isPlaying)
                return;
            if (_toggle == null && _toggleGroup == null && _button == null)
                Debug.LogWarning("[Tutorial] '" + name + "' AutoHandUITaskEntity has no Toggle, ToggleGroup or Button; " +
                                 "it can only be focused.", this);
        }
#endif

        private void Reset()
        {
            _toggle = GetComponent<Toggle>();
            _toggleGroup = GetComponent<ToggleGroup>();
            _button = GetComponent<Button>();
        }
    }
}
