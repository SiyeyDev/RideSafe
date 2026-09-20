#if UNITY_EDITOR
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace StepCommand.Debugging
{
    [ExecuteAlways]
    [RequireComponent(typeof(ParallelStepCommand))]
    public class ParallelStepCommandDebug : StepCommandDebug<ParallelStepCommand>
    {
        [ReadOnly][ShowInInspector] private StepCommandWrapper[] _stepCommands;
        [TableList]
        [SerializeField] private ParallelStepCommandWrapper[] _parallelStepCommandWrappers;


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
            if (!IsActiveStep() || !IsPlaying())
                return;
            if (_parallelStepCommandWrappers == null)
                return;
            foreach (ParallelStepCommandWrapper parallelStepCommandWrapper in _parallelStepCommandWrappers)
                parallelStepCommandWrapper.UpdateData();

        }
        #endregion
        #region Debug Methods
        [Button("SetSteps")]
        private void GetSteps()
        {
            _stepCommands = GetVariable<StepCommandWrapper[]>(nameof(_stepCommands));
            _parallelStepCommandWrappers = new ParallelStepCommandWrapper[_stepCommands.Length];
            for (int i = 0; i < _stepCommands.Length; i++)
            {
                ParallelStepCommandWrapper parallelStep = new ParallelStepCommandWrapper(this, _stepCommands[i]);
                parallelStep.isCompleted = !parallelStep.isParallel ? true : parallelStep.stepCommandParallel.IsExecuted();
                _parallelStepCommandWrappers[i] = parallelStep;
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
        internal class ParallelStepCommandWrapper
        {
            [HideLabel, DisplayAsString(false), ReadOnly]
            public string stepCommandName;
            private StepCommandWrapper _stepCommand;
            public IParallelStepCommand stepCommandParallel;
            [TableColumnWidth(64, Resizable = false)]
            [VerticalGroup("Parallel")]
            [LabelText(""), ReadOnly]
            [GUIColor("ButtonColorParallel")]
            public bool isParallel;
            private Color ButtonColorParallel() => isParallel ? Color.green : Color.red;
            [TableColumnWidth(64, Resizable = false)]
            [VerticalGroup("ForceQuit")]
            [LabelText(""), ReadOnly]
            [ShowIf("isParallel")]
            [GUIColor("ButtonColorForceQuit")]
            public bool forceQuit;
            private Color ButtonColorForceQuit() => forceQuit ? Color.green : Color.red;
            [TableColumnWidth(64, Resizable = false)]
            [VerticalGroup("Completed")]
            [LabelText(""), ReadOnly]
            [GUIColor("ButtonColorCompleted")]
            public bool isCompleted;
            private Color ButtonColorCompleted() => isCompleted ? Color.green : Color.red;

            private ParallelStepCommandDebug _parallelStepCommandDebug;

            public ParallelStepCommandWrapper(ParallelStepCommandDebug parallelStepCommandDebug, StepCommandWrapper stepCommand)
            {
                _parallelStepCommandDebug = parallelStepCommandDebug;
                _stepCommand = stepCommand;
                stepCommandParallel = stepCommand.StepCommand as IParallelStepCommand;
                IParallelStepCommand paralleStepCommand = _stepCommand.StepCommand as IParallelStepCommand;
                stepCommandName = _parallelStepCommandDebug.GetName(_stepCommand);
                isParallel = paralleStepCommand != null;
                if (isParallel)
                    forceQuit = paralleStepCommand.ForceQuit();
            }

            public void UpdateData()
            {
                stepCommandParallel = _stepCommand.StepCommand as IParallelStepCommand;
                IParallelStepCommand paralleStepCommand = _stepCommand.StepCommand as IParallelStepCommand;
                isParallel = paralleStepCommand != null;
                if (isParallel)
                    forceQuit = paralleStepCommand.ForceQuit();
            }
            [TableColumnWidth(256, Resizable = false)]
            [Button("Complete")]
            public void Complete()
            {
                _parallelStepCommandDebug.CallMethod("Complete", true, _stepCommand);
            }

        }
    }
}
#endif