using System;
using System.Collections.Generic;
using I2.Loc;
using RideSafe.UI;
using UnityEngine;

namespace RideSafe.Localization
{
    /// <summary>
    /// Localizes the menu strings that a view writes at runtime, which an I2 Localize component
    /// cannot: it would set the text once and the view would overwrite it on the next state change.
    /// <para>
    /// Lives in the predefined assembly because I2 ships without an asmdef. It re-applies on every
    /// language change, so switching language updates these strings like any other.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class MenuRuntimeTextLocalizer : MonoBehaviour
    {
        [Serializable]
        public struct OptionName
        {
            [Tooltip("Option id as listed in the ChoicePanel.")]
            public string OptionId;
            public string Term;
        }

        [Header("Confirmation line")]
        [Tooltip("Panel whose confirmation reads e.g. 'E-bike Selected'.")]
        [SerializeField] private ChoicePanel _confirmationPanel;
        [Tooltip("Term for that line. Include {0} to place the choice yourself; without it the choice goes first.")]
        [SerializeField] private string _confirmationTerm = "Menu/Menu_Selected";
        [Tooltip("Name each option shows in that line. Without this it falls back to the authored name, which is not localized.")]
        [SerializeField] private List<OptionName> _optionNames = new List<OptionName>();

        [Header("Toggle state caption")]
        [Tooltip("Switch whose caption reads On / Off.")]
        [SerializeField] private SelectableStyle _stateToggle;
        [SerializeField] private string _onTerm = "Menu/Menu_Subtitulos1";
        [SerializeField] private string _offTerm = "Menu/Menu_Subtitulos2";

        private void OnEnable()
        {
            LocalizationManager.OnLocalizeEvent += Apply;
            Apply();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLocalizeEvent -= Apply;
        }

        private void Apply()
        {
            if (_confirmationPanel != null)
            {
                for (int i = 0; i < _optionNames.Count; i++)
                {
                    string name = Translate(_optionNames[i].Term);
                    if (name != null)
                        _confirmationPanel.SetDisplayName(_optionNames[i].OptionId, name);
                }

                string line = Translate(_confirmationTerm);
                if (line != null)
                    _confirmationPanel.ConfirmationFormat = line.Contains("{0}") ? line : "{0} " + line;
            }

            if (_stateToggle != null)
            {
                string on = Translate(_onTerm);
                string off = Translate(_offTerm);
                if (on != null || off != null)
                    _stateToggle.SetStateText(on, off);
            }
        }

        /// <summary>Null when the term is blank or missing, so the authored text is left alone.</summary>
        private string Translate(string term)
        {
            if (string.IsNullOrEmpty(term))
                return null;
            string translation = LocalizationManager.GetTranslation(term);
            if (string.IsNullOrEmpty(translation))
            {
                Debug.LogWarning("[RideSafe.Localization] I2 has no term '" + term + "'.", this);
                return null;
            }
            return translation;
        }
    }
}
