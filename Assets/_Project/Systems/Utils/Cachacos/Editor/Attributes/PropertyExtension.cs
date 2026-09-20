#if UNITY_EDITOR
using UnityEditor;

public static class PropertyExtension
{
    public static string GetPath(this SerializedProperty property, bool includeObject = false)
    {
        string path = property.propertyPath;
        if (!includeObject)
            return path;
        if (property.serializedObject == null)
            return string.Empty;
        return $"{path}_ID{property.serializedObject.targetObject.GetEntityId()}";
    }
}
#endif