using UnityEditor;

[CustomEditor(typeof(StringBrandData), true)]
public class StringBrandDataEditor : BaseBrandDataEditor<string>
{
    #region BaseBrandDataEditor Reusable Methods
    protected override string DrawGenericValue(string value) => EditorGUILayout.TextField(value);
    #endregion
}
