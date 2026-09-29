using System;
using System.Collections.Generic;
using Autohand.Demo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.Tutorial.AutoHand
{
    /// <summary>
    /// Flat 2D Meta Quest Touch Plus drawn with uGUI, used to show which physical button to
    /// press. Knows buttons, not ActionIds: <see cref="TutorialPresenter"/> resolves the
    /// ActionId to a hand and button through <see cref="AutoHandTutorialInputService"/>.
    /// <para>
    /// The required button is marked by several cues at once, never colour alone: an
    /// outline ring, a scale pulse and a small "press" movement. <see cref="Release"/> plays
    /// a short pressed pose and hides the view (~0.5 s).
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/Tutorial/Controller Hint View")]
    public class ControllerHintView : MonoBehaviour
    {
        [Serializable]
        public struct Part
        {
            public CommonButton Button;
            public RectTransform Rect;
            public Graphic Graphic;
        }

        [Tooltip("Drawing of a RIGHT controller. Mirrored on X for the left hand.")]
        [SerializeField] private RectTransform _controller;
        [SerializeField] private List<Part> _parts = new List<Part>();

        [Tooltip("Ring placed behind the required button.")]
        [SerializeField] private Image _outline;

        [Tooltip("Face-button letters: A/B on the right controller, X/Y on the left.")]
        [SerializeField] private TMP_Text _primaryLetter;
        [SerializeField] private TMP_Text _secondaryLetter;

        [SerializeField] private Color _activeColor = new Color(0.45f, 0.82f, 1f);
        [SerializeField] private Vector2 _pressOffset = new Vector2(0f, -5f);
        [SerializeField, Min(0.1f)] private float _cycleSeconds = 1.2f;
        [SerializeField, Min(0.1f)] private float _releaseSeconds = 0.5f;

        private int _active = -1;
        private Vector2 _restPosition;
        private Vector3 _restScale;
        private Color _restColor;
        private float _time;
        private float _releaseTime = -1f;

        public bool IsVisible => gameObject.activeSelf && _active >= 0;
        public bool IsReleasing => IsVisible && _releaseTime >= 0f;

        /// <summary>Shows the controller with <paramref name="button"/> marked. False when the drawing has no such part.</summary>
        public bool Show(CommonButton button, bool leftHand)
        {
            int index = _parts.FindIndex(part => part.Button == button && part.Rect != null);
            if (index < 0)
                return false;

            RestoreActivePart();
            _active = index;
            Part part = _parts[index];
            _restPosition = part.Rect.anchoredPosition;
            _restScale = part.Rect.localScale;
            _restColor = part.Graphic != null ? part.Graphic.color : Color.white;
            if (part.Graphic != null)
                part.Graphic.color = _activeColor;

            Mirror(leftHand);
            PlaceOutline(part.Rect);

            _time = 0f;
            _releaseTime = -1f;
            gameObject.SetActive(true);
            return true;
        }

        /// <summary>The learner pressed it: show the button pressed, then hide.</summary>
        public void Release()
        {
            if (IsVisible && _releaseTime < 0f)
                _releaseTime = 0f;
        }

        public void Hide()
        {
            RestoreActivePart();
            _releaseTime = -1f;
            gameObject.SetActive(false);
        }

        protected virtual void Update()
        {
            if (_active < 0)
                return;

            RectTransform rect = _parts[_active].Rect;
            float dt = Time.deltaTime;

            if (_releaseTime >= 0f)
            {
                _releaseTime += dt;
                // Pressed pose, outline swells and fades, then gone.
                rect.anchoredPosition = _restPosition + _pressOffset;
                rect.localScale = _restScale * 0.92f;
                float fade = Mathf.Clamp01(_releaseTime / _releaseSeconds);
                SetOutline(1f + 0.6f * fade, 1f - fade);
                if (_releaseTime >= _releaseSeconds)
                    Hide();
                return;
            }

            _time += dt;
            float phase = (_time % _cycleSeconds) / _cycleSeconds;
            float pulse = 0.5f + 0.5f * Mathf.Sin(phase * Mathf.PI * 2f);
            // A short "press" in the last fifth of each cycle shows HOW to use the button.
            float press = phase > 0.8f ? Mathf.Sin((phase - 0.8f) / 0.2f * Mathf.PI) : 0f;

            rect.localScale = _restScale * (1f + 0.15f * pulse);
            rect.anchoredPosition = _restPosition + _pressOffset * press;
            SetOutline(1f + 0.25f * pulse, 0.55f + 0.45f * pulse);
        }

        private void RestoreActivePart()
        {
            if (_active < 0)
                return;
            Part part = _parts[_active];
            if (part.Rect != null)
            {
                part.Rect.anchoredPosition = _restPosition;
                part.Rect.localScale = _restScale;
            }
            if (part.Graphic != null)
                part.Graphic.color = _restColor;
            _active = -1;
        }

        private void Mirror(bool leftHand)
        {
            if (_controller != null)
                _controller.localScale = new Vector3(leftHand ? -1f : 1f, 1f, 1f);

            // Letters stay readable: counter-mirror them and use the left controller's names.
            SetLetter(_primaryLetter, leftHand ? "X" : "A", leftHand);
            SetLetter(_secondaryLetter, leftHand ? "Y" : "B", leftHand);
        }

        private static void SetLetter(TMP_Text letter, string text, bool mirrored)
        {
            if (letter == null)
                return;
            letter.text = text;
            letter.rectTransform.localScale = new Vector3(mirrored ? -1f : 1f, 1f, 1f);
        }

        private void PlaceOutline(RectTransform target)
        {
            if (_outline == null)
                return;
            RectTransform ring = _outline.rectTransform;
            ring.SetParent(target.parent, false);
            ring.SetSiblingIndex(target.GetSiblingIndex());
            ring.anchoredPosition = target.anchoredPosition;
            ring.sizeDelta = target.sizeDelta + new Vector2(14f, 14f);
            ring.gameObject.SetActive(true);
        }

        private void SetOutline(float scale, float alpha)
        {
            if (_outline == null)
                return;
            _outline.rectTransform.localScale = Vector3.one * scale;
            Color color = _activeColor;
            color.a = alpha;
            _outline.color = color;
        }
    }
}
