using Sirenix.OdinInspector;
using Sirenix.Serialization;
using StepCommand;
using StepCommand.Debugging;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StepCommandHandler : SerializedMonoBehaviour, ITask, IStepInitialize
{
    [PropertySpace(SpaceAfter = 16)]
    [HideReferenceObjectPicker, OdinSerialize] private StepCommandWrapper[] _tasks;
    [FoldoutGroup("Score")]
    [SerializeField] private bool _customScore;
    [FoldoutGroup("Score")]
    [ShowIf("_customScore")]
    [FoldoutGroup("Score")]
    [SerializeField] private FloatSharedData _currentScore;
    [ShowIf("_customScore")]
    [FoldoutGroup("Score")]
    [SerializeField] private FloatSharedData _targetScore;
    private bool _initialized;

    [Header("Events")]
    public UnityEvent onTaskInit;
    public UnityEvent<bool> onTaskCompleted;
    private Stack<StepCommandWrapper> _stepsToDo;
    private StepCommandWrapper _currentStep;
    public event Action<bool, IdTask> OnTaskCompleted;

    private void Awake()
    {
        _initialized = false;
        this.WaitForNextFrame(SetSteps);
    }
    private void Start()
    {
    }
    #region Task Methods
    [ContextMenu("Start")]
    public void Init()
    {
        SetSteps();
        _initialized = true;
        _currentStep = null;
        ProcessNextStep(true);
        onTaskInit?.Invoke();
    }
    private void Complete(bool succesfull)
    {
        SendScore();
        onTaskCompleted?.Invoke(succesfull);
        OnTaskCompleted?.Invoke(succesfull, ID);
        _initialized = false;
        _stepsToDo = null;
    }

    private void SendScore()
    {
        if (!GameManagerBridge.HasInstance)
            return;
#if UNITY_EDITOR
        StepCommandDebugUtilities.DebugLog($"Set Score {StepCommandDebugUtilities.GetBoldColorMessage(GetScore().ToString(), StepCommandDebugData.commandLableMainColor)} in {_idTask}");
#endif
        PlayerPrefs.SetFloat($"{Constants.k_playerPrefsScorePrefix}{_idTask}", GetScore());
    }


    public float GetScore()
    {
        float score = _customScore ? (_currentScore.GetValue() / _targetScore.GetValue()) * 100 : 100;
        return Mathf.Round(Math.Clamp(score, 0, 100));
    }


    #endregion
    #region Main Methods
    private void SetSteps()
    {
        if (_stepsToDo != null)
            return;
        _stepsToDo = new Stack<StepCommandWrapper>(_tasks.Length);
        for (int i = _tasks.Length - 1; i >= 0; i--)
        {
            _stepsToDo.Push(_tasks[i]);
            StepCommandDebugUtilities.DebugLog($"Intiailize {StepCommandDebugUtilities.GetBoldColorMessage(_tasks[i].StepCommand.ToString(), StepCommandDebugData.commandLableMainColor)} in {gameObject.name}");
            _tasks[i].StepCommand.Initialize();
        }
    }
    private void ProcessNextStep(bool right, IStepCommand stepCommand = null)
    {
        if (_stepsToDo.Count == 0)
        {
            Complete(true);
            return;
        }
        DoNextStep();
    }
    private void DoNextStep()
    {
        _currentStep = _stepsToDo.Pop();
        StepCommandDebugUtilities.DebugLog($"EXECUTE {StepCommandDebugUtilities.GetBoldColorMessage(_currentStep.GetGuideName(), StepCommandDebugData.commandLableMainColor)} in {gameObject.name}");
        _currentStep.StepCommand.Execute(ProcessNextStep);
    }
    public void ResetTask() => Init();
    public void SkipStep()
    {
        StepCommandDebugUtilities.DebugLog($"SKIP step {StepCommandDebugUtilities.GetBoldColorMessage(_currentStep.GetGuideName(), StepCommandDebugData.commandLableMainColor)} in {gameObject.name}");
        _currentStep.StepCommand.Exit();
        if (_stepsToDo.Count == 0)
        {
            Complete(true);
            return;
        }
        DoNextStep();
    }
    #endregion

    #region Iterface Implementation

    [SerializeField] private IdTask _idTask;
    public IdTask ID => _idTask;
    public bool IsActiveTask
    {
        get => _initialized;
        set => _initialized = value;
    }
    #endregion


}

