#if UNITY_EDITOR
using StepCommand.Debugging;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace StepCommand.Editor
{
    public class EditorStepInitializedNode : EditorStepCommandNode
    {
        public EditorStepInitializedNode(MonoBehaviour targetComponent) : base(targetComponent)
        {
        }
        #region EditorStepCommandNode Methods
        protected override StepCommandWrapper[] GetCommands()
        {
            List<StepCommandWrapper> stepCommandWrappers = new List<StepCommandWrapper>();
            Type type = TargetComponent.GetType();
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (FieldInfo fieldInfo in fields)
            {
                if (fieldInfo == null)
                    continue;
                stepCommandWrappers.AddRange(GetCommands(fieldInfo));
            }
            return stepCommandWrappers.ToArray();
        }
        protected override void SetDebug()
        {
            IStepInitialize stepInitialize = TargetComponent as IStepInitialize;
            if (!StepCommandDebugData.DebugComponent.TryGetValue(stepInitialize.GetType(), out Type stepCommandDebugType))
                return;
            debug = new EditorStepCommandNodeDebug(stepInitialize as MonoBehaviour, stepCommandDebugType);
        }
        public override string GetName()
        {
            if (TargetComponent == null)
                return "Null";
            return $"{TargetComponent.gameObject.name}({CleanName((TargetComponent as IStepInitialize).GetType())})";
        }

        #endregion
        private List<StepCommandWrapper> GetCommands(FieldInfo fieldInfo)
        {
            object value = fieldInfo.GetValue(TargetComponent);
            if (value == null)
                return new List<StepCommandWrapper>();

            Type fieldType = fieldInfo.FieldType;
            if (fieldType == typeof(StepCommandWrapper))
                return new List<StepCommandWrapper> { (StepCommandWrapper)value };
            if (fieldType == typeof(StepCommandWrapper[]))
                return new List<StepCommandWrapper>((StepCommandWrapper[])value);
            if (IsListOfStepCommandWrapper(fieldType))
                return new List<StepCommandWrapper>((IEnumerable<StepCommandWrapper>)value);
            return new List<StepCommandWrapper>(0);
        }
        private bool IsListOfStepCommandWrapper(Type type)
        {
            return type.IsGenericType &&
                   type.GetGenericTypeDefinition() == typeof(List<>) &&
                   type.GetGenericArguments()[0] == typeof(StepCommandWrapper);
        }
    }
}
#endif