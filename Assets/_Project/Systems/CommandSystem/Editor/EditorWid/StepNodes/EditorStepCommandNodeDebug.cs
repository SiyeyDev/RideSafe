#if UNITY_EDITOR
using System;
using System.Reflection;
using Sirenix.OdinInspector;
using StepCommand.Debugging;
using UnityEngine;

namespace StepCommand.Editor
{
    [Serializable]
    public class EditorStepCommandNodeDebug
    {
        [ShowIf("$HasDebugAttacched")]
        [InlineEditor(InlineEditorModes.GUIOnly, Expanded = true), HideLabel()]
        [SerializeField] private StepCommandDebugBase _debugBase;
        private MonoBehaviour _target;
        private Type _stepcommandDebugType;
        public EditorStepCommandNodeDebug(MonoBehaviour target, Type stepcommandDebugType)
        {
            _target = target;
            _stepcommandDebugType = stepcommandDebugType;
            _debugBase = _target.GetComponent(stepcommandDebugType) as StepCommandDebugBase;
        }

        public void AddDebug()
        {
            try
            {
                if (_debugBase != null)
                    return;
                _debugBase = _target.gameObject.AddComponent(_stepcommandDebugType) as StepCommandDebugBase;
                FieldInfo field = StepCommandDebugUtilities.GetFieldInfo(_debugBase, "stepCommand");
                field?.SetValue(_debugBase, _target);
                StepCommandDebugUtilities.DebugLog($"Enable  debug for {_target}");
            }
            catch (Exception e)
            {
                StepCommandDebugUtilities.DebugError($"{e.Source}:{e.Message}");
            }

        }
        public void RemoveDebug()
        {
            try
            {
                if (_target.gameObject.GetComponent<StepCommandDebugBase>() == null)
                    return;
                StepCommandDebugUtilities.DebugLog($"Disable  debug for {_target}");
                StepCommandDebugBase[] debugs = _target.gameObject.GetComponents<StepCommandDebugBase>();
                foreach (StepCommandDebugBase debug in debugs)
                    UnityEngine.Object.DestroyImmediate(debug);
                _debugBase = null;
            }
            catch (Exception e)
            {
                StepCommandDebugUtilities.DebugError($"{e.Source}:{e.Message}");
            }
        }
        public bool HasDebugAttacched()
        {
            if (_target == null || _target.gameObject == null)
                return false;
            return _target.gameObject.GetComponent<StepCommandDebugBase>() != null;
        }

    }
}
#endif