using System;
using System.Collections.Generic;
using Cachacos;
using RideSafe.UI;
using UnityEngine;

namespace RideSafe.Help
{
    /// <summary>
    /// The scene's tutorial. Observes without touching any module: scans what can be selected,
    /// hears clicks on those targets, pauses for video and narration, follows the dominant hand,
    /// and lets <see cref="HelpDirector"/> decide what <see cref="GestureHintView"/> and
    /// <see cref="TargetHighlighter"/> show. Registers itself for <see cref="TutorialHelp"/>.
    /// Runs early so a module's OnEnable can already call it.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    [AddComponentMenu("RideSafe/Tutorial Help/Tutorial Help Manager")]
    public sealed class TutorialHelpManager : MonoBehaviour, ITutorialHelpService
    {
        [SerializeField] private TutorialGestureSO _selectGesture;
        [SerializeField] private GestureHintView _hintView;
        [SerializeField] private TargetHighlighter _highlighter;
        [SerializeField, Min(0.05f)] private float _scanSeconds = 0.2f;

        private readonly TargetScanner _scanner = new TargetScanner();
        private readonly QuietDetector _quiet = new QuietDetector();
        private readonly HandTracker _hand = new HandTracker();
        private readonly List<HelpTarget> _visible = new List<HelpTarget>();
        private readonly List<ClickWitness> _witnesses = new List<ClickWitness>();
        private HelpDirector _director;
        private ILocalizationProvider _localization;
        private float _sinceScan = float.MaxValue;
        private float _sinceHands = float.MaxValue;

        public HelpDirector Director => _director;
        public IReadOnlyList<HelpTarget> Visible => _visible;
        public GestureHintView HintView => _hintView;
        public TargetHighlighter Highlighter => _highlighter;
        public bool Observing => _director != null && _director.Observing;

        private void Awake()
        {
            _director = new HelpDirector(_selectGesture, _hintView, _highlighter, Translate);
            if (_hintView != null)
                _hintView.Hide();
            TutorialHelp.Register(this);
        }

        private void OnDestroy()
        {
            TutorialHelp.Unregister(this);
            _hand.Dispose();
            foreach (ClickWitness witness in _witnesses)
            {
                if (witness == null)
                    continue;
                witness.Clicked -= OnClicked;
                Destroy(witness);
            }
            _witnesses.Clear();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            _sinceHands += deltaTime;
            if (_sinceHands >= TargetScanner.ObjectRefreshSeconds)
            {
                _sinceHands = 0f;
                _hand.Listen(FindObjectsByType<ComfortSettingsPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            }
            _director.Hand = _hand.Hand;

            if (_director.Observing)
            {
                _sinceScan += deltaTime;
                Camera camera = Camera.main;
                if (_sinceScan >= _scanSeconds && camera != null)
                {
                    _scanner.Scan(new Pose(camera.transform.position, camera.transform.rotation), _sinceScan, transform, _visible);
                    _sinceScan = 0f;
                    Witness(_visible);
                }
            }
            else
            {
                _visible.Clear();
                _sinceScan = float.MaxValue;
            }

            _director.Update(deltaTime, _visible, _director.Observing && _quiet.IsQuiet(deltaTime));
        }

        public IDisposable Show(TutorialGestureSO gesture, IReadOnlyList<Transform> targets) => _director.Show(gesture, targets);
        public void Stop() => _director.Stop();
        public void Resume() => _director.Resume();

        private void Witness(List<HelpTarget> targets)
        {
            foreach (HelpTarget target in targets)
            {
                if (target.Transform == null)
                    continue;
                ClickWitness witness = ClickWitness.Attach(target.Transform.gameObject, target.Kind);
                if (_witnesses.Contains(witness))
                    continue;
                witness.Clicked += OnClicked;
                _witnesses.Add(witness);
            }
        }

        private void OnClicked(ClickWitness witness) => _director.Clicked(witness.Kind);

        private string Translate(string key)
        {
            if (_localization == null)
                _localization = ServiceLocator.Instance.RequestService<ILocalizationProvider>();
            return _localization?.GetTranslation(key);
        }
    }
}
