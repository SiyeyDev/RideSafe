using UnityEngine;

[CreateAssetMenu(fileName = "SO_FadeSettings", menuName = "System/UI/Fade Settings Data", order = 51)]
public class FadeSettingsData : ScriptableObject
{
    [SerializeField] private FadeSettings _fadeSettings;
    public FadeSettings FadeSettings => _fadeSettings;
}
