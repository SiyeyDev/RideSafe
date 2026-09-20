using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class RandomSFx : MonoBehaviour
{
    [SerializeField] private RandomSound _randomSound;
    [SerializeField] private float _startDelay = 5;
    [SerializeField] private float _tickRate = 5;

    private void Awake()
    {
        _randomSound.Initialize();
    }
    private void Start()
    {
        if (_startDelay > 0)
            this.CoroutineExecuteActionAfter(PlayRandomSound, _startDelay);
        else
            PlayRandomSound();
    }

    private void PlayRandomSound()
    {
        _randomSound.Play();
        this.CoroutineExecuteActionAfter(PlayRandomSound, _tickRate);
    }

    [Serializable]
    private struct RandomSound
    {
        [SerializeField] private SoundData[] _sounds;

        [SerializeField, ReadOnly] private List<SoundData> _availableSounds;
        private SoundData _soundPlayed;
        private System.Random _random;
        private bool _firstPlayed;

        public void Initialize()
        {
            _random = new System.Random();
            _availableSounds = new List<SoundData>(_sounds);
            _firstPlayed = false;
        }
        public void Play()
        {
            int index = _random.Next(0, _availableSounds.Count);
            if (_firstPlayed)
                _availableSounds.Add(_soundPlayed);
            _firstPlayed = true;
            _soundPlayed = _availableSounds[index];
            _soundPlayed.Play();
            _availableSounds.Remove(_soundPlayed);
        }

        [Serializable]
        private struct SoundData
        {
            [SerializeField] private AudioSource _audisoSource;
            [SerializeField] private AudioClip _audioClip;

            public void Play()
            {
                _audisoSource.clip = _audioClip;
                _audisoSource.Play();
            }
        }
    }
}
