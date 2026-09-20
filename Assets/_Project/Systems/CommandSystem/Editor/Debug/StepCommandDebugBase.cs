#if UNITY_EDITOR
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace StepCommand.Debugging
{
    public class StepCommandDebugBase : SerializedMonoBehaviour
    {
        [EnumToggleButtons]
        [SerializeField] public DebugType debugMessage = DebugType.Everything;

        #region Console
        protected void DebugLog(string message)
        {
            if ((debugMessage & DebugType.Log) != 0)
                StepCommandDebugUtilities.DebugLog(message);
        }
        protected void DebugWarning(string message)
        {
            if ((debugMessage & DebugType.Warning) != 0)
                StepCommandDebugUtilities.DebugWarning(message);
        }
        protected void DebugError(string message)
        {
            if ((debugMessage & DebugType.Error) != 0)
                StepCommandDebugUtilities.DebugError(message);
        }
        #endregion
        protected bool IsPlaying() => EditorApplication.isPlaying;
    }
}
#endif