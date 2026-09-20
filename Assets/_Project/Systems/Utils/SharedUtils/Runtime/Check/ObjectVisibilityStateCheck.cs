using Check;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Check
{
    public class ObjectVisibilityStateCheck : BaseCheck
    {
        [FoldoutGroup("Visibility  Settings", true)]
        [SerializeField] private GameObject _source;
        [FoldoutGroup("Visibility  Settings", true)]
        [SerializeField] private bool _targetState;
        protected override object GetValue1() => _source.activeInHierarchy;
        protected override object GetValue2() => _targetState;

#if UNITY_EDITOR
        protected override bool Validate(ref string message)
        {
            if (_source == null)
            {
                Debug.LogWarning($"There is not _source variable assgined.");
                return false;
            }
            return true;
        }
#endif
    }
}
