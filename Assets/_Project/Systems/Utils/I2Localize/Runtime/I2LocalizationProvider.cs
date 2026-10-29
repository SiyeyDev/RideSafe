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
    public string CurrentLanguage => LocalizationManager.CurrentLanguage;
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
    // The I2 callback made for each listener is kept so `remove` takes out that same one:
    // `value.Invoke` wraps the Action instance, and the `-= Refresh` of an unsubscribe brings a
    // new instance, so removing a fresh `value.Invoke` matched nothing and every listener stayed
    // in I2 — destroyed Module01Text included (MissingReferenceException on the next LocalizeAll).
    private readonly Dictionary<Action, List<LocalizationManager.OnLocalizeCallback>> _forwarded =
        new Dictionary<Action, List<LocalizationManager.OnLocalizeCallback>>();

    public event Action LanguageChanged
    {
        add
        {
            if (value == null)
                return;
            LocalizationManager.OnLocalizeCallback callback = value.Invoke;
            if (!_forwarded.TryGetValue(value, out List<LocalizationManager.OnLocalizeCallback> callbacks))
                _forwarded[value] = callbacks = new List<LocalizationManager.OnLocalizeCallback>();
            callbacks.Add(callback);
            LocalizationManager.OnLocalizeEvent += callback;
        }
        remove
        {
            if (value == null || !_forwarded.TryGetValue(value, out List<LocalizationManager.OnLocalizeCallback> callbacks))
                return;
            LocalizationManager.OnLocalizeEvent -= callbacks[callbacks.Count - 1];
            callbacks.RemoveAt(callbacks.Count - 1);
            if (callbacks.Count == 0)
                _forwarded.Remove(value);
        }
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
