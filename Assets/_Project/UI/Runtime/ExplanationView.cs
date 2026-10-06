using TMPro;
using UnityEngine;

namespace RideSafe.UI
{
    /// <summary>
    /// Review state B (10B / 13B): one item explained at a time beside the video. The video
    /// surface is a RawImage for a VideoPlayer's RenderTexture; playback itself is owned by
    /// whoever drives the Pause / Replay / Continue buttons.
    /// </summary>
    [DisallowMultipleComponent]
    public class ExplanationView : MonoBehaviour
    {
        [SerializeField] private StatusGlyphs _glyphs = new StatusGlyphs();

        [SerializeField] private TMP_Text _badge;
        [SerializeField] private string _badgeFormat = "Explaining · item {0} of {1}";
        [SerializeField] private TMP_Text _heading;
        [SerializeField] private TMP_Text _caption;
        [SerializeField] private GameObject _playingTag;
        [SerializeField] private ProgressView _videoProgress;
        [SerializeField] private UnityEngine.UI.RawImage _videoSurface;

        [Header("Item card")]
        [SerializeField] private Transform _itemRow;
        [SerializeField] private TMP_Text _itemCounter;
        [SerializeField] private ProgressView _itemProgress;
        [SerializeField] private TMP_Text _explanation;

        public UnityEngine.UI.RawImage VideoSurface => _videoSurface;

        /// <summary>
        /// Sets the header wording, e.g. ("Explaining · control {0} of {1}", "The control
        /// being explained") for the diagnostic review.
        /// </summary>
        public void SetMode(string badgeFormat, string heading)
        {
            _badgeFormat = badgeFormat;
            if (_heading != null)
                _heading.text = heading;
        }

        /// <param name="index">1-based position of the item being explained.</param>
        public void SetItem(int index, int total, ComparisonEntry item, string explanation, string caption)
        {
            if (_badge != null)
                _badge.text = string.Format(_badgeFormat, index, total);
            if (_itemRow != null)
                _glyphs.Bind(_itemRow, item);
            if (_itemCounter != null)
                _itemCounter.text = "Item " + index + " of " + total;
            if (_itemProgress != null)
                _itemProgress.SetValue(total > 0 ? (float)index / total : 0f);
            if (_explanation != null)
                _explanation.text = explanation;
            if (_caption != null)
                _caption.text = caption;
        }

        public void SetPlaying(bool playing)
        {
            if (_playingTag != null)
                _playingTag.SetActive(playing);
        }

        public void SetVideoProgress(float normalized)
        {
            if (_videoProgress != null)
                _videoProgress.SetValue(normalized);
        }
    }
}
