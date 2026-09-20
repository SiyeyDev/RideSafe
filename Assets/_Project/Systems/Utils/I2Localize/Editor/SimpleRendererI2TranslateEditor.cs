
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SimpleRendererI2Translate), true)]
public class SimpleRendererI2TranslateEditor : SimpleI2LocalizeTranslateEditor<Renderer, Material>
{
}
