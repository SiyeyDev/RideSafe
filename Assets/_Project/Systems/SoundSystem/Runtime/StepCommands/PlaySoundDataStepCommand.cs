using Sirenix.OdinInspector;
using StepCommand;
using System;
using UnityEngine;

[Serializable]
public class PlaySoundDataStepCommand : StepCommandClass
{
    [Header("PlaySoundDataStepCommand Settings")]
    [SerializeField] private bool _useDefault;
    [ShowIf("_useDefault")][SerializeField] private SoundEnviorementManager.DefaultSounds _defaultSound;
    [InlineEditor][HideIf("_useDefault")][SerializeField] private ScriptableSoundData _soundData;

    #region ICommand Methods
    public override void Execute(Action<bool, IStepCommand> onComplete)
    {
        base.Execute(onComplete);
        if (_useDefault)
            SoundEnviorementManager.PlayAudio(_defaultSound);
        else
            SoundEnviorementManager.PlayAudio(_soundData);
        CoroutineCaller.Instance.WaitForNextFrame( () => Complete(true, this));
    }
    #endregion
}
