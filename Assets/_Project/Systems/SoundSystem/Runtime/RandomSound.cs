using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RandomSound 
{
    [SerializeField] private SoundCommand[] _sounds;

    private List<SoundCommand> _availableSounds;
    private SoundCommand _soundPlayed;
    private System.Random _random;

    public void Initialize(MonoBehaviour caller)
    {
        _random = new System.Random();
        _availableSounds = new List<SoundCommand>(_sounds);
        foreach (SoundCommand sound in _sounds)
            sound.Initialize(caller);
    }
    public void Play()
    {
        int index = _random.Next(0, _availableSounds.Count);
        if(_soundPlayed != null)
            _availableSounds.Add(_soundPlayed);
        _soundPlayed = _availableSounds[index];
        _soundPlayed.Execute();
        _availableSounds.Remove(_soundPlayed);
    }
}
