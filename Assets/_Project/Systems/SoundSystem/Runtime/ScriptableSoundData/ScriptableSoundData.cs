using UnityEngine;

[CreateAssetMenu(fileName = "ScriptableSoundsManager", menuName = "ScriptableObjects/ScriptableClipData", order = 1)]
public class ScriptableSoundData : ScriptableObject
{
    public bool loop;
    public AudioClip audioClip;
    [Range(0, 1)] public float volume;
    public bool isAmbiental;

    public void PlayAudio(AudioSource audioSource)
    {
        audioSource.loop = loop;
        audioSource.volume = volume;
        audioSource.clip = audioClip;
        audioSource.Play();
    }
    public void PlayAudio(SoundEnviorementManager soundEnviorementManager)
    {
        AudioSource audioSource = soundEnviorementManager.GetAudioSource(isAmbiental);
        Debug.Log($"PLAY ADIO MAMON");
        audioSource.loop = loop;
        audioSource.volume = volume;
        audioSource.clip = audioClip;
        audioSource.Play();
    }
    public void StopAudio(SoundEnviorementManager soundEnviorementManager)
    {
        soundEnviorementManager.StopAudioSource(isAmbiental);
    }
}
