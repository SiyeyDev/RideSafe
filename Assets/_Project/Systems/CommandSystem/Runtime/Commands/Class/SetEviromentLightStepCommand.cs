using System;
using UnityEngine;

namespace StepCommand
{
    [Serializable]
    public class SetEviromentLightStepCommand : StepCommandClass
    {
        [Header("SetEviromentLight Settings")]
        [SerializeField] private float _lightValue;
        private float _prewLightValue;
        #region ICommand Methods

        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            _prewLightValue = RenderSettings.ambientIntensity;
            RenderSettings.ambientIntensity = _lightValue;
            base.Execute(onComplete);
            CoroutineCaller.Instance.WaitForNextFrame(() => Complete(true, this));
        }
        public override void InternalUndo()
        {
            RenderSettings.ambientIntensity = _prewLightValue;
            base.InternalUndo();
        }
        #endregion
    }
}
