using Cachacos;
using I2.Loc;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Binds I2 Localization to the core <see cref="ILocalizationProvider"/> seam.
/// Lives outside the Cachacos assembly so the core stays independent of I2.
/// </summary>
public class I2LocalizationProvider : ILocalizationProvider
{
    public string GetTranslation(string key) => LocalizationManager.GetTranslation(key);
    public IEnumerable<string> GetTerms() => LocalizationManager.GetTermsList();

    /// <summary>Sets I2's language (I2 remembers it in PlayerPrefs). Exact code first, then a regional match.</summary>
    public bool SetLanguage(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return false;
        string language = LocalizationManager.GetLanguageFromCode(languageCode);
        if (string.IsNullOrEmpty(language))
            language = LocalizationManager.GetLanguageFromCode(languageCode, exactMatch: false);
        if (string.IsNullOrEmpty(language))
            return false;
        LocalizationManager.CurrentLanguage = language;
        return true;
    }

    // Forwarded, not cached: I2 clears OnLocalizeEvent when play mode exits.
    public event Action LanguageChanged
    {
        add { LocalizationManager.OnLocalizeEvent += value.Invoke; }
        remove { LocalizationManager.OnLocalizeEvent -= value.Invoke; }
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        if (ServiceLocator.Instance.RequestService<ILocalizationProvider>() != null)
            return;
        ServiceLocator.Instance.RegisterService<ILocalizationProvider>(new I2LocalizationProvider());
    }
}
