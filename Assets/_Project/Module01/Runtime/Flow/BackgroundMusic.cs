using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace RideSafe.Module01
{
    /// <summary>
    /// La música de fondo (Charlie, 2026-11-19): suena siempre, baja mientras habla una voz y se
    /// pausa mientras corre un video; al acabar sigue donde iba. Voz es cualquier sonido que no
    /// es bucle y dura más que un clic, como en <c>QuietDetector</c> de la ayuda.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class BackgroundMusic : MonoBehaviour
    {
        public const float VoiceMinSeconds = .5f;
        private const float RefreshSeconds = 5f; // las voces y los videos existen desde el principio; buscar en la escena no es gratis en Quest
        private const float HoldSeconds = 1f;    // entre dos frases de la misma locución no sube

        [SerializeField, Range(0f, 1f)] private float _volume = .08f;
        [SerializeField, Range(0f, 1f)] private float _duckedVolume = .025f;
        [SerializeField] private float _fadeSeconds = .5f;

        private AudioSource _source;
        private readonly List<VideoPlayer> _videos = new List<VideoPlayer>();
        private readonly List<AudioSource> _sounds = new List<AudioSource>();
        private float _sinceRefresh = float.MaxValue;
        private float _sinceVoice = float.MaxValue;
        private bool _pausedForVideo;

        public float Volume => _volume;
        public float DuckedVolume => _duckedVolume;

        private void Awake() => _source = GetComponent<AudioSource>();

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _sinceRefresh += dt;
            if (_sinceRefresh >= RefreshSeconds)
            {
                _sinceRefresh = 0f;
                _videos.Clear();
                _videos.AddRange(FindObjectsByType<VideoPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None));
                _sounds.Clear();
                _sounds.AddRange(FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            }

            if (AnyVideo())
            {
                if (_source.isPlaying) _source.Pause();
                _pausedForVideo = true;
                _source.volume = 0f; // al volver entra suave
                return;
            }
            if (_pausedForVideo)
            {
                _pausedForVideo = false;
                _source.UnPause();
            }

            _sinceVoice = AnyVoice() ? 0f : _sinceVoice + dt;
            float target = _sinceVoice < HoldSeconds ? _duckedVolume : _volume;
            _source.volume = Mathf.MoveTowards(_source.volume, target, dt * _volume / Mathf.Max(_fadeSeconds, .01f));
        }

        private bool AnyVideo()
        {
            foreach (VideoPlayer video in _videos)
                if (video != null && video.isActiveAndEnabled && video.isPlaying) return true;
            return false;
        }

        private bool AnyVoice()
        {
            foreach (AudioSource sound in _sounds)
                if (sound != null && sound != _source && sound.isActiveAndEnabled && sound.isPlaying && !sound.loop
                    && sound.clip != null && sound.clip.length > VoiceMinSeconds)
                    return true;
            return false;
        }
    }
}
