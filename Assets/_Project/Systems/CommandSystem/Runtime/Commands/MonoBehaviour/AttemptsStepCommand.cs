using Sirenix.OdinInspector;
using Sirenix.Serialization;
using StepCommand.Debugging;
using System;
using UnityEngine;

namespace StepCommand
{
    [AddComponentMenu("StepCommand/AttemptsStepCommand")]
    public class AttemptsStepCommand : StepCommandMonoBehaviour
    {
        [Header("Try Settings")]
        [HideReferenceObjectPicker, OdinSerialize] private StepCommandWrapper _step;
        [SerializeField] private int _maxAttempts;
        [SerializeField] private bool _shareLeftAttemps;
        [ShowIf("_shareLeftAttemps")][SerializeField] private FloatShareDataSO _leftAttempsData;
        [SerializeField] private bool _modifyFloat;
        [ShowIf("_modifyFloat")][SerializeField] private FloatShareDataSO _shareDataSO;
        [ShowIf("_modifyFloat")][SerializeField] private FloatSharedData _totalValue;
        private int _attempts;
        #region ICommand Methods
        public override IStepCommand Initialize()
        {
            _step.StepCommand.Initialize();
            return base.Initialize();
        }
        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            if (_shareLeftAttemps)
                _leftAttempsData.Value = _maxAttempts;
            _attempts = 0;
            _step.StepCommand.Execute(CompleteCommandValidation);
            base.Execute(onComplete);
        }
        public override void InternalUndo()
        {
            SetScore(false);
            base.InternalUndo();
        }


        protected override void Complete(bool right, IStepCommand stepCommand, bool callDelegate = true)
        {
            SetScore(true);
            base.Complete(right, stepCommand, callDelegate);
        }
        #endregion

        private void CompleteCommandValidation(bool right, IStepCommand stepCommand)
        {
            _attempts++;
            StepCommandDebugUtilities.DebugLog($"Complete Attemp {StepCommandDebugUtilities.GetBoldColorMessage(_step.GetGuideName(), StepCommandDebugData.commandLableMainColor)} with {right} in {gameObject.name}");
            if (right)
            {
                Complete(true, stepCommand);
                return;
            }
            if (_shareLeftAttemps)
                _leftAttempsData.Value--;
            StepCommandDebugUtilities.DebugLog($"Left Attemps {_attempts} / {_maxAttempts}  in {gameObject.name}");
            if (_attempts <= _maxAttempts)
            {
                _step.StepCommand.Undo();
                _step.StepCommand.Execute(CompleteCommandValidation);
                return;
            }
            Complete(false, stepCommand);
        }
        private void SetScore(bool addValue)
        {
            float scorePercentage = Mathf.InverseLerp(_maxAttempts + 2, 1, _attempts);
            if (!addValue)
                scorePercentage *= -1;
            _shareDataSO.Value += _totalValue.GetValue() * scorePercentage;
        }

    }
}