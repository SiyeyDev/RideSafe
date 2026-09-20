using Sirenix.OdinInspector;
using System;
using UnityEngine;

/// <note>
/// The <see cref="SoundSequencePropertyDrawer"/> class is used to draw the `SoundSequence` fields in the Inspector using a custom property drawer.
/// Any changes to the serialized fields will not be automatically reflected in the Inspector unless the 
/// <see cref="SoundSequencePropertyDrawer"/> is updated accordingly. If you add or modify any variables in `SoundSequence`,
/// ensure the <see cref="SoundSequencePropertyDrawer"/> logic is also updated to properly display and handle them in the Inspector.
/// </note>
[Serializable]
public class SoundSequence
{
    [SerializeField] private bool _playSequence;
    [SerializeField] private SoundCommand[] _sounds;
    private float _duration = 0;
    public void PlaySequence(MonoBehaviour caller, Action onComplete = null)
    {
        if (!_playSequence)
        {
            onComplete?.Invoke();
            return;
        }
        CommandSequence commandSequence = new CommandSequence();
        foreach (SoundCommand soundCommand in _sounds)
        {
            soundCommand.Initialize(caller);
            commandSequence.AddCommand(soundCommand);
        }
        commandSequence.StartSequence(onComplete);
    }
    public float GetDuration()
    {
        if (_duration != 0)
            return _duration;
        foreach (SoundCommand soundCommand in _sounds)
            _duration += soundCommand.GetDuration();
        return _duration;
    }

}
