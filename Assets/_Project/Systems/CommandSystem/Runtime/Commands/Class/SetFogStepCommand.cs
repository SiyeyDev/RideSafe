using StepCommand;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetFogStepCommand : MonoBehaviour
{
    [Serializable]
    public class SetEviromentLightStepCommand : StepCommandClass
    {
        [Header("SetEviromentLight Settings")]
        [SerializeField] private bool _FogVisible;
        private float _prewLightValue;
        #region ICommand Methods

        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            _prewLightValue = RenderSettings.ambientIntensity;
            RenderSettings.fog = _FogVisible;
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

