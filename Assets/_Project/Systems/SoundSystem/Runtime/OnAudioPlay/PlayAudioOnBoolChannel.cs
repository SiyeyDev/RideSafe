using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayAudioOnBoolChannel : MonoBehaviour
{
    [SerializeField] private BoolEventChannelSO _boolChannel;
    [SerializeField] private AudioSource _audioSource;

    private void Awake()
    {
        if (_audioSource == null)
            _audioSource = gameObject.GetComponent<AudioSource>();
    }
    private void OnEnable() => _boolChannel.OnEventRaised += ChangeAudioState;

    private void OnDisable() => _boolChannel.OnEventRaised -= ChangeAudioState;
    private void ChangeAudioState(bool state)
    {
        if (state == _audioSource.isPlaying)
            return;
        if (state)
            _audioSource.Play();
        else
            _audioSource.Stop();
    }

}
