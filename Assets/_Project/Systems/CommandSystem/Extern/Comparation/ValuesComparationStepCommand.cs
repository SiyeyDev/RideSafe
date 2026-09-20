using Sirenix.OdinInspector;
using StepCommand;
using System;
using UnityEngine;

[Serializable]
public class ValuesComparationStepCommand : StepCommandClass
{
    [Header("CheckScoreStepCommand Settings")]
    [LabelText("$GetName")]
    [HideReferenceObjectPicker][SerializeField] private IStepComparation comparation;
    #region IScore 
    public bool UsingScore() => true;
    #endregion
    #region ICommand Methods
    public override void Execute(Action<bool, IStepCommand> onComplete)
    {
        base.Execute(onComplete);
        CoroutineCaller.Instance.WaitForNextFrame(() => Complete(IsRight(), this));
    }
    #endregion
    private bool IsRight() => comparation.Evaluate();


#if UNITY_EDITOR
    private string GetName() => comparation.ToString();
#endif
}
