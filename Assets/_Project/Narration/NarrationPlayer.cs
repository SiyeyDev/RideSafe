using System;
using System.Collections;
using System.Collections.Generic;
using I2.Loc;
using RideSafe.UI;
using UnityEngine;
using UnityEngine.Events;

namespace RideSafe.Narration
{
    /// <summary>
    /// Plays the instructor's lines in order and drives the subtitles from the clip's own clock,
    /// so each word appears exactly when it is spoken.
    /// <para>
    /// The audio tool writes every line as a pair under
    /// Resources/&lt;folder&gt;/&lt;language&gt;/&lt;key&gt;: an .mp3 and a .json holding the word
    /// timings. Only the key is authored here; the language folder is resolved from I2 when the
    /// line plays, so switching language in the menu also switches the voice.
    /// </para>
    /// </summary>
    public class NarrationPlayer : MonoBehaviour
    {
        /// <summary>Shape written by the audio tool. Field names must match the JSON exactly.</summary>
        [Serializable]
        private class TimedTranscript
        {
            public string[] characters;
            public float[] character_start_times_seconds;
            public float[] character_end_times_seconds;
        }

        [Tooltip("Resources folder holding one subfolder per language, e.g. Sounds/Module1.")]
        [SerializeField] private string _resourceFolder = "Sounds/Module1";
        [Tooltip("File names without extension, played in this order.")]
        [SerializeField] private List<string> _lines = new List<string>();
        [Tooltip("Language folder used when I2 reports no language.")]
        [SerializeField] private string _fallbackLanguage = "Español";
        [Tooltip("Silence between one line and the next.")]
        [SerializeField] private float _gapSeconds = 0.4f;
        [SerializeField] private AudioSource _audio;
        [SerializeField] private SubtitleView _subtitles;
        [Tooltip("Objects switched on while the narration runs, e.g. the subtitle panel.")]
        [SerializeField] private List<GameObject> _showWhilePlaying = new List<GameObject>();

        public UnityEvent onStarted = new UnityEvent();
        public UnityEvent onFinished = new UnityEvent();

        public bool IsPlaying { get; private set; }

        private Coroutine _routine;

        /// <summary>Plays every line in order. Safe to call again; it restarts from the top.</summary>
        public void Play()
        {
            Stop();
            _routine = StartCoroutine(PlayLines());
        }

        public void Stop()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
            if (_audio != null)
                _audio.Stop();
            if (_subtitles != null)
                _subtitles.Clear();
            SetVisuals(false);
            IsPlaying = false;
        }

        /// <summary>Comfort setting: hides the text, the voice keeps playing.</summary>
        public void SetSubtitlesEnabled(bool enabled)
        {
            if (_subtitles != null)
                _subtitles.SetSubtitlesEnabled(enabled);
        }

        private IEnumerator PlayLines()
        {
            if (_audio == null)
            {
                Debug.LogError("[RideSafe.Narration] No AudioSource assigned.", this);
                yield break;
            }

            IsPlaying = true;
            SetVisuals(true);
            onStarted.Invoke();

            string language = LocalizationManager.CurrentLanguage;
            if (string.IsNullOrEmpty(language))
                language = _fallbackLanguage;

            for (int i = 0; i < _lines.Count; i++)
            {
                string key = _lines[i];
                string path = _resourceFolder + "/" + language + "/" + key;

                AudioClip clip = Resources.Load<AudioClip>(path);
                if (clip == null && language != _fallbackLanguage)
                {
                    // A language can be selectable in the menu before its voice over is recorded.
                    Debug.LogWarning("[RideSafe.Narration] No clip at Resources/" + path
                                     + ", falling back to " + _fallbackLanguage, this);
                    path = _resourceFolder + "/" + _fallbackLanguage + "/" + key;
                    clip = Resources.Load<AudioClip>(path);
                }
                if (clip == null)
                {
                    Debug.LogError("[RideSafe.Narration] No clip at Resources/" + path, this);
                    continue;
                }

                if (_subtitles != null)
                    _subtitles.SetTrack(ReadWords(path));

                _audio.clip = clip;
                _audio.Play();
                while (_audio.isPlaying)
                {
                    if (_subtitles != null)
                        _subtitles.SetTime(_audio.time);
                    yield return null;
                }

                // Lets the closing cue finish its hold before the next line wipes it.
                float held = 0f;
                while (held < _gapSeconds)
                {
                    held += Time.deltaTime;
                    if (_subtitles != null)
                        _subtitles.SetTime(clip.length + held);
                    yield return null;
                }
            }

            if (_subtitles != null)
                _subtitles.Clear();
            SetVisuals(false);
            IsPlaying = false;
            _routine = null;
            onFinished.Invoke();
        }

        /// <summary>Reads the word timings that sit next to the clip. Missing ones just mean no subtitles.</summary>
        private List<SubtitleWord> ReadWords(string path)
        {
            TextAsset json = Resources.Load<TextAsset>(path);
            if (json == null)
            {
                Debug.LogWarning("[RideSafe.Narration] No timings at Resources/" + path + ".json", this);
                return null;
            }

            TimedTranscript transcript = JsonUtility.FromJson<TimedTranscript>(json.text);
            if (transcript == null || transcript.characters == null)
            {
                Debug.LogWarning("[RideSafe.Narration] Unreadable timings at Resources/" + path + ".json", this);
                return null;
            }

            var words = new List<SubtitleWord>(transcript.characters.Length);
            for (int i = 0; i < transcript.characters.Length; i++)
            {
                float start = i < transcript.character_start_times_seconds.Length
                    ? transcript.character_start_times_seconds[i] : 0f;
                float end = i < transcript.character_end_times_seconds.Length
                    ? transcript.character_end_times_seconds[i] : start;
                words.Add(new SubtitleWord(transcript.characters[i], start, end));
            }
            return words;
        }

        private void SetVisuals(bool visible)
        {
            for (int i = 0; i < _showWhilePlaying.Count; i++)
            {
                if (_showWhilePlaying[i] != null)
                    _showWhilePlaying[i].SetActive(visible);
            }
        }
    }
}
