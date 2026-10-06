using UnityEngine;

namespace RideSafe.Demo
{
    /// <summary>
    /// Shows the mouse pointer while the flat PC rig is active.
    /// <para>
    /// The project hides the cursor on every play, from CursorManager's
    /// RuntimeInitializeOnLoadMethod, which is right for VR and wrong for a demo driven with the
    /// mouse. This asks through the same counter rather than setting Cursor directly, so nothing
    /// else that locks the cursor is trampled.
    /// </para>
    /// </summary>
    public class DesktopCursor : MonoBehaviour
    {
        private void OnEnable() => CursorManager.UnlockCursor = true;

        private void OnDisable() => CursorManager.UnlockCursor = false;
    }
}
