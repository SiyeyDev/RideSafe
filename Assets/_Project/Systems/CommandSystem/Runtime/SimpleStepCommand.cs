using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace StepCommand
{
    [AddComponentMenu("StepCommand/SimpleStepCommand")]
    public class SimpleStepCommand : SerializedMonoBehaviour, IStepInitialize
    {
        [LabelText("Step")]
        [HideReferenceObjectPicker, OdinSerialize] private StepCommandWrapper _stepCommandWrapper;
        [SerializeField] private bool _autoInitialize;
        [SerializeField] private bool _loop;

        #region Unity Methods
        private void Awake()
        {
            Init();
        }
        private void Start()
        {
            if (_autoInitialize)
                _stepCommandWrapper.StepCommand.Execute(Complete);
            else
                enabled = false;
        }
        #endregion
        #region IStepInitialize Methods
        public void Init()
        {
            enabled = true;
            _stepCommandWrapper.StepCommand.Initialize();
        }
        #endregion
        #region Main Methods
        private void Complete(bool isRight, IStepCommand stepCommand)
        {
            if (!_loop)
            {
                enabled = false;
                return;
            }
            this.WaitForNextFrame(() => _stepCommandWrapper.StepCommand.Execute(Complete));
        }
        #endregion
    }
}