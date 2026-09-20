#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;

public class EditorUtils : MonoBehaviour
{
    public static void ClearConsole()
    {
        var logEntries = Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
        var clearMethod = logEntries.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
        clearMethod.Invoke(null, null);
    }
    public static FieldInfo GetField(Type type, string name)
    {
        while (type != null)
        {
            FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
                return field;
            type = type.BaseType;
        }
        return null;
    }
    public static MethodInfo GetMethod(Type type, string name)
    {
        while (type != null)
        {
            var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (method != null)
                return method;

            type = type.BaseType;
        }

        return null;
    }
}
#endif