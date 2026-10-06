using System;
using System.Collections.Generic;
using I2.Loc;
using RideSafe.UI;
using UnityEngine;

namespace RideSafe.Localization
{
    /// <summary>
    /// Applies the language picked in a <see cref="ChoicePanel"/> to I2 Localization.
    /// <para>
    /// Lives in the predefined assembly on purpose: I2 ships without an asmdef, so it compiles
    /// into Assembly-CSharp, which RideSafe.UI cannot reference. Keeping the bridge here lets the
    /// panels stay free of any localization dependency.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(ChoicePanel))]
    [DisallowMultipleComponent]
    public class LanguageChoiceBinder : MonoBehaviour
    {
        [Serializable]
        public struct Mapping
        {
            [Tooltip("Option id as listed in the ChoicePanel.")]
            public string OptionId;
            [Tooltip("I2 language code, e.g. 'en' or 'es-CO'.")]
            public string LanguageCode;
        }

        [SerializeField] private List<Mapping> _languages = new List<Mapping>();
        [Tooltip("Switch as soon as an option is picked. Off waits for Continue.")]
        [SerializeField] private bool _applyOnSelection = true;

        private ChoicePanel _panel;

        private void OnEnable()
        {
            if (_panel == null)
                _panel = GetComponent<ChoicePanel>();

            if (_applyOnSelection)
                _panel.onSelectionChanged.AddListener(Apply);
            _panel.onContinue.AddListener(Apply);
            PreselectCurrentLanguage();
        }

        private void OnDisable()
        {
            if (_panel == null)
                return;
            _panel.onSelectionChanged.RemoveListener(Apply);
            _panel.onContinue.RemoveListener(Apply);
        }

        /// <summary>Switches I2 to the language mapped to a panel option id.</summary>
        public void Apply(string optionId)
        {
            for (int i = 0; i < _languages.Count; i++)
            {
                if (_languages[i].OptionId != optionId)
                    continue;

                string code = _languages[i].LanguageCode;
                if (LocalizationManager.CurrentLanguageCode == code)
                    return;
                if (!LocalizationManager.GetAllLanguagesCode().Contains(code))
                {
                    Debug.LogError("[RideSafe.Localization] I2 has no language with code '" + code + "'.", this);
                    return;
                }
                LocalizationManager.CurrentLanguageCode = code;
                return;
            }
        }

        /// <summary>Marks the option matching the active language, so the panel opens in sync.</summary>
        private void PreselectCurrentLanguage()
        {
            string current = LocalizationManager.CurrentLanguageCode;
            for (int i = 0; i < _languages.Count; i++)
            {
                if (_languages[i].LanguageCode == current)
                {
                    _panel.Select(_languages[i].OptionId);
                    return;
                }
            }
        }
    }
}
