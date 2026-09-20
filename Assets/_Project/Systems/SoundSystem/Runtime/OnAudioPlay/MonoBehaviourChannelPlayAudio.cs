using System;
using UnityEngine;
using UnityEngine.Events;

public class MonoBehaviourChannelPlayAudio : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private UnityEvent<bool> _onAudioStateChange;
    [SerializeField] private UnityEvent _onAudioPlay;
    [SerializeField] private UnityEvent _onAudioStop;
    public event Action<AudioSource, bool> OnAudioStateChange;
    public event Action<AudioSource, AudioClip, AudioClip> OnAudioClipChange;
    private bool _played;
    private AudioClip _lastclip;
    private void Update()
    {
        if (!_audioSource.isPlaying)
        {
            AudioStopped();
            return;
        }
        AudioPlayed();
        if (_lastclip == _audioSource.clip)
            return;
        _lastclip = _audioSource.clip;
        OnAudioClipChange?.Invoke(_audioSource, _lastclip, _audioSource.clip);
    }
    private void AudioStopped()
    {
        if (!_played)
            return;
        Debug.Log($"Stop audio {_audioSource.clip.name} in {gameObject.name}");
        _played = false;
        _onAudioStateChange?.Invoke( false);
        OnAudioStateChange?.Invoke(_audioSource, false);
        _onAudioStop?.Invoke();
    }
    private void AudioPlayed()
    {
        if (_played)
            return;
        Debug.Log($"Play audio {_audioSource.clip.name} in {gameObject.name}");
        _played = true;
        _onAudioStateChange?.Invoke( true);
        OnAudioStateChange?.Invoke(_audioSource, true);
        _onAudioPlay?.Invoke();
    }
}
