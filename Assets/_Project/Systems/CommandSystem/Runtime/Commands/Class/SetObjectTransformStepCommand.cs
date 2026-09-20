using StepCommand.Debugging;
using System;
using UnityEngine;

namespace StepCommand
{
    [Serializable]
    public class SetObjectTransformStepCommand : StepCommandClass
    {
        [Header("SetObjectTransform Settings")]
        [SerializeField] private Transform _objectToMove;
        [SerializeField] private Transform _targetPosition;
        [SerializeField] private bool _matchPosition;
        [SerializeField] private bool _matchRotation;
        [SerializeField] private bool _matchScale;
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private Vector3 _startScale;

        #region ICommand Methods
        public override IStepCommand Initialize()
        {
            // Nulls References
            if (_objectToMove == null)
                Debug.LogError($"[SetObjectTransformStepCommand] El campo '_objectToMove' es NULL en el objeto: {this?.GetType().Name} dentro de {this}");
            if (_targetPosition == null)
                Debug.LogError($"[SetObjectTransformStepCommand] El campo '_targetPosition' es NULL en el objeto: {this?.GetType().Name} dentro de {this}");

            if (_matchPosition)
                _startPosition = _objectToMove.position;
            if (_matchRotation)
                _startRotation = _objectToMove.rotation;
            if (_matchScale)
                _startScale = _objectToMove.transform.lossyScale;
            return base.Initialize();
        }
        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            StepCommandDebugUtilities.DebugLog($"SETTING TRANSFORM for {StepCommandDebugUtilities.GetBoldColorMessage(_objectToMove.name, StepCommandDebugData.commandLableMainColor)}");
            base.Execute(onComplete);
            if (_matchPosition)
                _objectToMove.position = _targetPosition.position;
            if (_matchRotation)
                _objectToMove.rotation = _targetPosition.rotation;
            if (_matchScale)
                _objectToMove.transform.SetGlobalScale(_targetPosition.lossyScale);
            CoroutineCaller.Instance.WaitForNextFrame(() => Complete(true, this));
        }
        public override void InternalUndo()
        {
            if (_matchPosition)
                _objectToMove.position = _startPosition;
            if (_matchRotation)
                _objectToMove.rotation = _startRotation;
            if (_matchScale)
                _objectToMove.transform.SetGlobalScale(_startScale);
        }
        #endregion
    }
}
