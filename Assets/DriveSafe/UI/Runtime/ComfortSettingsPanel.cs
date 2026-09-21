using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace RideSafe.UI
{
    public enum RidingPosture
    {
        Seated = 0,
        Standing = 1
    }

    public enum DominantHand
    {
        Left = 0,
        Right = 1
    }

    /// <summary>Values chosen on the comfort screen. None of them affect results.</summary>
    [Serializable]
    public struct ComfortSettings
    {
        public RidingPosture Posture;
        public bool Subtitles;
        public float TextScalePercent;
        public DominantHand Hand;
    }

    /// <summary>Comfort & accessibility (05): posture, subtitles, text size, dominant hand.</summary>
    [DisallowMultipleComponent]
    public class ComfortSettingsPanel : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Toggle _standing;
        [SerializeField] private UnityEngine.UI.Toggle _subtitles;
        [SerializeField] private UnityEngine.UI.Slider _textSize;
        [SerializeField] private TMP_Text _textSizeValue;
        [SerializeField] private UnityEngine.UI.Toggle _leftHand;
        [SerializeField] private UnityEngine.UI.Button _startButton;

        /// <summary>Raised with the full set on "Start Module 1".</summary>
        public UnityEvent<ComfortSettings> onStart = new UnityEvent<ComfortSettings>();

        public ComfortSettings Current => new ComfortSettings
        {
            Posture = _standing != null && _standing.isOn ? RidingPosture.Standing : RidingPosture.Seated,
            Subtitles = _subtitles == null || _subtitles.isOn,
            TextScalePercent = _textSize != null ? _textSize.value : 100f,
            Hand = _leftHand != null && _leftHand.isOn ? DominantHand.Left : DominantHand.Right
        };

        private void Awake()
        {
            if (_textSize != null)
                _textSize.onValueChanged.AddListener(ShowTextSize);
            if (_startButton != null)
                _startButton.onClick.AddListener(HandleStart);
        }

        private void OnEnable()
        {
            if (_textSize != null)
                ShowTextSize(_textSize.value);
        }

        private void HandleStart() => onStart.Invoke(Current);

        private void ShowTextSize(float value)
        {
            if (_textSizeValue != null)
                _textSizeValue.text = Mathf.RoundToInt(value) + "%";
        }
    }
}
