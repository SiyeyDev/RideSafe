using TMPro;
using UnityEngine;

namespace RideSafe.UI
{
    /// <summary>
    /// Keeps a Button or Toggle's visuals in sync with its state, beyond what uGUI's
    /// sprite-swap transition covers:
    /// <list type="bullet">
    /// <item>Disabled label colour (sprite swap only changes the background).</item>
    /// <item>Toggle ON look: base sprite, hover/pressed sprites, label colour, caption.</item>
    /// <item>Switch knob position and On/Off text.</item>
    /// </list>
    /// Every ON-look field is optional; leave it empty and it is skipped.
    /// </summary>
    [DisallowMultipleComponent]
    public class SelectableStyle : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Selectable _selectable;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Color _labelColor = Color.white;
        [SerializeField] private Color _disabledLabelColor = new Color(0.43f, 0.51f, 0.59f, 1f);

        [Header("Toggle ON look (optional)")]
        [SerializeField] private UnityEngine.UI.Image _background;
        [SerializeField] private Sprite _offSprite;
        [SerializeField] private Sprite _onSprite;
        [SerializeField] private bool _swapSpriteState;
        [SerializeField] private UnityEngine.UI.SpriteState _offSpriteState;
        [SerializeField] private UnityEngine.UI.SpriteState _onSpriteState;
        [SerializeField] private Color _onLabelColor = Color.white;
        [SerializeField] private TMP_Text _caption;
        [SerializeField] private string _onCaption;

        [Header("Switch (optional)")]
        [SerializeField] private RectTransform _knob;
        [SerializeField] private Vector2 _knobOff;
        [SerializeField] private Vector2 _knobOn;
        [SerializeField] private TMP_Text _stateText;
        [SerializeField] private string _offText = "Off";
        [SerializeField] private string _onText = "On";

        private UnityEngine.UI.Toggle _toggle;
        private bool? _lastInteractable;

        private void Awake()
        {
            if (_selectable == null)
                _selectable = GetComponent<UnityEngine.UI.Selectable>();
            _toggle = _selectable as UnityEngine.UI.Toggle;
            if (_toggle != null)
                _toggle.onValueChanged.AddListener(ApplyToggle);
        }

        private void OnEnable()
        {
            _lastInteractable = null;
            if (_toggle != null)
                ApplyToggle(_toggle.isOn);
        }

        private void OnDestroy()
        {
            if (_toggle != null)
                _toggle.onValueChanged.RemoveListener(ApplyToggle);
        }

        private void LateUpdate()
        {
            // Interactable has no change event, so poll it cheaply and act only on change.
            if (_selectable == null || _label == null)
                return;
            bool interactable = _selectable.IsInteractable();
            if (_lastInteractable == interactable)
                return;
            _lastInteractable = interactable;
            _label.color = interactable ? CurrentLabelColor() : _disabledLabelColor;
        }

        private Color CurrentLabelColor() => _toggle != null && _toggle.isOn ? _onLabelColor : _labelColor;

        private void ApplyToggle(bool isOn)
        {
            if (_background != null && (isOn ? _onSprite : _offSprite) != null)
                _background.sprite = isOn ? _onSprite : _offSprite;
            if (_swapSpriteState)
                _toggle.spriteState = isOn ? _onSpriteState : _offSpriteState;
            if (_label != null && _selectable.IsInteractable())
                _label.color = CurrentLabelColor();

            if (_caption != null)
            {
                _caption.text = isOn ? _onCaption : string.Empty;
                _caption.gameObject.SetActive(isOn && !string.IsNullOrEmpty(_onCaption));
            }
            if (_knob != null)
                _knob.anchoredPosition = isOn ? _knobOn : _knobOff;
            if (_stateText != null)
                _stateText.text = isOn ? _onText : _offText;
        }
    }
}
