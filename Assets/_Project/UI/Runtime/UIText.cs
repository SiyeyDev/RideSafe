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
        private static ILocalizationProvider _provider;

        /// <summary>The provider, asked for once and kept; asked again only while there is none (or it was destroyed).</summary>
        private static ILocalizationProvider Provider
        {
            get
            {
                if (_provider is UnityEngine.Object unityObject && unityObject == null)
                    _provider = null;
                if (_provider == null)
                    _provider = ServiceLocator.Instance.RequestService<ILocalizationProvider>();
                return _provider;
            }
        }

        public static string Resolve(string termOrText)
        {
            if (string.IsNullOrEmpty(termOrText))
                return termOrText;
            ILocalizationProvider localization = Provider;
            string text = localization != null ? localization.GetTranslation(termOrText) : null;
            return string.IsNullOrEmpty(text) ? termOrText : text;
        }

        /// <summary>Subscribes to language changes when a provider exists; returns it so the caller can unsubscribe.</summary>
        public static ILocalizationProvider ListenForLanguage(Action onChanged)
        {
            ILocalizationProvider localization = Provider;
            if (localization != null && onChanged != null)
                localization.LanguageChanged += onChanged;
            return localization;
        }
    }
}
