#if UNITY_EDITOR
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace StepCommand.Debugging
{
    public abstract class StepCommandDebug<T> : StepCommandDebugBase where T : SerializedMonoBehaviour
    {
        [SerializeField] protected T stepCommand;
       
        #region Reflection Methods

        protected TField GetVariable<TField>(string methodName) => GetVariable<TField>(stepCommand, methodName);

        protected TField GetVariable<TField>(object target, string fieldName)
        {
            if (!StepCommandDebugUtilities.TryGetVariable(target, fieldName, out TField result))
                DebugWarning($"Field '{fieldName}' not found in {target.GetType().Name}");
            return result;
        }

        protected void CallMethod(string methodName, params object[] parameters) => CallMethod(stepCommand, methodName, parameters);
        protected void CallMethod(object target, string methodName, params object[] parameters)
        {
            if (!StepCommandDebugUtilities.TryGetMethod(target, methodName, out MethodInfo methodInfo, parameters))
            {
                DebugWarning($"Method '{methodName}' does not was found for {target}");
                return;
            }
            Debug.Log($"Method '{methodInfo}");
            methodInfo.Invoke(target, parameters);
        }
        protected TReturn CallGetMethod<TReturn>(string methodName, params object[] parameters) => CallGetMethod<TReturn>(stepCommand, methodName, parameters);
        protected TReturn CallGetMethod<TReturn>(object target, string methodName, params object[] parameters)
        {
            if (!StepCommandDebugUtilities.TryCallGetMethod(target, methodName, out TReturn result, parameters))
                DebugWarning($"Method '{methodName}' did not return expected type {typeof(TReturn).Name}");
            return result;
        }
        #endregion
        protected bool IsActiveStep() => stepCommand?.enabled ?? false;
        protected string GetName(StepCommandWrapper stepCommandWrapper)
        {
            if (stepCommandWrapper == null)
                return "null";
            return CallGetMethod<string>(stepCommandWrapper, StepCommandDebugData.getGuideName);
        }
        protected string GetNames(IList<StepCommandWrapper> stepcommandWrappers)
        {
            string names = string.Empty;
            if (stepcommandWrappers == null)
                return "There is not commands";
            foreach (StepCommandWrapper stepCommandWrapper in stepcommandWrappers)
            {
                if (names != string.Empty)
                    names += $"\n";
                if (stepCommandWrapper == null)
                {
                    names += $"null";
                    continue;
                }
                names += $"{GetName(stepCommandWrapper)}";
            }
            if (String.IsNullOrEmpty(names))
                names = "There is not commands";
            return names;
        }
    }
}
#endif