using UnityEditor;
using UnityEngine;

namespace Check
{
    [InitializeOnLoad]
    internal static class FOVCheckEditorInstaller
    {
        static FOVCheckEditorInstaller()
        {
            FOVCheck.DrawHook = Draw;
            FOVCheck.ValidateHook = Validate;
        }

        private static void Draw(FOVCheck check, bool wire)
        {
            float radius = Mathf.Sin(check.Angle * 0.5f * Mathf.Deg2Rad) * 2;
            bool lookingTarget = check.Source.position.IsInFOV(check.Source.forward, check.Target.position, check.Angle);
            Handles.color = lookingTarget ? Color.green : Color.red;
            Gizmos.color = lookingTarget ? Color.green : Color.red;
            Vector3 CameraDistance = Vector3.forward * Vector3.Distance(check.Source.position, check.Source.position + (check.Source.forward * 2f));
            Vector3 CameraDirection = check.Source.forward;
            Matrix4x4 oldCameraHandleMatrix = Handles.matrix;
            Matrix4x4 oldCameraGimosMatrix = Handles.matrix;
            Handles.matrix = Matrix4x4.TRS(check.Source.position, Quaternion.LookRotation(CameraDirection), Vector3.one);
            Gizmos.matrix = Matrix4x4.TRS(check.Source.position, Quaternion.LookRotation(CameraDirection), Vector3.one);
            Handles.DrawWireDisc(CameraDistance, Vector3.forward, radius);
            Gizmos.DrawLine(Vector3.zero, CameraDistance + check.Target.up * radius);
            Gizmos.DrawLine(Vector3.zero, CameraDistance - check.Target.up * radius);
            Gizmos.DrawLine(Vector3.zero, CameraDistance + check.Target.forward * radius);
            Gizmos.DrawLine(Vector3.zero, CameraDistance - check.Target.forward * radius);
            Handles.matrix = oldCameraHandleMatrix;
            Gizmos.matrix = oldCameraGimosMatrix;
            Gizmos.DrawLine(check.Source.position, check.Target.position);
        }

        private static bool Validate(FOVCheck check, ref string message)
        {
            if (check.Source == null)
            {
                Debug.LogWarning($"There is not _source  variable assgined.");
                return false;
            }
            if (check.Target == null)
            {
                Debug.LogWarning($"There is not _target  variable assgined.");
                return false;
            }
            return true;
        }
    }
}
