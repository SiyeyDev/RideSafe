#if UNITY_EDITOR
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace StepCommand.Debugging
{
    [ExecuteAlways]
    [RequireComponent(typeof(SequenceParallelStepCommand))]
    public class SequenceParallelStepCommandDebug : StepCommandDebug<SequenceParallelStepCommand>
    {
        [ReadOnly][ShowInInspector] private StepCommandWrapper[] _stepCommands;

        [ShowIf("$IsPlaying")]
        [HorizontalGroup("Block Debug")]
        [BoxGroup("Block Debug/To do", centerLabel: true), GUIColor(1, 0f, 0f, 1f)]
        [HideLabel, DisplayAsString(false)]
        [SerializeField] private string _stepToDoNames;

        [ShowIf("$IsPlaying")]
        [HorizontalGroup("Block Debug")]
        [BoxGroup("Block Debug/Current", centerLabel: true), GUIColor(0, 1f, 1f, 1f)]
        [HideLabel, DisplayAsString(false)]
        [SerializeField] private string _curretnStepName;

        [ShowIf("$IsPlaying")]
        [HorizontalGroup("Block Debug")]
        [BoxGroup("Block Debug/Completed", centerLabel: true), GUIColor(0f, 1f, 0f, 1f)]
        [HideLabel, DisplayAsString(false)]
        [SerializeField] private string _stepsCompletedNames;

        private StepCommandWrapper _currentStep;
        [SerializeField, ReadOnly] private int _index = -1;

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
            _stepCommands = GetVariable<StepCommandWrapper[]>(nameof(_stepCommands));
            int index = GetVariable<int>(nameof(_index));
            if (index == _index || index >= _stepCommands.Length)
                return;
            _index = index - 1;
            if (_index < 0)
                _index = 0;
            List<StepCommandWrapper> stepsToDo = new List<StepCommandWrapper>();
            List<StepCommandWrapper> stepsCompleted = new List<StepCommandWrapper>();
            for (int i = 0; i < _stepCommands.Length; i++)
            {
                if (i > _index)
                    stepsToDo.Add(_stepCommands[i]);
                if (i < _index)
                    stepsCompleted.Add(_stepCommands[i]);
            }
            _stepToDoNames = GetNames(stepsToDo);
            _stepsCompletedNames = GetNames(stepsCompleted);
            
            _currentStep = _stepCommands[_index];
            _curretnStepName = GetName(_currentStep);
        }
        #endregion
        #region Debug Methods
        [ShowIf("@IsActiveStep() && IsPlaying()")]
        [Button("Complete"), GUIColor(0, 1f, 0f, 1f)]
        private void Complete()
        {
            _currentStep.StepCommand.Exit();
            DebugLog($"Complete {stepCommand.gameObject.name}");
            CallMethod("Complete", true);

        }
        #endregion
    }
}

#endif