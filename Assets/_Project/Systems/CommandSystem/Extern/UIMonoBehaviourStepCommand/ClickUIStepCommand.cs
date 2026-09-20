using StepCommand;
using System;

[Serializable]
public class ClickUIStepCommand : ParralleBaseActionStepCommand<UIHandButton>
{
    private Action _right;
    #region BaseAction Reusable Methods
    protected override void Set(bool state)
    {
        if (state)
            action.Active();
        else
            action.Deactive();
    }
    protected override void AddDelegates(Action right, Action wrong)
    {
        _right = right;
        action.Click += OnClick;
    }
    protected override void RemoveDelegates(Action right, Action wrong)
    {
        _right = null;
        action.Click -= OnClick;
    }
    #endregion
    private void OnClick(UIHandButton handBUtton) => _right?.Invoke();
}
