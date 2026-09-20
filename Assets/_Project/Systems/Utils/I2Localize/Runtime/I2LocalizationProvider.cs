using Cachacos;
using I2.Loc;
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
