using System;
using Cachacos;

namespace RideSafe.UI
{
    /// <summary>
    /// Resolves text that panels write at runtime (selected caption, On/Off, the vehicle
    /// confirmation sentence) through the project's localization seam.
    /// <para>
    /// A value is treated as a term first and falls back to itself, so localized prefabs
    /// store terms (M0/...) while not-yet-localized ones keep working with literal text.
    /// </para>
    /// </summary>
    public static class UIText
    {
        public static string Resolve(string termOrText)
        {
            if (string.IsNullOrEmpty(termOrText))
                return termOrText;
            ILocalizationProvider localization = ServiceLocator.Instance.RequestService<ILocalizationProvider>();
            string text = localization != null ? localization.GetTranslation(termOrText) : null;
            return string.IsNullOrEmpty(text) ? termOrText : text;
        }

        /// <summary>Subscribes to language changes when a provider exists; returns it so the caller can unsubscribe.</summary>
        public static ILocalizationProvider ListenForLanguage(Action onChanged)
        {
            ILocalizationProvider localization = ServiceLocator.Instance.RequestService<ILocalizationProvider>();
            if (localization != null && onChanged != null)
                localization.LanguageChanged += onChanged;
            return localization;
        }
    }
}
