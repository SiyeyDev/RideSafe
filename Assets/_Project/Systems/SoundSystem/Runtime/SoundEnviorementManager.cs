using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEnviorementManager : MonoBehaviour
{
    [Header("AudiosSources")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioSource _audioSourceAmbiental;

    private static SoundEnviorementManager _instance;
    private static Dictionary<DefaultSounds, ScriptableSoundData> soundMap;

    private void Awake()
    {
        // Singleton pattern
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject); // No destroy
            LoadSounds(); // Load Sound Defaults
        }
        else Destroy(gameObject);
    }



    public static void PlayAudioQuestions(bool isCorrect)
    {
        if (isCorrect)
        {
            PlayAudio(DefaultSounds.Congratulations);
        }
        else
        {
            PlayAudio(DefaultSounds.Wrong);
        }
    }

    public static void PlayAudio(DefaultSounds _sound)
    {
        var soundData = GetSound(_sound);
        if (soundData == null)
        {
            Debug.LogWarning($"No se encontró el sonido {_sound}. Verifica los archivos en Resources.");
            return;
        }
        PlayAudio(soundData);
    }

    public static void PlayAudio(ScriptableSoundData scriptableSoundData)
    {
        if (_instance != null)
            scriptableSoundData.PlayAudio(_instance);
    }

    public static void StopAudioTaskInExecution()
    {
        _instance._audioSourceAmbiental.Stop();
    }

    public static IEnumerator StopAudioCoroutine(float _time)
    {
        yield return new WaitForSeconds(_time);
        _instance._audioSource.Stop();
    }

    public void StopAudioSource(bool isAmbiental)
    {
        if (isAmbiental)
        {
            _audioSourceAmbiental.Stop();
        }
        else
        {
            _audioSource.Stop();
        }
    }
    public static ScriptableSoundData GetSound(DefaultSounds sound)
    {
        if (soundMap == null)
        {
            Debug.LogWarning("Los sonidos no han sido cargados. Llamando LoadSounds()...");
            LoadSounds();
        }

        return soundMap.TryGetValue(sound, out var soundData) ? soundData : null;
    }
    public static void LoadSounds()
    {
        soundMap = new Dictionary<DefaultSounds, ScriptableSoundData>
        {
            { DefaultSounds.Congratulations, Resources.Load<ScriptableSoundData>("SO_DataSound_Congratulations") },
            { DefaultSounds.Wrong, Resources.Load<ScriptableSoundData>("SO_DataSound_Wrong") },
            { DefaultSounds.TaskInProcess, Resources.Load<ScriptableSoundData>("SO_DataSound_TaskInExecution") }
        };
    }

    public AudioSource GetAudioSource(bool isAmbiental)
    {
        if (isAmbiental)
        {
            return _audioSourceAmbiental;
        }
        return _audioSource;
    }



    public enum DefaultSounds
    {
        None = 0,
        Congratulations = 1,
        Wrong = 2,
        TaskInProcess = 3,
    }

}