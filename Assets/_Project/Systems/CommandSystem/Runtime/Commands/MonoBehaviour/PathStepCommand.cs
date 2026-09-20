using Sirenix.OdinInspector;
using Sirenix.Serialization;
using StepCommand.Debugging;
using System;
using UnityEngine;

namespace StepCommand
{
    [AddComponentMenu("StepCommand/PathStepCommand")]
    public class PathStepCommand : StepCommandMonoBehaviour
    {
        [Header("Path Settings")]
        [HideReferenceObjectPicker, OdinSerialize] private Path[] _paths;
        #region ICommand Methods
        public override IStepCommand Initialize()
        {
            foreach (Path path in _paths)
            {
                path.validationStepCommand.StepCommand.Initialize();
                path.pathStepCommand.StepCommand.Initialize();
            }
            return base.Initialize();
        }
        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            foreach (Path path in _paths)
            {
                StepCommandDebugUtilities.DebugLog($"EXECUTE {StepCommandDebugUtilities.GetBoldColorMessage(path.validationStepCommand.StepCommand.ToString(), StepCommandDebugData.commandLableMainColor)} in {gameObject.name}");
                path.ExecutePath(CompleteValidation, CompleteBlock);
            }
            base.Execute(onComplete);
        }
        public override void Exit()
        {
            foreach (Path path in _paths)
                path.ExitPath();
            base.Exit();
        }
        public override void InternalUndo()
        {
            foreach (Path path in _paths)
            {
                if (!path.DidValidation)
                    continue;
                path.UndoPath();
                break;
            }
            foreach (Path path in _paths)
            {
                if (!path.DidValidation)
                    path.UndoPath();
            }
            base.InternalUndo();
        }
        #endregion
        #region Main Methods
        private void CompleteValidation(bool right, IStepCommand stepCommand)
        {
            if (!right)
                return;
            StepCommandDebugUtilities.DebugLog($"Complete in {gameObject.name} path validation with {right} command {StepCommandDebugUtilities.GetBoldColorMessage(stepCommand.ToString(), StepCommandDebugData.commandLableMainColor)}");
            foreach (Path path in _paths)
            {
                if (path.validationStepCommand.StepCommand == stepCommand)
                    continue;
                path.ExitPath();
            }
        }
        private void CompleteBlock(bool right, Path path)
        {
            StepCommandDebugUtilities.DebugLog($"Complete in {gameObject.name} path with {right} command {StepCommandDebugUtilities.GetBoldColorMessage(path.pathStepCommand.StepCommand.ToString(), StepCommandDebugData.commandLableMainColor)}");
            Complete(right, path.pathStepCommand.StepCommand);
        }
        #endregion
        [Serializable]
        public class Path
        {
            [HideReferenceObjectPicker, OdinSerialize] public StepCommandWrapper validationStepCommand;
            [HideReferenceObjectPicker, OdinSerialize] public StepCommandWrapper pathStepCommand;
            private Action<bool, Path> _onComplete;
            public bool DidValidation { get; private set; }
            public void ExecutePath(Action<bool, IStepCommand> onCompleteValidation, Action<bool, Path> onCompleteBlock)
            {
                DidValidation = false;
                _onComplete = onCompleteBlock;
                onCompleteValidation += OnComplete;
                validationStepCommand.StepCommand.Execute(onCompleteValidation);
            }
            public void ExitPath()
            {
                if (DidValidation)
                    pathStepCommand.StepCommand.Exit();
                else
                    validationStepCommand.StepCommand.Exit();
            }
            public void UndoPath()
            {
                if (DidValidation)
                    pathStepCommand.StepCommand.Undo();
                validationStepCommand.StepCommand.Undo();
            }
            private void OnComplete(bool right, IStepCommand stepCommand)
            {
                if (!right)
                    return;
                DidValidation = true;
                Action<bool, IStepCommand> onComplete = (pathRight, path) => _onComplete.Invoke(pathRight, this);
                pathStepCommand.StepCommand.Execute(onComplete);
            }
        }
    }
}
