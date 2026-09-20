using Sirenix.OdinInspector;
using UnityEngine;

namespace Check
{
    public class TransformDistanceCheck : BaseCheck
    {
        [FoldoutGroup("Transform  Settings", true)]
        [SerializeField] private Transform _source;
        [FoldoutGroup("Transform  Settings", true)]
        [SerializeField] private Transform _target;
        [FoldoutGroup("Transform  Settings", true)]
        [SerializeField] private float _distance;

        #region Check Methods
        protected override object GetValue1() => Vector3.Distance(_source.position, _target.position);
        protected override object GetValue2() => _distance;
        #endregion
#if UNITY_EDITOR
        protected override void InteralDraw(bool wire)
        {
            if (wire)
                Gizmos.DrawWireSphere(_target.position, _distance);
            else
                Gizmos.DrawSphere(_target.position, _distance);
        }
        protected override bool Validate(ref string message)
        {
            if (_source == null)
            {
                Debug.LogWarning($"There is not _source  variable assgined.");
                return false;
            }
            if (_target == null)
            {
                Debug.LogWarning($"There is not  _target variable assgined.");
                return false;
            }
            return true;
        }
#endif
    }
}
