using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

namespace RideSafe.UI
{
    /// <summary>
    /// A world-space screen that exists only while a clip plays.
    /// <para>
    /// The visuals hang off a child rather than this object, so the VideoPlayer keeps running
    /// while the screen is hidden: a disabled GameObject cannot prepare a clip. The screen only
    /// appears once the first frame is ready, so nobody sees a black rectangle pop in.
    /// </para>
    /// </summary>
    public class VideoScreen : MonoBehaviour
    {
        [Tooltip("Shown only while the clip plays. Must be a child, never this object.")]
        [SerializeField] private GameObject _visuals;
        [SerializeField] private VideoPlayer _player;

        public UnityEvent onFinished = new UnityEvent();

        public bool IsPlaying => _player != null && _player.isPlaying;

        private bool _playWhenReady;

        private void Awake()
        {
            if (_player == null)
                _player = GetComponent<VideoPlayer>();
            if (_player != null)
            {
                _player.playOnAwake = false;
                _player.prepareCompleted += HandlePrepared;
                _player.loopPointReached += HandleFinished;
            }
            Show(false);
        }

        private void OnDestroy()
        {
            if (_player == null)
                return;
            _player.prepareCompleted -= HandlePrepared;
            _player.loopPointReached -= HandleFinished;
        }

        /// <summary>
        /// Decodes the first frames ahead of time. Call it while something else still holds the
        /// user's attention, so Play lands instantly instead of after a second of black.
        /// </summary>
        public void Prepare()
        {
            if (_player != null && _player.clip != null && !_player.isPrepared)
                _player.Prepare();
        }

        /// <summary>Entry point for whatever hands off to the video, e.g. the narration finishing.</summary>
        public void Play()
        {
            if (_player == null || _player.clip == null)
            {
                Debug.LogError("[RideSafe.UI] VideoScreen has no clip to play.", this);
                return;
            }

            if (_player.isPrepared)
            {
                StartPlayback();
                return;
            }
            _playWhenReady = true;
            _player.Prepare();
        }

        public void Stop()
        {
            _playWhenReady = false;
            if (_player != null)
                _player.Stop();
            Show(false);
        }

        private void HandlePrepared(VideoPlayer player)
        {
            if (_playWhenReady)
                StartPlayback();
        }

        private void StartPlayback()
        {
            _playWhenReady = false;
            Show(true);
            _player.Play();
        }

        private void HandleFinished(VideoPlayer player)
        {
            Show(false);
            onFinished.Invoke();
        }

        private void Show(bool visible)
        {
            if (_visuals != null && _visuals != gameObject && _visuals.activeSelf != visible)
                _visuals.SetActive(visible);
        }
    }
}
