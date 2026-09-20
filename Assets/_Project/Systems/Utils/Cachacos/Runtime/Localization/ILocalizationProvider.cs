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
    }
}
