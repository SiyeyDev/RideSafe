using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace RideSafe.Help
{
    /// <summary>
    /// A video on or a voice speaking: the learner is listening, not stuck. Narration is
    /// any playing, non-looping sound longer than <see cref="NarrationMinSeconds"/>; ambient
    /// loops and short clicks do not count. Sources are listed every few seconds, inactive
    /// ones included, so a player switched on between two listings is not missed.
    /// </summary>
    public sealed class QuietDetector
    {
        public const float NarrationMinSeconds = 2f;
        private const float RefreshSeconds = 5f; // narration and video players exist from the start; a scene search is not free on Quest

        private readonly List<VideoPlayer> _videos = new List<VideoPlayer>();
        private readonly List<AudioSource> _sounds = new List<AudioSource>();
        private float _sinceRefresh = float.MaxValue;

        public bool IsQuiet(float deltaTime)
        {
            _sinceRefresh += deltaTime;
            if (_sinceRefresh >= RefreshSeconds)
            {
                _sinceRefresh = 0f;
                _videos.Clear();
                _videos.AddRange(Object.FindObjectsByType<VideoPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None));
                _sounds.Clear();
                _sounds.AddRange(Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            }
            foreach (VideoPlayer video in _videos)
            {
                if (video != null && video.isActiveAndEnabled &&
                    IsVideoOn(video.isPlaying, video.isPaused, video.frame, video.frameCount))
                    return true;
            }
            foreach (AudioSource sound in _sounds)
            {
                if (sound != null && sound.isActiveAndEnabled &&
                    IsNarration(sound.isPlaying, sound.loop, sound.clip != null ? sound.clip.length : 0f))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// A video is on while it plays and while it is paused mid-clip: the learner's pause
        /// button, or Unity pausing it when the app loses focus (Quest menu, another window).
        /// Paused on its last frame it has ended, and the learner may be stuck again.
        /// </summary>
        public static bool IsVideoOn(bool playing, bool paused, long frame, ulong frameCount) =>
            playing || (paused && (frameCount == 0 || frame < (long)frameCount - 1));

        public static bool IsNarration(bool playing, bool loop, float clipSeconds) =>
            playing && !loop && clipSeconds > NarrationMinSeconds;
    }
}
