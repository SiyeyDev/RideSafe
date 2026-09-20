using Sirenix.OdinInspector;
using StepCommand.Debugging;
using UnityEngine;

namespace StepCommand
{
    [AddComponentMenu("StepCommand/SequenceParallelStepCommand")]
    public class SequenceParallelStepCommand : ParallelStepCommand
    {
        [ReadOnly, ShowInInspector] private int _index;
        #region ICommand Methods

        protected override void Complete(bool right, IStepCommand stepCommand, bool callDelegate = true)
        {
            StepCommandDebugUtilities.DebugLog($"Complete in {gameObject.name} sequence parallalel with {right} command {StepCommandDebugUtilities.GetBoldColorMessage(stepCommand.ToString(), StepCommandDebugData.commandLableMainColor)}");
            if (!right)
            {
                HandleWrongCommand(stepCommand);
                return;
            }
            if (_index == _stepCommands.Length)
            {
                Complete(true);
                return;
            }
            ProccesNextCommand();
        }
        #endregion
        #region ParallelStepCommand Reusable Methods
        protected override void InternalExecute()
        {
            _index = 0;
            ProccesNextCommand();
        }
        #endregion
        #region Main Methods
        private void ProccesNextCommand()
        {
            IStepCommand stepCommand = _stepCommands[_index].StepCommand;
            _index++;
            StepCommandDebugUtilities.DebugLog($"EXECUTE {StepCommandDebugUtilities.GetBoldColorMessage(stepCommand.ToString(), StepCommandDebugData.commandLableMainColor)} in {gameObject.name} Sequence Parallel");
            stepCommand.Execute(_onCompletedInternalStep);
        }
        private void HandleWrongCommand(IStepCommand completeCommand)
        {
            ExitLastCommand();
            if (_index > 0)
                FindWrongCommand();
            ProccesNextCommand();
        }
        private void ExitLastCommand()
        {
            _index--;
            _stepCommands[_index].StepCommand.Exit();
            _stepCommands[_index].StepCommand.Undo();
        }
        private void FindWrongCommand()
        {
            for (int i = _index - 1; i >= 0; i--)
            {
                _stepCommands[i].StepCommand.Undo();
                _index = i;
                if (_stepCommands[i].StepCommand is not IParallelStepCommand parrallelStepCommand)
                    continue;
                if (parrallelStepCommand.IsExecuted())
                    continue;
                break;
            }
        }
        protected override bool AllStepCommandsCompleted() => _index > _stepCommands.Length;
        #endregion
    }
}