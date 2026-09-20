#if UNITY_EDITOR
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StepCommand.Debugging
{
    [ExecuteAlways]
    [RequireComponent(typeof(CompleteMultiplesStepCommand))]
    public class CompleteMultiplesStepCommandDebug : StepCommandDebug<CompleteMultiplesStepCommand>
    {
        [ReadOnly][ShowInInspector] private StepCommandWrapper[] _stepCommands;
        [TableList]
        [SerializeField] private StepInMultipleWrapper[] _singleSteps;
        private HashSet<IStepCommand> _remainingSteps;

        #region Unity Methods
        private void Start()
        {
            GetSteps();
        }
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
            _remainingSteps = GetVariable<HashSet<IStepCommand>>(nameof(_remainingSteps));
            if (!IsActiveStep() || !IsPlaying())
                return;
            if (_singleSteps == null || _remainingSteps == null)
                return;
            foreach (StepInMultipleWrapper StepINMutiple in _singleSteps)
                StepINMutiple.isCompleted = !_remainingSteps.Contains(StepINMutiple.stepCommand.StepCommand);
        }
        #endregion
        #region Debug Methods
        [Button("SetSteps")]
        private void GetSteps()
        {
            _stepCommands = GetVariable<StepCommandWrapper[]>(nameof(_stepCommands));
            _remainingSteps = GetVariable<HashSet<IStepCommand>>(nameof(_remainingSteps));
            _singleSteps = new StepInMultipleWrapper[_stepCommands.Length];
            for (int i = 0; i < _stepCommands.Length; i++)
            {
                StepInMultipleWrapper StepINMutiple = new StepInMultipleWrapper(this, _stepCommands[i]);
                if (_remainingSteps != null)
                    StepINMutiple.isCompleted = !_remainingSteps.Contains(_stepCommands[i].StepCommand);
                _singleSteps[i] = StepINMutiple;
            }
        }
        [ShowIf("@IsActiveStep() && IsPlaying()")]
        [Button("Complete"), GUIColor(0, 1f, 0f, 1f)]
        private void Complete()
        {
            DebugLog($"Comple {stepCommand.gameObject.name}");
            CallMethod("Complete", true);
        }
        #endregion

        [Serializable]
        internal class StepInMultipleWrapper
        {
            [HideLabel, DisplayAsString(false), ReadOnly]
            public string stepCommandName;
            [HideInInspector, OdinSerialize] public StepCommandWrapper stepCommand;
            [TableColumnWidth(64, Resizable = false)]
            [VerticalGroup("ForceQuit")]
            [LabelText(""), ReadOnly]
            [ShowIf("@stepCommand.StepCommand as IParallelStepCommand")]
            [GUIColor("ButtonColorForceQuit")]
            public bool forceQuit;
            private Color ButtonColorForceQuit() => forceQuit ? Color.green : Color.red;
            [TableColumnWidth(64, Resizable = false)]
            [VerticalGroup("Completed")]
            [LabelText(""), ReadOnly]
            [GUIColor("ButtonColorCompleted")]
            public bool isCompleted;
            private Color ButtonColorCompleted() => isCompleted ? Color.green : Color.red;

            private CompleteMultiplesStepCommandDebug _compleMultiplesStepCommandDebug;

            public StepInMultipleWrapper(CompleteMultiplesStepCommandDebug parallelStepCommandDebug, StepCommandWrapper stepCommand)
            {
                _compleMultiplesStepCommandDebug = parallelStepCommandDebug;
                this.stepCommand = stepCommand;
                stepCommandName = _compleMultiplesStepCommandDebug.GetName(this.stepCommand);
                if (this.stepCommand.StepCommand is IParallelStepCommand paralleStepCommand)
                    forceQuit = paralleStepCommand.ForceQuit();
            }
            [TableColumnWidth(256, Resizable = false)]
            [Button("Complete")]
            public void Complete()
            {
                _compleMultiplesStepCommandDebug.CallMethod("CompletStepCommand", true, stepCommand);
            }

        }
    }
}
#endif