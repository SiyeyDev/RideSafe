using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace RideSafe.UI
{
    /// <summary>
    /// Single choice among Toggles: Language (02), Jurisdiction (03), Vehicle (04).
    /// Continue stays disabled until something is chosen. The optional confirmation bar is
    /// the Vehicle screen's explicit second step.
    /// </summary>
    [DisallowMultipleComponent]
    public class ChoicePanel : MonoBehaviour
    {
        [Serializable]
        public struct Option
        {
            public UnityEngine.UI.Toggle Toggle;
            public string Id;
            public string DisplayName;
        }

        [SerializeField] private UnityEngine.UI.ToggleGroup _group;
        [SerializeField] private List<Option> _options = new List<Option>();
        [SerializeField] private UnityEngine.UI.Button _continueButton;
        [SerializeField] private UnityEngine.UI.Button _backButton;

        [Header("Confirmation bar (optional)")]
        [SerializeField] private GameObject _confirmationBar;
        [SerializeField] private TMP_Text _confirmationText;
        [SerializeField] private string _confirmationFormat = "{0} selected —";
        [SerializeField] private UnityEngine.UI.Button _changeButton;

        public UnityEvent<string> onSelectionChanged = new UnityEvent<string>();
        public UnityEvent<string> onContinue = new UnityEvent<string>();
        public UnityEvent onBack = new UnityEvent();

        public string SelectedId { get; private set; }

        private void Awake()
        {
            foreach (Option option in _options)
            {
                if (option.Toggle == null)
                    continue;
                string id = option.Id;
                option.Toggle.onValueChanged.AddListener(isOn => HandleToggle(id, isOn));
            }
            if (_continueButton != null)
                _continueButton.onClick.AddListener(HandleContinue);
            if (_backButton != null)
                _backButton.onClick.AddListener(onBack.Invoke);
            if (_changeButton != null)
                _changeButton.onClick.AddListener(ClearSelection);
        }

        private void OnEnable()
        {
            SelectedId = null;
            foreach (Option option in _options)
            {
                if (option.Toggle != null && option.Toggle.isOn)
                    SelectedId = option.Id;
            }
            Refresh();
        }

        public void Select(string id)
        {
            foreach (Option option in _options)
            {
                if (option.Id == id && option.Toggle != null)
                {
                    option.Toggle.isOn = true;
                    return;
                }
            }
            Debug.LogWarning("[RideSafe.UI] '" + name + "' has no option '" + id + "'.", this);
        }

        public void ClearSelection()
        {
            if (_group != null)
            {
                bool previous = _group.allowSwitchOff;
                _group.allowSwitchOff = true;
                _group.SetAllTogglesOff();
                _group.allowSwitchOff = previous;
            }
            SelectedId = null;
            Refresh();
        }

        private void HandleToggle(string id, bool isOn)
        {
            if (isOn)
            {
                SelectedId = id;
                onSelectionChanged.Invoke(id);
            }
            else if (SelectedId == id)
            {
                SelectedId = null;
            }
            Refresh();
        }

        private void HandleContinue()
        {
            if (!string.IsNullOrEmpty(SelectedId))
                onContinue.Invoke(SelectedId);
        }

        private void Refresh()
        {
            bool chosen = !string.IsNullOrEmpty(SelectedId);
            if (_continueButton != null)
                _continueButton.interactable = chosen;
            if (_confirmationBar != null)
                _confirmationBar.SetActive(chosen);
            if (_confirmationText != null && chosen)
                _confirmationText.text = string.Format(_confirmationFormat, DisplayNameOf(SelectedId));
        }

        private string DisplayNameOf(string id)
        {
            foreach (Option option in _options)
            {
                if (option.Id == id)
                    return string.IsNullOrEmpty(option.DisplayName) ? id : option.DisplayName;
            }
            return id;
        }
    }
}
