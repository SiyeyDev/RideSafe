using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace StepCommand.Debugging
{
    public static class StepCommandDebugUtilities
    {
        #region Reflection Methods
        private static Dictionary<string, FieldInfo> _fieldCache = new();
        private static Dictionary<string, MethodInfo> _methodCache = new();
        private static string GetFieldCacheKey(object target, string fieldName) => $"{target.GetType().FullName}::{fieldName}";

        private static string GetMethodCacheKey(Type targetType, string methodName, string parameters) => $"{targetType.FullName}::{methodName}::{parameters}";

        public static FieldInfo GetFieldInfo(object target, string fieldName)
         => target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Public);


        public static bool TryGetVariable<TField>(object target, string fieldName, out TField variable)
        {
            variable = default;
            if (target == null)
                return false;
            string cacheKey = GetFieldCacheKey(target, fieldName);
            if (!_fieldCache.TryGetValue(cacheKey, out FieldInfo fieldInfo))
            {
                fieldInfo = GetFieldInfo(target, fieldName);
                if (fieldInfo == null)
                    return false;
                _fieldCache[fieldName] = fieldInfo;
            }
            variable = (TField)fieldInfo.GetValue(target);
            return true;
        }

        public static bool TryGetMethod(object target, string methodName, out MethodInfo methodInfo, params object[] parameters)
        {
            methodInfo = default;
            if (target == null)
                return false;
            Type[] paramTypes = parameters?.Select(p => p?.GetType() ?? typeof(object)).ToArray() ?? Type.EmptyTypes;
            string cacheKey = GetMethodCacheKey(target.GetType(), methodName, paramTypes.ToString());
            if (!_methodCache.TryGetValue(cacheKey, out MethodInfo cachedMethodInfo))
            {
                cachedMethodInfo = target.GetType().GetMethod(
                                                methodName,
                                                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Public | BindingFlags.Public,
                                                null,
                                                paramTypes,
                                                null
                                            );
                if (cachedMethodInfo == null)
                    return false;
                _methodCache[methodName] = cachedMethodInfo;
            }
            methodInfo = cachedMethodInfo;
            return true;
        }
        public static bool TryCallGetMethod<TReturn>(object target, string methodName, out TReturn value, params object[] parameters)
        {
            value = default;
            if (!TryGetMethod(target, methodName, out MethodInfo methodInfo))
                return false;
            object result = methodInfo?.Invoke(target, parameters);
            if (result is not TReturn casted)
                return false;
            value = casted;
            return true;
        }
        #endregion
        #region Console
        public static void DebugLog(string message)
        {
            Debug.Log($"{StepCommandDebugData.GetHeader()}{message} IN {Time.realtimeSinceStartup}");
        }
        public static void DebugWarning(string message) => Debug.LogWarning($"{StepCommandDebugData.GetHeader()}{message}IN {Time.realtimeSinceStartup}");
        public static void DebugError(string message) => Debug.LogError($"{StepCommandDebugData.GetHeader()}{message}IN {Time.realtimeSinceStartup}");
        public static string GetBoldColorMessage(string mesagge, Color color) => $"<color=#{ColorUtility.ToHtmlStringRGB(color)}><b>{mesagge}</b></color>";
        public static string GetBoldColorMessage(string mesagge, string htmlColor) => $"<color={htmlColor}><b>{mesagge}</b></color>";
        #endregion
    }
}
