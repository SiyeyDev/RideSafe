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

        [Tooltip("Botón de continuar. Se apaga hasta que el video termina.")]
        [SerializeField] private UnityEngine.UI.Button _continueButton;

        [Header("Item card")]
        [SerializeField] private Transform _itemRow;
        [SerializeField] private TMP_Text _itemCounter;
        [SerializeField] private ProgressView _itemProgress;
        [SerializeField] private TMP_Text _explanation;

        public UnityEngine.UI.RawImage VideoSurface => _videoSurface;

        /// <summary>
        /// Puts the player's texture on the screen in its own colours. The builder paints the
        /// RawImage navy so an empty screen is not a white hole, but a RawImage multiplies its
        /// texture by that colour: left as it was, every frame came out dark blue (PO, 2026-10-28).
        /// </summary>
        public void ShowVideo(Texture texture)
        {
            if (_videoSurface == null)
                return;
            _videoSurface.texture = texture;
            _videoSurface.color = Color.white;
        }

        /// <summary>
        /// Si el prefab es anterior a que existiera <see cref="_continueButton"/>, el botón se
        /// busca por nombre.
        /// <para>
        /// Es el mismo patrón que el resto del proyecto —enlazar por nombre en runtime en vez
        /// de por referencia guardada— y existe por la misma razón: regenerar el prefab para
        /// añadir un campo borra el cableado de todo lo demás. Así el bloqueo del video
        /// funciona en el prefab que ya está en la escena, sin tocarlo.
        /// </para>
        /// </summary>
        private void Awake()
        {
            if (_continueButton != null)
                return;
            foreach (UnityEngine.UI.Button b in GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                if (b.name != "ContinueButton")
                    continue;
                _continueButton = b;
                return;
            }
        }

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

        /// <summary>
        /// Permite o impide continuar. La reunión pidió que no se pueda saltar el video: no
        /// basta con ignorar el clic, porque un botón que responde y no hace nada se lee como
        /// una interfaz rota. Se apaga, que es una respuesta.
        /// </summary>
        public void SetContinueEnabled(bool value)
        {
            if (_continueButton != null)
                _continueButton.interactable = value;
        }

        /// <summary>¿Se puede continuar? Para que un test lo pueda afirmar.</summary>
        public bool CanContinue => _continueButton == null || _continueButton.interactable;
    }
}
