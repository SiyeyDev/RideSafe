#if UNITY_EDITOR
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;



namespace StepCommand.Debugging
{
    [ExecuteAlways]
    [RequireComponent(typeof(AttemptsStepCommand))]
    public class AttempStepCommandDebug : StepCommandDebug<AttemptsStepCommand>
    {
        [HideLabel,ReadOnly][OdinSerialize,HideReferenceObjectPicker] private StepCommandWrapper _step;
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
            _step = GetVariable<StepCommandWrapper>(nameof(_step));
        }
        #endregion
        #region Debug Methods

        private string _methodName = "CompleteCommandValidation";
        [HorizontalGroup("")]
        [Button("Right")]
        public void CompleteRight()
        {
            _step.StepCommand.Exit();
            CallMethod(_methodName, true, _step.StepCommand);
        }
        [HorizontalGroup("")]
        [Button("Wrong")]
        public void CompleteWrong()
        {
            _step.StepCommand.Exit();
            CallMethod(_methodName, false, _step.StepCommand);
        }
        #endregion
    }
}
#endif