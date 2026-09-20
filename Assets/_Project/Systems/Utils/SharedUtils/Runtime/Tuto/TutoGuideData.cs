using Cachacos;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "SO_Guide", menuName = "System/Tuto/Guide", order = 51)]
public class TutoGuideData : ScriptableObject
{
    [ValueDropdown("GetTerms")]
    [SerializeField] private string _tutoKey;
    [SerializeField] private VideoClip _videoClip;

    public virtual string GetText()
    {
        ILocalizationProvider localization = ServiceLocator.Instance.RequestService<ILocalizationProvider>();
        return localization == null ? _tutoKey : localization.GetTranslation(_tutoKey);
    }
    public virtual VideoClip GetClip() => _videoClip;

#if UNITY_EDITOR
    protected IEnumerable<string> GetTerms()
    {
        ILocalizationProvider localization = ServiceLocator.Instance.RequestService<ILocalizationProvider>();
        return localization == null ? new List<string>() : localization.GetTerms();
    }
#endif
}
