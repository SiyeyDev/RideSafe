#if UNITY_EDITOR
using Sirenix.OdinInspector;
using UnityEngine;

namespace StepCommand.Debugging
{
    [ExecuteAlways]
    [RequireComponent(typeof(ValidateStepCommand))]

    public class ValidateStepCommandDebug : StepCommandDebug<ValidateStepCommand>
    {
        [ShowIf("$IsPlaying")]
        [HorizontalGroup("Block Debug")]
        [BoxGroup("Block Debug/Validation", centerLabel: true), GUIColor("ValidationColor")]
        [HideLabel, DisplayAsString(false)]
        [SerializeField] private string _stepCommandValidationName;
        private StepCommandWrapper _stepCommandValidation;
        private Color ValidationColor()
        {
            if (_stepCommand == null)
                return Color.black;
            return _stepCommand == _stepCommandValidation.StepCommand ? Color.green : Color.red;
        }

        [ShowIf("$IsPlaying")]
        [HorizontalGroup("Block Debug")]
        [BoxGroup("Block Debug/Step", centerLabel: true), GUIColor("CommandColor")]
        [HideLabel, DisplayAsString(false)]
        [SerializeField] private string _commandToDoName;
        private StepCommandWrapper _commandToDo;
        private Color CommandColor()
        {
            if (_stepCommand == null)
                return Color.black;
            return _stepCommand == _commandToDo.StepCommand ? Color.green : Color.red;
        }

        [ShowIf("@IsActiveStep() && IsPlaying()")]
        [HideLabel, DisplayAsString(false)]
        [InlineButton("CompleteRightValidation", "Right")]
        [InlineButton("CompleteWrongValidation", "Wrong")]
        [SerializeField] private string _stepCommandName;
        private IStepCommand _stepCommand;
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
            _stepCommandValidation = GetVariable<StepCommandWrapper>(nameof(_stepCommandValidation));
            _stepCommandValidationName = GetName(_stepCommandValidation);
            _commandToDo = GetVariable<StepCommandWrapper>(nameof(_commandToDo));
            _commandToDoName = GetName(_commandToDo);
            _stepCommand = GetVariable<IStepCommand>(nameof(_stepCommand));
            _stepCommandName = _stepCommandValidation.StepCommand == _stepCommand ? _stepCommandValidationName : _commandToDoName;
        }
        #endregion
        #region Debug Methods

        private void CompleteRightValidation()
        {
            if (_stepCommand == _stepCommandValidation.StepCommand)
            {
                CallMethod("CompleteCommandValidation", GetVariable<bool>("_shouldBeRight"), _stepCommand);
                return;
            }
        }
        private void CompleteWrongValidation()
        {
            if (_stepCommand == _stepCommandValidation.StepCommand)
            {
                CallMethod("CompleteCommandValidation", !GetVariable<bool>("_shouldBeRight"), _stepCommand);
                return;
            }
            _stepCommand.Exit();
            CallMethod("CompleteToDoStep", true, _stepCommand);
        }
        #endregion
    }
}
#endif