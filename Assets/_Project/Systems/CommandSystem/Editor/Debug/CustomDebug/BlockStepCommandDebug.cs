#if UNITY_EDITOR
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StepCommand.Debugging
{
    [ExecuteAlways]
    [RequireComponent(typeof(BlockStepCommand))]
    public class BlockStepCommandDebug : StepCommandDebug<BlockStepCommand>
    {
        [ReadOnly][ShowInInspector] private StepCommandWrapper[] _steps;

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
        private List<StepCommandWrapper> _stepsCompleted;

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
            _steps = GetVariable<StepCommandWrapper[]>(nameof(_steps));
            _stepsToDo = GetVariable<Stack<StepCommandWrapper>>(nameof(_stepsToDo))?.ToList();
            _stepToDoNames = GetNames(_stepsToDo);
            _stepsCompleted = GetVariable<Stack<StepCommandWrapper>>(nameof(_stepsCompleted))?.ToList();
            _stepsCompletedNames = GetNames(_stepsCompleted);
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