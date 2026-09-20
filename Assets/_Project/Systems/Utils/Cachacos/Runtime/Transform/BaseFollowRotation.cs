using Sirenix.OdinInspector;
using UnityEngine;

namespace Cachacos
{
    public abstract class BaseFollowRotation : MonoBehaviour
    {
        [SerializeField] private Transform _follower;
        [SerializeField] private Transform _target;
        [Space(2)]
        [FoldoutGroup("Axis")]
        [SerializeField] private bool _followX;
        [FoldoutGroup("Axis")]
        [SerializeField] private bool _followY;
        [FoldoutGroup("Axis")]
        [SerializeField] private bool _followZ;
        [Space(2)]
        [SerializeField] private bool _keepOffset;
        [ShowIf("_keepOffset")][SerializeField] private bool _recalculateOffset;

        private Vector3 _offset;

        private void Awake()
        {
            _offset = _follower.eulerAngles - _target.eulerAngles;

        }
        private void OnEnable()
        {
            if (_recalculateOffset)
                _offset = _follower.position - _target.position;
        }
        protected void Rotate()
        {
            Vector3 targetPosition = GetTargetRotation();
            _follower.eulerAngles = targetPosition;
        }
        private Vector3 GetTargetRotation()
        {
            Vector3 targetRotation = _target.eulerAngles;
            if (_keepOffset)
                targetRotation += _offset;
            if (!_followX)
                targetRotation.x = _follower.eulerAngles.x;
            if (!_followY)
                targetRotation.y = _follower.eulerAngles.y;
            if (!_followZ)
                targetRotation.z = _follower.eulerAngles.z;
            return targetRotation;
        }
    }
}
