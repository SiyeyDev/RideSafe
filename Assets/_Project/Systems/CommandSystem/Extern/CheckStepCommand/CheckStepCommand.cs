using Check;
using Sirenix.OdinInspector;
using StepCommand;
using System;
using UnityEngine;
using UnityEngine.Events;

public class CheckStepCommand : ParralleBaseMonoBehaviourStepCommand
{
    [Header("Check Settings")]
    [SerializeField] private BaseCheck _check;
    [SerializeField] private UnityEvent _onCheck;
    private bool _doingRight;
    private void Awake()
    {
        _check.Initialize();
    }
    private void OnDestroy()
    {
        _check.Shutdown();
    }
    private void Update()
    {
        bool doingRight = _check.Evaluate();
        if (_doingRight == doingRight)
            return;
        Complete(doingRight, this);
        _doingRight = doingRight;
    }
    #region ICommand Methods
    public override void Execute(Action<bool, IStepCommand> onComplete)
    {
        _doingRight = false;
        base.Execute(onComplete);
    }
    protected override void Complete(bool right, IStepCommand stepCommand, bool callDelegate = true)
    {
        _onCheck?.Invoke();
        base.Complete(right, stepCommand, callDelegate);
    }
    #endregion
    #region ParralleBaseMonoBehaviour Methods
    public override bool IsExecuted() => _check.Evaluate();
    #endregion

#if UNITY_EDITOR
    [FoldoutGroup("Debug")]
    [SerializeField] private bool _useDebug;
    [FoldoutGroup("Debug")]
    [ShowIf(nameof(_useDebug))][SerializeField] private Color _GizmoColor = Color.white;
    [FoldoutGroup("Debug")]
    [ShowIf(nameof(_useDebug))][SerializeField] private bool _useWire;
    [FoldoutGroup("Debug")]
    [ShowIf(nameof(_useDebug))][SerializeField] private bool _onlySelected;

    private void OnDrawGizmos()
    {
        if (!_useDebug)
            return;
        if (_onlySelected)
            return;
        Gizmos.color = _GizmoColor;
        _check.DrawGizmos(_useWire);
    }
    private void OnDrawGizmosSelected()
    {
        if (!_useDebug)
            return;
        if (!_onlySelected)
            return;
        Gizmos.color = _GizmoColor;
        _check.DrawGizmos(_useWire);
    }
#endif

}
