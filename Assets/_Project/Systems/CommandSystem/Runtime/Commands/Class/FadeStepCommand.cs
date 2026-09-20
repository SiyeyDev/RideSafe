using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace StepCommand
{
    [Serializable]
    public class FadeStepCommand : StepCommandClass
    {
        [Header("FadeStep Settings")]
        [SerializeField] private bool _halfFade;
        [ShowIf("_halfFade")][SerializeField] private bool _doOut;
        [ShowIf("_halfFade")][SerializeField] private float _transitionTime = 1;
        [ShowIf("_halfFade")][SerializeField] private Color _color = Color.black;
        [HideIf("_halfFade")][SerializeField] private bool _useData;
        [ShowIf("@_useData && !_halfFade")][SerializeField] private FadeSettingsData _fadeSettingsData;
        [HideIf("@_halfFade || _useData")][SerializeField] private FadeSettings _fadeSettings;
        [HideIf("_halfFade")][SerializeField] private bool _endInTheMiddle;
        #region ICommand Methods
        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            base.Execute(onComplete);
            if (_halfFade)
            {
                if (_doOut)
                    FadeUI.FadeOut(_transitionTime, _color, FadeCompleted);
                else
                    FadeUI.FadeIn(_transitionTime, _color, FadeCompleted);
                return;
            }
            if (_endInTheMiddle)
            {
                FadeUI.Fade(_useData ? _fadeSettingsData.FadeSettings : _fadeSettings, actionInTheMiddle: FadeCompleted);
                return;
            }
            FadeUI.Fade(_useData ? _fadeSettingsData.FadeSettings : _fadeSettings, FadeCompleted);
        }
        #endregion
        private void FadeCompleted() => Complete(true, this);
    }
}
