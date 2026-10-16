using System;
using System.Collections.Generic;
using RideSafe.Tutorial;
using RideSafe.UI;
using UnityEngine;

namespace RideSafe.Help
{
    /// <summary>
    /// Decides what the views show, without Unity's frame loop: the automatic help from
    /// <see cref="HelpRules"/> and the explicit requests from modules (the latest wins, and
    /// they work while observation is stopped).
    /// </summary>
    public sealed class HelpDirector : ITutorialHelpService
    {
        private readonly HelpRules _rules = new HelpRules();
        private readonly TutorialGestureSO _gesture;
        private readonly IGestureHintView _view;
        private readonly ITargetHighlighter _highlighter;
        private readonly Func<string, string> _translate;
        private readonly List<Request> _requests = new List<Request>();
        private readonly List<HelpTarget> _visible = new List<HelpTarget>();
        private readonly List<HelpTarget> _shown = new List<HelpTarget>();
        private readonly List<HelpTarget> _next = new List<HelpTarget>();
        private HelpDecision _shownFor = HelpDecision.None;
        private Request _presentedRequest;
        private DominantHand _presentedHand;
        private bool _autoPresented;
        private HelpDecision _textFor = HelpDecision.None;
        private string _text;

        public HelpDirector(TutorialGestureSO gesture, IGestureHintView view, ITargetHighlighter highlighter,
                            Func<string, string> translate)
        {
            _gesture = gesture;
            _view = view;
            _highlighter = highlighter;
            _translate = translate;
        }

        public DominantHand Hand { get; set; } = DominantHand.Right;
        public HelpDecision Decision { get; private set; } = HelpDecision.None;
        public bool Observing => _rules.Observing;
        public bool IsLearned(InteractionKind kind) => _rules.IsLearned(kind);

        public void Clicked(InteractionKind kind) => _rules.Clicked(kind);

        public void Update(float deltaTime, IReadOnlyList<HelpTarget> visible, bool quiet)
        {
            _visible.Clear();
            bool ui = false, obj = false;
            for (int i = 0; i < visible.Count; i++)
            {
                HelpTarget target = visible[i];
                _visible.Add(target);
                if (target.Kind == InteractionKind.Interface)
                    ui = true;
                else
                    obj = true;
            }
            Decision = _rules.Tick(deltaTime, ui, obj, quiet);
            Render();
        }

        public IDisposable Show(TutorialGestureSO gesture, IReadOnlyList<Transform> targets)
        {
            Request request = new Request(this, gesture != null ? gesture : _gesture, targets);
            _requests.Add(request);
            Render();
            return request;
        }

        public void Stop()
        {
            _rules.Stop();
            Decision = HelpDecision.None;
            Render();
        }

        public void Resume() => _rules.Resume();

        private void Render()
        {
            if (_requests.Count > 0)
            {
                Request request = _requests[_requests.Count - 1];
                if (request == _presentedRequest && Hand == _presentedHand)
                    return;
                Present(request.Gesture, request.Targets, null);
                _presentedRequest = request;
                _autoPresented = false;
                return;
            }
            _presentedRequest = null;
            if (Decision.Mode == HelpMode.None)
            {
                _shown.Clear();
                _shownFor = HelpDecision.None;
                if (_autoPresented || _view == null || _view.IsVisible)
                {
                    _view?.Hide();
                    _highlighter?.Clear();
                }
                _autoPresented = false;
                return;
            }

            _next.Clear();
            for (int i = 0; i < _visible.Count; i++)
            {
                HelpTarget target = _visible[i];
                if (Decision.Mode == HelpMode.Stuck || target.Kind == Decision.Kind)
                    _next.Add(target);
            }
            // While the same help stays up, an option it showed keeps its highlight even if the
            // learner looks away from it: an evaluated choice never ends with one option singled out.
            bool sameHelp = Decision.Equals(_shownFor);
            if (sameHelp)
            {
                for (int i = 0; i < _shown.Count; i++)
                {
                    HelpTarget kept = _shown[i];
                    if (kept.Transform != null && kept.Transform.gameObject.activeInHierarchy && !_next.Contains(kept))
                        _next.Add(kept);
                }
            }
            // Same help, same targets, same hand: what is on screen is already right. Redrawing
            // every frame boxed enumerators, asked I2 for the text and re-ran the highlighter.
            bool unchanged = _autoPresented && sameHelp && Hand == _presentedHand && SameTargets(_next, _shown);
            _shown.Clear();
            _shown.AddRange(_next);
            _shownFor = Decision;
            if (unchanged)
                return;
            if (!_textFor.Equals(Decision))
            {
                _textFor = Decision;
                _text = Decision.Mode == HelpMode.FirstTime ? Translate(_gesture.TextKeyFor(Decision.Kind)) : null;
            }
            Present(_gesture, _shown, _text);
            _autoPresented = true;
        }

        private static bool SameTargets(List<HelpTarget> a, List<HelpTarget> b)
        {
            if (a.Count != b.Count)
                return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (!a[i].Equals(b[i]))
                    return false;
            }
            return true;
        }

        private void Present(TutorialGestureSO gesture, IReadOnlyList<HelpTarget> targets, string text)
        {
            _presentedHand = Hand;
            _view?.Show(gesture, HandFor(gesture), text, Centroid(targets));
            _highlighter?.Highlight(targets);
        }

        private string Translate(string key)
        {
            if (string.IsNullOrEmpty(key) || _translate == null)
                return null;
            string text = _translate(key);
            return string.IsNullOrEmpty(text) ? null : text;
        }

        private XRNodeRole HandFor(TutorialGestureSO gesture)
        {
            switch (gesture != null ? gesture.Hand : TutorialGestureSO.HandChoice.Dominant)
            {
                case TutorialGestureSO.HandChoice.Left: return XRNodeRole.LeftHand;
                case TutorialGestureSO.HandChoice.Right: return XRNodeRole.RightHand;
                default: return Hand == DominantHand.Left ? XRNodeRole.LeftHand : XRNodeRole.RightHand;
            }
        }

        private static Vector3? Centroid(IReadOnlyList<HelpTarget> targets)
        {
            if (targets == null || targets.Count == 0)
                return null;
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < targets.Count; i++)
                sum += targets[i].Position;
            return sum / targets.Count;
        }

        private void Remove(Request request)
        {
            if (_requests.Remove(request))
                Render();
        }

        private sealed class Request : IDisposable
        {
            private HelpDirector _owner;

            public Request(HelpDirector owner, TutorialGestureSO gesture, IReadOnlyList<Transform> targets)
            {
                _owner = owner;
                Gesture = gesture;
                Targets = new List<HelpTarget>();
                if (targets == null)
                    return;
                foreach (Transform target in targets)
                {
                    if (target != null)
                        Targets.Add(HelpTarget.From(target));
                }
            }

            public TutorialGestureSO Gesture { get; }
            public List<HelpTarget> Targets { get; }

            public void Dispose()
            {
                HelpDirector owner = _owner;
                _owner = null;
                owner?.Remove(this);
            }
        }
    }
}
