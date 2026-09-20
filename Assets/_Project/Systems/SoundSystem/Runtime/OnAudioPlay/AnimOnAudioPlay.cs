using Cachacos;
using UnityEngine;

[AddComponentMenu("OnAudio/AnimOnAudioPlay")]
public class AnimOnAudioPlay : BaseOnAudioPlay
{
    [Header("Anim Settings")]
    [SerializeField] private Animator _animator;
    [SelectAnimatorParameter][SerializeField] private int _animationHash;
    protected override void AudioChange(AudioSource audioSource, bool audioState) => _animator.SetBool(_animationHash, audioState);

}
