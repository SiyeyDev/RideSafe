using System;
using UnityEngine;

/// <note>
/// The <see cref="SoundCommandPropertyDrawer"/> class is used to draw the `SoundCommand` fields in the Inspector using a custom property drawer.
/// Any changes to the serialized fields will not be automatically reflected in the Inspector unless the 
/// <see cref="SoundCommandPropertyDrawer"/> is updated accordingly. If you add or modify any variables in `SoundCommand`,
/// ensure the <see cref="SoundCommandPropertyDrawer"/> logic is also updated to properly display and handle them in the Inspector.
/// </note>
[Serializable]
public class SoundCommand : ICommand
{
    [SerializeField] private float _delay;
    [SerializeField] private AudioSource _source;
    [SerializeField] private string _clipName;
    private MonoBehaviour _caller;
    private Coroutine _coroutine;
    private AudioClip _clip;
#if UNITY_EDITOR
    [SerializeField] private string _routhFolder;
#endif
    public SoundCommand Initialize(MonoBehaviour caller)
    {
        _caller = caller;
        if (_clip == null)
            _clip = AudioTranslateBridge.GetAudioClip(_clipName, false);
        return this;
    }
    #region ICommand Methods
    public void Execute(Action onComplete = null)
    {
        if (_delay == 0)
        {
            Play(onComplete);
            return;
        }
        _coroutine = _caller.CoroutineExecuteActionAfter(() => Play(onComplete), _delay);
    }
    public void Exit()
    {
        if (_source == null)
            return;
        if (_coroutine != null)
            _caller.StopCoroutine(_coroutine);

        _source.Stop();
    }
    #endregion
    private void Play(Action onComplete)
    {
        _source.clip = _clip;
        _source.Play();
        Debug.Log($"SOund command play {_clip.name}");
        if (onComplete != null)
            _coroutine = _caller.CoroutineExecuteActionAfter(onComplete, _clip.length);
    }

    public float GetDuration()
    {
        if (_clip == null)
            _clip = AudioTranslateBridge.GetAudioClip(_clipName, false);
        return _delay + _clip.length;
    }
    public AudioClip GetClip() => _clip;
}
