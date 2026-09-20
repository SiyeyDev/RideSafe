#if UNITY_EDITOR
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace StepCommand.Debugging
{
    [ExecuteAlways]
    [RequireComponent(typeof(TreeStepCommand))]
    public class TreeStepCommandDebug : StepCommandDebug<TreeStepCommand>
    {
        [FoldoutGroup("Validation")]
        [HideLabel][SerializeField] private CommandWrapper _stepCommandValidation;
        [FoldoutGroup("Tree"), HorizontalGroup("Tree/Tree")]
        [BoxGroup("Tree/Tree/Right")]
        [HideLabel][SerializeField] private CommandWrapper _commandRight;
        [FoldoutGroup("Tree"), HorizontalGroup("Tree/Tree")]
        [BoxGroup("Tree/Tree/Wrong")]
        [HideLabel][SerializeField] private CommandWrapper _commandWrong;

        #region Unity Methods
        private void Update()
        {
            SetValue();
        }
        #endregion
        #region Main Methods
        private void SetValue()
        {
            if (stepCommand == null)
                return;
            _stepCommandValidation = new CommandWrapper(this, GetVariable<StepCommandWrapper>(nameof(_stepCommandValidation)), "CompleteCommandValidation");
            _commandRight = new CommandWrapper(this, GetVariable<StepCommandWrapper>(nameof(_commandRight)), "CompleteToDoStep");
            _commandWrong = new CommandWrapper(this, GetVariable<StepCommandWrapper>(nameof(_commandWrong)), "CompleteToDoStep");
        }
        #endregion
        [Serializable]
        internal class CommandWrapper
        {
            [HorizontalGroup("Validation")]
            [HideLabel, DisplayAsString(false), ReadOnly]
            [GUIColor(nameof(ButtonColorCompleted))]
            public string stepCommandName;
            private StepCommandWrapper _stepCommand;

            private TreeStepCommandDebug _treeStepCommand;
            private string _methodName;

            public CommandWrapper(TreeStepCommandDebug treeStepCommandDebug, StepCommandWrapper stepCommand, string methodName)
            {
                _treeStepCommand = treeStepCommandDebug;
                _stepCommand = stepCommand;
                stepCommandName = _treeStepCommand.GetName(_stepCommand);
                _methodName = methodName;
            }
            [ShowIf(nameof(IsActive))]
            [TableColumnWidth(256, Resizable = false)]
            [HorizontalGroup("Validation")]
            [Button("Right")]
            public void CompleteRight()
            {
                _stepCommand.StepCommand.Exit();
                _treeStepCommand.CallMethod(_methodName, true, _stepCommand.StepCommand);
            }
            [ShowIf(nameof(IsActive))]
            [TableColumnWidth(256, Resizable = false)]
            [HorizontalGroup("Validation")]
            [Button("Wrong")]
            public void CompleteWrong()
            {
                _stepCommand.StepCommand.Exit();
                _treeStepCommand.CallMethod(_methodName, false, _stepCommand.StepCommand);
            }

            private bool IsActive()
            {
                if (_treeStepCommand == null)
                    return false;
                if (!_treeStepCommand.IsActiveStep())
                    return false;
                IStepCommand currentStep = _treeStepCommand.GetVariable<IStepCommand>("_stepCommand");
                if (currentStep == null)
                    return false;
                return currentStep == _stepCommand.StepCommand;
            }
            private Color ButtonColorCompleted() => IsActive() ? Color.green : Color.red;
        }
    }
}
#endif