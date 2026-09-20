using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundOnlyPlay : MonoBehaviour
{
    public bool activeEnable;
    public ScriptableSoundData dataToPlay;

    private void OnEnable()
    {
        if (activeEnable)
        {
            SoundEnviorementManager.PlayAudio(dataToPlay);

        }
    }

    [Button("PlayAudio")]
    public void PlayAudio()
    {
       SoundEnviorementManager.PlayAudio(dataToPlay);
    }

    public void PlayAudioAnimation(ScriptableSoundData _setClip)
    {
        SoundEnviorementManager.PlayAudio(_setClip);
    }
}
