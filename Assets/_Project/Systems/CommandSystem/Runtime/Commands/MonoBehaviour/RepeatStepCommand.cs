using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace StepCommand
{
    [AddComponentMenu("StepCommand/RepeatStepCommand")]
    public class RepeatStepCommand : BlockStepCommand
    {
        [SerializeField] private bool _isInfinity;
        [HideIf("_isInfinity")][SerializeField] private int _amountRepetitions;
        private int _repeatCount;

        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            _repeatCount = 0;
            base.Execute(onComplete);
        }
        protected override void Complete(bool right, IStepCommand stepCommand, bool callDelegate = true)
        {
            _repeatCount++;
            if (_isInfinity ||_repeatCount < _amountRepetitions)
            {
                Repeat();
                return;
            }
            base.Complete(right, stepCommand, callDelegate);
        }

        private void Repeat()
        {
            SetSteps(false);
            ProcessNextStep();
        }
    }
}

