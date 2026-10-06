using UnityEngine;

namespace RideSafe.UI
{
    /// <summary>
    /// Hands the comfort choices to the pieces that honour them. Today that is the subtitle
    /// panel: how big its text is, and whether it shows at all.
    /// <para>
    /// ComfortSettingsPanel reports its settings through a generic UnityEvent, which Unity does
    /// not serialise, so this cannot be wired in the Inspector and lives in code instead.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(ComfortSettingsPanel))]
    [DisallowMultipleComponent]
    public class ComfortSettingsBinder : MonoBehaviour
    {
        [Tooltip("Empty finds the subtitle view in the scene, which is where it lives: the panel is a prefab and cannot reference it.")]
        [SerializeField] private SubtitleView _subtitles;

        private ComfortSettingsPanel _panel;

        private void OnEnable()
        {
            if (_panel == null)
                _panel = GetComponent<ComfortSettingsPanel>();
            _panel.onStart.AddListener(Apply);
        }

        private void OnDisable()
        {
            if (_panel != null)
                _panel.onStart.RemoveListener(Apply);
        }

        /// <summary>Applies a set of comfort choices. Public so a sequence can replay them later.</summary>
        public void Apply(ComfortSettings settings)
        {
            SubtitleView view = ResolveSubtitles();
            if (view == null)
                return;
            view.SetTextScale(settings.TextScalePercent);
            view.SetSubtitlesEnabled(settings.Subtitles);
        }

        private SubtitleView ResolveSubtitles()
        {
            if (_subtitles == null)
                _subtitles = FindAnyObjectByType<SubtitleView>(FindObjectsInactive.Include);
            return _subtitles;
        }
    }
}
