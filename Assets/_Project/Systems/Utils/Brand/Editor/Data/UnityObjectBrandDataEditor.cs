using UnityEditor;
using UnityEngine;

public class UnityObjectBrandDataEditor<T> : BaseBrandDataEditor<T> where T : Object
{
    #region BaseBrandDataEditor Reusable Methods
    protected override T DrawGenericValue(T value) => (T) EditorGUILayout.ObjectField(value, typeof(T), false);
    #endregion
}
