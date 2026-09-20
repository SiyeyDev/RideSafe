using Comparation;
using UnityEngine;

namespace Check
{
    public abstract class BaseCheck
    {
        [Header("Check Settings")]
        [SerializeField] private BaseComparation _comparation;

        public virtual void Initialize() { }
        public virtual void Shutdown() { }

        public bool Evaluate()
        {
            if (_comparation == null)
                throw new System.ArgumentNullException($"There is not {typeof(BaseComparation)} assigned");
            return _comparation.IsValid(GetValue1(), GetValue2());
        }
        #region Check Methods
        protected abstract object GetValue1();
        protected abstract object GetValue2();
        #endregion
#if UNITY_EDITOR

        public void DrawGizmos(bool wire)
        {
            string message = string.Empty;
            if (!Validate(ref message))
            {
                Debug.LogWarning($"{message} Gizmos will not show");
                return;
            }
            InteralDraw(wire);
        }
        protected virtual void InteralDraw(bool wire) { }
        protected virtual bool Validate(ref string message) => true;

#endif
    }
}
