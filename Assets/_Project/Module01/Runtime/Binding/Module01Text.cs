using Cachacos;
using RideSafe.UI;
using TMPro;
using UnityEngine;
namespace RideSafe.Module01
{
    [RequireComponent(typeof(TMP_Text))]
    public class Module01Text : MonoBehaviour
    {
        [SerializeField] private string _key;
        private ILocalizationProvider _provider;
        public void Configure(string key) { _key = key; if (Application.isPlaying) Refresh(); }
        private void OnEnable() { _provider = UIText.ListenForLanguage(Refresh); Refresh(); }
        private void OnDisable() { if (_provider != null) _provider.LanguageChanged -= Refresh; }
        private void Refresh() { GetComponent<TMP_Text>().text = UIText.Resolve(_key); }
    }
}

