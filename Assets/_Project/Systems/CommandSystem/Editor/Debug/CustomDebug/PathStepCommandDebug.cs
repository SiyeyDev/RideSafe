#if UNITY_EDITOR
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace StepCommand.Debugging
{
    [ExecuteAlways]
    [RequireComponent(typeof(PathStepCommand))]
    public class PathStepCommandDebug : StepCommandDebug<PathStepCommand>
    {
        private PathStepCommand.Path[] _paths;
        [TableList]
        [HideReferenceObjectPicker, OdinSerialize] private PathStepCommandWrapper[] _parallelStepCommandWrappers;


        #region Unity Methods
        private void Start()
        {
            GetPaths();
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
            _paths = GetVariable<PathStepCommand.Path[]>(nameof(_paths));
            if (!IsActiveStep() || !IsPlaying())
            {
                _paths = null;
                return;
            }
            CheckPaths();
        }
        private void GetPaths()
        {
            _paths = GetVariable<PathStepCommand.Path[]>(nameof(_paths));
            _parallelStepCommandWrappers = new PathStepCommandWrapper[_paths.Length];
            for (int i = 0; i < _paths.Length; i++)
            {
                PathStepCommandWrapper parallelStep = new PathStepCommandWrapper(this, _paths[i]);
                _parallelStepCommandWrappers[i] = parallelStep;
            }
        }
        private void CheckPaths()
        {
            if (_paths == null)
                return;
            bool anyValidated = false;

            foreach (PathStepCommandWrapper pathStepCommandWrapper in _parallelStepCommandWrappers)
            {
                if (!pathStepCommandWrapper.DidValidation())
                    continue;
                anyValidated = true;
                break;
            }
            foreach (PathStepCommandWrapper pathStepCommandWrapper in _parallelStepCommandWrappers)
            {
                if (anyValidated)
                    pathStepCommandWrapper.active = pathStepCommandWrapper.DidValidation();
                else
                    pathStepCommandWrapper.active = true;
            }
        }
        #endregion
        [Serializable]
        internal class PathStepCommandWrapper
        {
            [VerticalGroup("Validation")]
            [HorizontalGroup("Validation/Row", Width = 0.8f)]
            [LabelText(""), ReadOnly]
            [GUIColor("ButtonColorValidation")]
            public StepCommandWrapper stepCommandValidation;
            private Color ButtonColorValidation() => NotDidValidationAndActive() ? Color.green : Color.red;

            [VerticalGroup("Path")]
            [HorizontalGroup("Path/Row", Width = 0.8f)]
            [LabelText(""), ReadOnly]
            [GUIColor("ButtonColorPath")]
            public StepCommandWrapper pathStepCommand;
            private Color ButtonColorPath() => DidValidation() ? Color.green : Color.red;


            private PathStepCommandDebug _pathStepCommandDebug;
            private PathStepCommand.Path _path;
            [HideInInspector] public bool active;


            public PathStepCommandWrapper(PathStepCommandDebug parallelStepCommandDebug, PathStepCommand.Path path)
            {
                _pathStepCommandDebug = parallelStepCommandDebug;
                _path = path;
                stepCommandValidation = path.validationStepCommand;
                pathStepCommand = path.pathStepCommand;
                active = true;
            }
            [VerticalGroup("Validation")]
            [HorizontalGroup("Validation/Row", Width = 0.2f)]
            [ShowIf(nameof(NotDidValidationAndActive))]
            [Button("Select")]
            private void SelectPath()
            {
                stepCommandValidation.StepCommand.Exit();
                _pathStepCommandDebug.CallMethod("CompleteValidation", true, stepCommandValidation.StepCommand);
                _pathStepCommandDebug.CallMethod(_path, "OnComplete", true, stepCommandValidation.StepCommand);
            }

            [VerticalGroup("Path")]
            [HorizontalGroup("Path/Row", Width = 0.8f)]
            [ShowIf(nameof(DidValidation))]
            [Button("Complete")]
            private void Complete()
            {
                pathStepCommand.StepCommand.Exit();
                _pathStepCommandDebug.CallMethod("CompleteBlock", true, _path);
            }
            public bool DidValidation()
            {
                if (_pathStepCommandDebug == null)
                    return false;
                if (_path == null)
                    return false;
                return _path.DidValidation;
            }
            private bool NotDidValidationAndActive() => !DidValidation() && active;
        }
    }
}
#endif