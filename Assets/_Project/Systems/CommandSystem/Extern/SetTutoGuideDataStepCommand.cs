using StepCommand;
using System;
using UnityEngine;

[Serializable]
public class SetTutoGuideDataStepCommand : StepCommandClass
{
    [SerializeField] private TutoUI _tutoUI;
    [SerializeField] private TutoGuideData _guideData;
    private TutoGuideData _lastTutoGuideData;
    #region ICommand Methods

    public override void Execute(Action<bool, IStepCommand> onComplete)
    {
        base.Execute(onComplete);
        _lastTutoGuideData = _tutoUI.GetCurrentData();
        _tutoUI.UpdateGuideData(_guideData);
        CoroutineCaller.Instance.WaitForNextFrame(() => Complete(true, this));
    }
    public override void InternalUndo()
    {
        _tutoUI.UpdateGuideData(_lastTutoGuideData);
        base.InternalUndo();
    }
    #endregion
}
