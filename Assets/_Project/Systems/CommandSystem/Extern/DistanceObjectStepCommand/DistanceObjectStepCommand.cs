using StepCommand;
using System;

[Serializable]
public class DistanceObjectStepCommand : ParralleBaseActionStepCommand<DistanceObject>
{
    #region BaseAction Reusable Methods
    protected override void Set(bool state) => action.Set(state);

    protected override void AddDelegates(Action right, Action wrong)
    {
        action.onSelect += right;
        action.onDeselect += wrong;
    }
    protected override void RemoveDelegates(Action right, Action wrong)
    {
        action.onSelect -= right;
        action.onDeselect -= wrong;
    }
    #endregion
}
