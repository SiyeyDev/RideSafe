#if UNITY_EDITOR
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using StepCommand.Debugging;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StepCommand.Editor
{
    public class EditorPathStepCommandNode : EditorStepCommandNode
    {
        [Space(10)]
        [FoldoutGroup("Paths")]
        [ReadOnly, TableList(MaxScrollViewHeight = 256, AlwaysExpanded = true, HideToolbar = true), HideLabel]
        [SerializeField] private PathNode[] _pathNodes;

        public EditorPathStepCommandNode(PathStepCommand targetComponent) : base(targetComponent)
        {
            StepCommandDebugUtilities.TryGetVariable(targetComponent, "_paths", out PathStepCommand.Path[] paths);
            _pathNodes = new PathNode[paths.Length];
            for (int i = 0; i < _pathNodes.Length; i++)
                _pathNodes[i] = new PathNode(paths[i].validationStepCommand, paths[i].pathStepCommand);
        }
        #region EditorStepCommandNode Methods

        protected override StepCommandWrapper[] GetCommands()
        {
            List<StepCommandWrapper> commands = new List<StepCommandWrapper>();
            foreach (PathNode pathNode in _pathNodes)
            {
                commands.Add(pathNode.validationCommand);
                commands.Add(pathNode.pathCommand);
            }
            return commands.ToArray();
        }
        protected override bool CanShow() => false;

        #endregion
        [Serializable]
        public class PathNode
        {
            [VerticalGroup("Validation"), HideLabel]
            [OdinSerialize, ReadOnly] public StepCommandWrapper validationCommand;

            [VerticalGroup("Path"), HideLabel]
            [OdinSerialize, ReadOnly] public StepCommandWrapper pathCommand;

            public PathNode(StepCommandWrapper validationCommand, StepCommandWrapper pathCommand)
            {
                this.validationCommand = validationCommand;
                this.pathCommand = pathCommand;
            }
        }
    }
}
#endif