using Sirenix.OdinInspector;
using System;
using UnityEngine;

public class PlaySounds : MonoBehaviour
{
    [SerializeField] private bool _useDialogueStepCommand;
    [SerializeField] private bool _onlyOnePlay;
    [HideIf("_useDialogueStepCommand")][SerializeField] private Sound[] _audioClips;
    [ShowIf("_useDialogueStepCommand")][SerializeField] private DialogueStepCommand[] _soundCommands;
    private Vector2 range;
    private bool[] audioPlayed;
    private void Start()
    {
        int lengt = _audioClips.Length;
        if (_useDialogueStepCommand)
        {
            foreach (DialogueStepCommand soundCommand in _soundCommands)
                soundCommand.Initialize();
            lengt = _soundCommands.Length;
        }
        range = new Vector2(0, lengt - 1);
        audioPlayed = new bool[lengt];
    }


    public void PlayAudio(int index)
    {
        if (audioPlayed[index] && _onlyOnePlay)
            return;
        if (!index.InRange(range))
            throw new System.Exception($"{gameObject.name} does not have {index} in soundCommands");
        audioPlayed[index] = true;
        if (_useDialogueStepCommand)
        {
            _soundCommands[index].Execute(null);
            return;
        }
        _audioClips[index].PlaySound();
    }
    public void StopAudio(int index)
    {
        if (!index.InRange(range))
            throw new System.Exception($"{gameObject.name} does not have {index} in soundCommands");
        if (_useDialogueStepCommand)
        {
            _soundCommands[index].Exit();
            return;
        }
        _audioClips[index].StopSound();
    }
    public void Stop()
    {
        if (_useDialogueStepCommand)
        {
            foreach (DialogueStepCommand soundCommand in _soundCommands)
                soundCommand.Exit();
            return;
        }
        foreach (Sound sound in _audioClips)
            sound.StopSound();
    }

    [Serializable]
    private class Sound
    {
        [SerializeField] private AudioClip _audioClip;
        [SerializeField] private AudioSource _audioSource;

        public void PlaySound()
        {
            if (_audioSource == null)
            {
                Debug.LogError($"There is not audioSource Attached");
                return;
            }
            if (_audioClip == null)
            {
                Debug.LogError($"There is not audioClip Attached");
                return;
            }
            _audioSource.clip = _audioClip;
            _audioSource.Play();
        }
        public void StopSound() => _audioSource.Stop();
    }
}
