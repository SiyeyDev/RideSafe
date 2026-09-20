#if UNITY_EDITOR
using Sirenix.OdinInspector;
using StepCommand.Debugging;
using System.Collections.Generic;
using UnityEngine;

namespace StepCommand.Editor
{
    public class EditorParallelStepCommandNode : EditorStepCommandNode
    {
        [FoldoutGroup("Steps Parallel", Order = 0)]
        [SerializeField, ReadOnly] private List<StepCommandWrapper> _parallelCommands;

        [FoldoutGroup("Steps Parallel", Order = 0)]
        [SerializeField, ReadOnly] private List<StepCommandWrapper> _notparallelComand;
        public EditorParallelStepCommandNode(ParallelStepCommand targetComponent) : base(targetComponent)
        {
            _parallelCommands = new List<StepCommandWrapper>();
            _notparallelComand = new List<StepCommandWrapper>();
        }
        #region EditorStepCommandNode Methods

        protected override StepCommandWrapper[] GetCommands()
        {
            StepCommandDebugUtilities.TryGetVariable(TargetComponent, "_stepCommands", out StepCommandWrapper[] steps);
            foreach (StepCommandWrapper stepCommandWrapper in steps)
            {
                if (stepCommandWrapper.StepCommand is IParallelStepCommand parrallelStepCommand)
                    _parallelCommands.Add(stepCommandWrapper);
                else
                    _notparallelComand.Add(stepCommandWrapper);
            }
            return steps;
        }
        protected override bool CanShow() => false;
        #endregion
    }
}
#endif