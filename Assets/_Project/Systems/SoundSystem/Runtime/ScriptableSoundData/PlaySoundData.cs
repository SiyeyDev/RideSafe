using Sirenix.OdinInspector;
using UnityEngine;

public class PlaySoundData : MonoBehaviour
{
    [SerializeField] private bool _playOnEnabled;
    [InlineEditor][SerializeField] private ScriptableSoundData _dataToPlay;
    [Required]
    [SerializeField] private AudioSource _audioSource;
    private void Awake()
    {
        _audioSource.playOnAwake = false;
    }
    private void OnEnable()
    {
        if (_playOnEnabled)
            PlayAudio();
    }
    private void OnDisabled()
    {
        if (_playOnEnabled)
            StopAudio();
    }
    [Button("PlayAudio")]
    public void PlayAudio() => _dataToPlay.PlayAudio(_audioSource);
    [Button("StopAudio")]
    public void StopAudio() => _audioSource.Stop();
    public void PlayAudioAnimation(ScriptableSoundData _setClip) => _setClip.PlayAudio(_audioSource);
}
