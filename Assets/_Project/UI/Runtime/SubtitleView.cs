using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace RideSafe.UI
{
    /// <summary>One spoken word and when it is said, in seconds from the start of its clip.</summary>
    public struct SubtitleWord
    {
        public readonly string Text;
        public readonly float Start;
        public readonly float End;

        public SubtitleWord(string text, float start, float end)
        {
            Text = text;
            Start = start;
            End = end;
        }
    }

    /// <summary>
    /// Shows a spoken line as it is being said. Words are grouped into cues no longer than a
    /// couple of lines, and each cue types itself out following the clip's own word timings, so
    /// the text lands on the voice instead of running at a guessed speed.
    /// <para>
    /// A cue's full text is laid out up front and revealed with maxVisibleCharacters: letters
    /// appear in place instead of reflowing the paragraph every frame, which would be unreadable
    /// in VR.
    /// </para>
    /// </summary>
    public class SubtitleView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [Tooltip("Switched off whenever nothing is being said, so nothing floats in view between lines. Empty uses this object.")]
        [SerializeField] private GameObject _visuals;

        [Header("Cue splitting")]
        [Tooltip("A cue breaks once it passes this many characters, or right after . ! ? - whichever comes first.")]
        [SerializeField] private int _maxCharactersPerCue = 90;
        [Tooltip("A silence longer than this also breaks the cue.")]
        [SerializeField] private float _maxSilenceSeconds = 0.7f;
        [Tooltip("Seconds the last cue stays up after the voice stops.")]
        [SerializeField] private float _holdSeconds = 1.2f;

        private struct CueWord
        {
            public int CharStart;
            public int Length;
            public float Start;
            public float End;
        }

        private class Cue
        {
            public string Text;
            public float Start;
            public float End;
            public CueWord[] Words;
        }

        private readonly List<Cue> _cues = new List<Cue>();
        private int _current = -1;
        private float _baseFontSize;
        private float _baseFontSizeMin;
        private float _baseFontSizeMax;
        private bool _enabledBySettings = true;

        private void Awake()
        {
            if (_visuals == null)
                _visuals = gameObject;
            // Read before hiding: turning the visuals off can disable this component too.
            if (_label != null)
            {
                _baseFontSize = _label.fontSize;
                _baseFontSizeMin = _label.fontSizeMin;
                _baseFontSizeMax = _label.fontSizeMax;
            }
            ShowPanel(false);
        }

        /// <summary>Feeds a clip's words and rewinds to its start. Call before SetTime.</summary>
        public void SetTrack(IList<SubtitleWord> words)
        {
            _cues.Clear();
            _current = -1;
            ShowPanel(false);
            if (words == null || words.Count == 0)
                return;

            StringBuilder text = new StringBuilder();
            List<CueWord> cueWords = new List<CueWord>();
            float cueStart = words[0].Start;
            float previousEnd = words[0].Start;

            for (int i = 0; i < words.Count; i++)
            {
                SubtitleWord word = words[i];
                if (string.IsNullOrEmpty(word.Text))
                    continue;

                bool silenceBreak = cueWords.Count > 0 && word.Start - previousEnd > _maxSilenceSeconds;
                bool lengthBreak = cueWords.Count > 0 && text.Length + 1 + word.Text.Length > _maxCharactersPerCue;
                if (silenceBreak || lengthBreak)
                {
                    Flush(text, cueWords, cueStart, previousEnd);
                    cueStart = word.Start;
                }

                if (text.Length > 0)
                    text.Append(' ');
                cueWords.Add(new CueWord
                {
                    CharStart = text.Length,
                    Length = word.Text.Length,
                    Start = word.Start,
                    End = Mathf.Max(word.End, word.Start + 0.05f),
                });
                text.Append(word.Text);
                previousEnd = word.End;

                if (EndsSentence(word.Text))
                {
                    Flush(text, cueWords, cueStart, previousEnd);
                    cueStart = i + 1 < words.Count ? words[i + 1].Start : previousEnd;
                }
            }
            Flush(text, cueWords, cueStart, previousEnd);
        }

        /// <summary>Drives the reveal from the clip's playback position.</summary>
        public void SetTime(float seconds)
        {
            if (_cues.Count == 0 || _label == null || !_enabledBySettings)
                return;

            int index = IndexAt(seconds);
            if (index < 0)
            {
                ShowPanel(false);
                _current = -1;
                return;
            }

            if (index != _current)
            {
                _current = index;
                _label.text = _cues[index].Text;
                _label.maxVisibleCharacters = 0;
                ShowPanel(true);
            }
            _label.maxVisibleCharacters = VisibleCharacters(_cues[index], seconds);
        }

        /// <summary>Drops the current line and hides the panel.</summary>
        public void Clear()
        {
            _cues.Clear();
            _current = -1;
            if (_label != null)
                _label.text = string.Empty;
            ShowPanel(false);
        }

        /// <summary>Comfort setting: subtitles off hides the panel without stopping the audio.</summary>
        public void SetSubtitlesEnabled(bool enabled)
        {
            _enabledBySettings = enabled;
            if (!enabled)
                ShowPanel(false);
        }

        /// <summary>
        /// Comfort setting: text size as a percentage of the authored size. Auto-sizing bounds
        /// scale with it, otherwise TMP would clamp the new size straight back into the old range.
        /// </summary>
        public void SetTextScale(float percent)
        {
            if (_label == null || _baseFontSize <= 0f)
                return;
            float scale = Mathf.Max(0.1f, percent / 100f);
            _label.fontSize = _baseFontSize * scale;
            if (_label.enableAutoSizing)
            {
                _label.fontSizeMin = _baseFontSizeMin * scale;
                _label.fontSizeMax = _baseFontSizeMax * scale;
            }
        }

        private void Flush(StringBuilder text, List<CueWord> words, float start, float end)
        {
            if (words.Count == 0)
                return;
            _cues.Add(new Cue { Text = text.ToString(), Start = start, End = end, Words = words.ToArray() });
            text.Length = 0;
            words.Clear();
        }

        private static bool EndsSentence(string word)
        {
            char last = word[word.Length - 1];
            return last == '.' || last == '!' || last == '?';
        }

        /// <summary>Index of the cue owning this moment, counting the hold after the last one.</summary>
        private int IndexAt(float seconds)
        {
            for (int i = _cues.Count - 1; i >= 0; i--)
            {
                if (seconds < _cues[i].Start)
                    continue;
                bool isLast = i == _cues.Count - 1;
                if (!isLast || seconds <= _cues[i].End + _holdSeconds)
                    return i;
                return -1;
            }
            return -1;
        }

        private static int VisibleCharacters(Cue cue, float seconds)
        {
            int visible = 0;
            for (int i = 0; i < cue.Words.Length; i++)
            {
                CueWord word = cue.Words[i];
                if (seconds >= word.End)
                {
                    visible = word.CharStart + word.Length;
                    continue;
                }
                if (seconds <= word.Start)
                    break;

                float progress = (seconds - word.Start) / (word.End - word.Start);
                visible = word.CharStart + Mathf.CeilToInt(word.Length * progress);
                break;
            }
            return visible;
        }

        /// <summary>
        /// Turns the whole panel on only while a line is being spoken. GazeFollower snaps on
        /// enable, so every line also arrives already placed in front of the user.
        /// </summary>
        private void ShowPanel(bool visible)
        {
            GameObject target = _visuals != null ? _visuals : gameObject;
            if (target.activeSelf != visible)
                target.SetActive(visible);
        }
    }
}
