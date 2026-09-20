#if UNITY_EDITOR
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StepCommand.Debugging
{
    [ExecuteAlways]
    [RequireComponent(typeof(StepCommandHandler))]
    public class StepCommandHandlerDebug : StepCommandDebug<StepCommandHandler>
    {
        [ReadOnly][ShowInInspector] private StepCommandWrapper[] _tasks;

        [ShowIf("$IsPlaying")]
        [HorizontalGroup("Block Debug")]
        [BoxGroup("Block Debug/To do", centerLabel: true), GUIColor(1, 0f, 0f, 1f)]
        [HideLabel, DisplayAsString(false)]
        [SerializeField] private string _stepToDoNames;
        private List<StepCommandWrapper> _stepsToDo;

        [ShowIf("$IsPlaying")]
        [HorizontalGroup("Block Debug")]
        [BoxGroup("Block Debug/Current", centerLabel: true), GUIColor(0, 1f, 1f, 1f)]
        [HideLabel, DisplayAsString(false)]
        [SerializeField] private string _curretnStepName;
        private StepCommandWrapper _currentStep;

        [ShowIf("$IsPlaying")]
        [HorizontalGroup("Block Debug")]
        [BoxGroup("Block Debug/Completed", centerLabel: true), GUIColor(0f, 1f, 0f, 1f)]
        [HideLabel, DisplayAsString(false)]
        [SerializeField] private string _stepsCompletedNames;

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
            _tasks = GetVariable<StepCommandWrapper[]>(nameof(_tasks));
            _stepsToDo = GetVariable<Stack<StepCommandWrapper>>(nameof(_stepsToDo))?.ToList();
            _stepToDoNames = GetNames(_stepsToDo);
            List<StepCommandWrapper> stepsCompleted = _tasks?.Except(_stepsToDo ?? Enumerable.Empty<StepCommandWrapper>()).ToList();
            _stepsCompletedNames = GetNames(stepsCompleted);
            _currentStep = GetVariable<StepCommandWrapper>(nameof(_currentStep));
            _curretnStepName = GetName(_currentStep);
        }
        #endregion
        #region Debug Methods
        [ShowIf("@IsActiveStep() && IsPlaying()")]
        [Button("Skip")]
        private void Skip()
        {
            _currentStep.StepCommand.Exit();
            CallMethod("ProcessNextStep", true, _currentStep.StepCommand);
        }
        #endregion
    }
}
#endif