using System.Collections.Generic;
using UnityEngine;

namespace StepCommand
{
    internal static class StepCommandUtils
    {
        internal static void HandleReverse(this IStepCommand stepCommand, ref StepCommandWrapper _currentStep, ref Stack<StepCommandWrapper> _stepsCompleted, ref Stack<StepCommandWrapper> _stepsToDo)
        {
            int reverseAmount = _currentStep.StepCommand.ReverseAmount;
            if (reverseAmount > _stepsCompleted.Count)
            {
                Debug.LogWarning($"The Command {_currentStep} need revert {reverseAmount} commands, but only ther are {_stepsCompleted.Count} completed");
                reverseAmount = _stepsCompleted.Count;
            }
            for (int i = 0; i < reverseAmount; i++)
            {
                StepCommandWrapper stepToBack = _stepsCompleted.Pop();
                stepToBack.StepCommand.Undo();
                _stepsToDo.Push(stepToBack);
            }
            _currentStep = _stepsToDo.Pop();
        }
        internal static bool ShouldResetMethod(this IStepCommand stepCommand)
        {
            if(stepCommand is not IParallelStepCommand parallelStepCommand)
                return true;
            if(!parallelStepCommand.ActiveParrallel) 
                return true;
            return false;
        }
    }
}