using Sirenix.OdinInspector;
using UnityEngine;

namespace Check
{
    public class TriggerCheck : BaseCheck
    {
        [SerializeField] private ColliderEvents _colliderEvents;
        [SerializeField] private bool _shouldBeEnter;
        [SerializeField, ReadOnly] private bool _isEntered;
        public override void Initialize()
        {
            _colliderEvents.onTriggerEnter.AddListener(OnTriggerEnter);
            _colliderEvents.onTriggerExit.AddListener(OnTriggerExit);
        }

        public override void Shutdown()
        {
            _colliderEvents.onTriggerEnter.RemoveListener(OnTriggerEnter);
            _colliderEvents.onTriggerExit.RemoveListener(OnTriggerExit);
        }
        private void OnTriggerEnter(Collider collider) => _isEntered = true;
        private void OnTriggerExit(Collider collider) => _isEntered = false;
        protected override object GetValue1() => _isEntered;
        protected override object GetValue2() => _shouldBeEnter;

#if UNITY_EDITOR
        protected override bool Validate(ref string message)
        {
            if (_colliderEvents == null)
            {
                Debug.LogWarning($"There is not _colliderEvents  variable assgined.");
                return false;
            }
            return true;
        }
#endif
    }

}