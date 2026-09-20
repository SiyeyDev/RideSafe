using UnityEngine;

public abstract class BaseOnAudioPlay : MonoBehaviour
{
    [Header("Base Settings")]
    [SerializeField] protected MonoBehaviourChannelPlayAudio onAudioPlay;
    protected virtual void Awake()
    {
        if (onAudioPlay == null)
            onAudioPlay = gameObject.GetComponent<MonoBehaviourChannelPlayAudio>();
        if (onAudioPlay == null)
            throw new System.Exception($"GameObject: {gameObject.name} doesn't have a {typeof(MonoBehaviourChannelPlayAudio)} assigned");
    }

    private void OnEnable() => AddDelegates();

    private void OnDisable() => RemoveDelegates();

    private void OnDestroy() => RemoveDelegates();
    #region BaseOnAudioPlay Methods
    protected abstract void AudioChange(AudioSource audioSource, bool audioState);
    protected virtual void AddDelegates() => onAudioPlay.OnAudioStateChange += AudioChange;
    protected virtual void RemoveDelegates() => onAudioPlay.OnAudioStateChange -= AudioChange;
    #endregion

}
