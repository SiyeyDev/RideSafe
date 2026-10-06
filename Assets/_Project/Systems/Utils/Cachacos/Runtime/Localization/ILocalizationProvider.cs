using System;
using System.Collections.Generic;

namespace Cachacos
{
    /// <summary>
    /// Seam that lets core content resolve localized text without binding to a specific
    /// localization asset. Register an implementation with <see cref="ServiceLocator"/>;
    /// when none is registered, consumers fall back to the raw key.
    /// </summary>
    public interface ILocalizationProvider
    {
        /// <summary>
        /// Returns the translation for <paramref name="key"/> in the active language.
        /// </summary>
        string GetTranslation(string key);
        /// <summary>
        /// Returns every term available in the active source. Used to populate editor dropdowns.
        /// </summary>
        IEnumerable<string> GetTerms();
        /// <summary>
        /// Switches the active language by code (e.g. "en", "es"). A bare code matches a
        /// regional variant ("es" -> "es-CO"). Returns false when no language matches.
        /// </summary>
        bool SetLanguage(string languageCode);
        /// <summary>Raised after the active language changed and texts were re-localized.</summary>
        event Action LanguageChanged;
    }
}
