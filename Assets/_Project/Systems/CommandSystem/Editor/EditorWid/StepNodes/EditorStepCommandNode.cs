#if UNITY_EDITOR
using Sirenix.OdinInspector;
using StepCommand.Debugging;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StepCommand.Editor
{
    [Serializable]
    public class EditorStepCommandNode
    {
        [field: FoldoutGroup("Target Compoment"), Space(10)]
        [field: InlineEditor(InlineEditorModes.GUIOnly, Expanded = true), ReadOnly, HideLabel]
        [field: SerializeField] public MonoBehaviour TargetComponent { get; private set; }

        [ShowIf("@HasDebug() && debug.HasDebugAttacched()")]
        [FoldoutGroup("Debug"), LabelText(" ")]
        public EditorStepCommandNodeDebug debug;

        [ShowIf("CanShow")]
        [FoldoutGroup("Steps Commands"), ListDrawerSettings(ShowFoldout = false), LabelText(" ")]
        [ReadOnly, SerializeField] private StepCommandWrapper[] _wrappers;
        public EditorStepCommandNode[] ChildrenNodes { get; private set; }

        public EditorStepCommandNode(MonoBehaviour targetComponent)
        {
            TargetComponent = targetComponent;
        }

        #region Main Methods
        public EditorStepCommandNode Initialize()
        {
            ChildrenNodes = GetChildrenNodes();
            SetDebug();
            return this;
        }
        protected virtual void SetDebug()
        {
            if (TargetComponent is not StepCommandMonoBehaviour stepCommandMonoBehaviour)
                return;
            if (!StepCommandDebugData.DebugComponent.TryGetValue(stepCommandMonoBehaviour.GetType(), out Type stepCommandDebugType))
                return;
            debug = new EditorStepCommandNodeDebug(stepCommandMonoBehaviour, stepCommandDebugType);
        }
        private EditorStepCommandNode[] GetChildrenNodes()
        {
            List<EditorStepCommandNode> nodes = new List<EditorStepCommandNode>();
            _wrappers = GetCommands();
            if (_wrappers == null)
                return null;
            foreach (StepCommandWrapper stepCommandWrapper in _wrappers)
            {
                if (stepCommandWrapper.StepCommand is MonoBehaviour stepCommand)
                {
                    EditorStepCommandNode node = GetEditorStepCommandNode(stepCommand);
                    if (node != null)
                        nodes.Add(node.Initialize());
                }
            }
            return nodes.ToArray();
        }
        private EditorStepCommandNode GetEditorStepCommandNode(MonoBehaviour stepCommand)
        {
            Type commandType = stepCommand.GetType();
            if (StepCommandDebugData.Nodes.TryGetValue(commandType, out var editorNodeType))
                return (EditorStepCommandNode)Activator.CreateInstance(editorNodeType, args: stepCommand);
            StepCommandDebugUtilities.DebugWarning($"No node registered for {commandType}");
            return new EditorStepCommandNode(stepCommand);
        }
        public bool HasDebug() => debug != null;
        #endregion
        #region EditorStepCommandNode Methods
        protected virtual StepCommandWrapper[] GetCommands()
        {
            StepCommandDebugUtilities.DebugWarning($"No commands were returned because this method was not properly overridden for {this.GetType()}.");
            return null;
        }

        protected virtual bool CanShow() => true;
        public virtual string GetName()
        {
            if (TargetComponent == null)
            {
                StepCommandDebugUtilities.DebugWarning($"TargetComponent is null for {this.GetType()}.");
                return null;
            }
            if (TargetComponent is not StepCommandMonoBehaviour stepCommandMonoBehaviour)
                return TargetComponent.gameObject.name;
            return $"{TargetComponent.gameObject.name}({CleanName(stepCommandMonoBehaviour.GetType())})";
        }
        protected string CleanName(Type type)
        {
            string typeName = type.ToString();
            string matchName = "StepCommand.";
            if (!typeName.Contains(matchName))
                return typeName;
            return typeName.Replace(matchName, "");
        }
        #endregion
    }
}
#endif

