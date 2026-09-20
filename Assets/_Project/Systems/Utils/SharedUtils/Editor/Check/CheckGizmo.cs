#if UNITY_EDITOR
using Check;
using Sirenix.OdinInspector;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class CheckGizmo : SerializedMonoBehaviour
{
    [ValidateInput("ValidateCheck", "GameObject target should have compoment with Check class.", InfoMessageType.Error)]
    [InlineEditor(InlineEditorModes.GUIOnly)]
    [SerializeField] private Component _target;
    [SerializeField] private Color _gizmoRightColor = Color.green;
    [SerializeField] private Color _gizmoWrongColor = Color.red;
    [SerializeField] private bool _wire = true;
    [SerializeField] private bool _showAlways = false;

    private bool ValidateCheck()
    {
        if (_target == null)
            return false;
        return GetBaseCheck() != null;
    }
    private BaseCheck GetBaseCheck()
    {
        if (_target == null)
            return null;

        FieldInfo[] fields = _target.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            if (!typeof(BaseCheck).IsAssignableFrom(field.FieldType))
                continue;

            return field.GetValue(_target) as BaseCheck;
        }
        return null;
    }

    private void OnDrawGizmos()
    {
        if (_showAlways)
            DrawGizmo();
    }
    private void OnDrawGizmosSelected()
    {
        if (!_showAlways)
            DrawGizmo();
    }
    private void DrawGizmo()
    {
        BaseCheck check = GetBaseCheck();
        if (check == null)
            return;
        bool result = check.Evaluate();
        Gizmos.color = result ? _gizmoRightColor : _gizmoWrongColor;
        Handles.color = result ? _gizmoRightColor : _gizmoWrongColor;
        check.DrawGizmos(_wire);
    }
}
#endif