using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ColorBrandData), true)]
public class ColorBrandDataEditor:  BaseBrandDataEditor<Color>
{
    #region BaseBrandDataEditor Reusable Methods
    protected override Color DrawGenericValue(Color value) => EditorGUILayout.ColorField(value);
    #endregion
}
