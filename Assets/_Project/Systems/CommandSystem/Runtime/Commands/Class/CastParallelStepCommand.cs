using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace StepCommand
{
    [Serializable]
    public class CastParallelStepCommand : StepCommandClass, IParallelStepCommand
    {
        [Header("CastParallel Settings")]
        [SerializeField] private bool _forceQuit;
        [ValidateInput(nameof(ValidateParallel), "Target wrapper is already a ParallelStep. this commadn is redudant")]
        [HideLabel]
        [SerializeField, HideReferenceObjectPicker] private StepCommandWrapper _targetCommmand;

        private bool _isExecuted;
        #region ICommand Methods
        public override IStepCommand Initialize()
        {
            _targetCommmand.StepCommand.Initialize();
            return base.Initialize();
        }
        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            _isExecuted = false;
            onComplete += (right, command) => _isExecuted = true;
            base.Execute(onComplete);
            _targetCommmand.StepCommand.Execute(Complete);
        }
        public override void Exit()
        {
            _isExecuted = true;
            _targetCommmand.StepCommand.Exit();
            base.Exit();
        }
        public override void InternalUndo()
        {
            _isExecuted = false;
            _targetCommmand.StepCommand.Undo();
            base.InternalUndo();
        }
        protected override void Complete(bool right, IStepCommand stepCommand) => base.onComplete(right, this);
        #endregion
        #region IParallelComand Methods
        public bool ActiveParrallel { get; set; }
        public bool ForceQuit() => _forceQuit;
        public bool IsExecuted() => !_isExecuted;
        public void CompleteParallel() { }
        #endregion
        private bool ValidateParallel()
        {
            if (_targetCommmand == null)
                return true;
            return _targetCommmand.StepCommand is not IParallelStepCommand;
        }
    }
}