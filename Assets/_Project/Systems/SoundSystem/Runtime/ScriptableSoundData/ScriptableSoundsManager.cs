using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ScriptableSoundsManager", menuName = "ScriptableObjects/ScriptableSoundsManager", order = 1)]
public class ScriptableSoundsManager : ScriptableObject
{
    public List<ScriptableSoundData> scriptableSoundDatas ;

    public void PlayAudio(SoundEnviorementManager soundEnviorementManager, ScriptableSoundData audioToPlay)
    {
        int audioIndex = scriptableSoundDatas.IndexOf(audioToPlay);
        scriptableSoundDatas[audioIndex].PlayAudio(soundEnviorementManager);
    }
    public void StopAudio(SoundEnviorementManager soundEnviorementManager, ScriptableSoundData audioToStop)
    {
        int audioIndex = scriptableSoundDatas.IndexOf(audioToStop);
        scriptableSoundDatas[audioIndex].StopAudio(soundEnviorementManager);
    }


}
