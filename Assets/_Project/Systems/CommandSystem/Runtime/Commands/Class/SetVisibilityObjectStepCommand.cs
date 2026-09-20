using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace StepCommand
{
    /// <remarks>
    /// The <see cref="SetVisibilityObjectStepCommandPropertyDrawer"/> class is used to draw the fields in the Inspector using a custom property drawer.
    /// Any changes to the serialized fields will not be automatically reflected in the Inspector unless the
    /// <see cref="SetVisibilityObjectStepCommandPropertyDrawer"/> is updated accordingly.
    /// Ensure any added or modified variables are properly handled in the <see cref="SetVisibilityObjectStepCommandPropertyDrawer"/>.
    /// </remarks>
    [Serializable]
    public class SetVisibilityObjectStepCommand : StepCommandClass
    {
        [Header("SetVisibilityObjectStep settings")]
        [SerializeField] private bool _multiplesObject;
        [HideIf("_multiplesObject")]
        [SerializeField] private GameObject _target;
        [ShowIf("_multiplesObject")]
        [SerializeField] private GameObject[] _targets;
        [SerializeField] private bool _show;
        [SerializeField] private bool _startHide;
        #region ICommand Methods
        public override IStepCommand Initialize()
        {
            if (!_multiplesObject && _target == null)
                throw new Exception($"There is not target attached");
            if (_multiplesObject)
            {
                foreach (GameObject target in _targets)
                {
                    if (target == null)
                        throw new Exception($"There is not target attached to list");
                }
            }
            if (_startHide)
                SetVisibility(false);
            return base.Initialize();
        }
        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            base.Execute(onComplete);
            SetVisibility(_show);
            CoroutineCaller.Instance.WaitForNextFrame(() => Complete(true, this));
            //Complete(true, this);
        }
        public override void InternalUndo()
        {
            SetVisibility(!_show);
            if (_startHide)
                SetVisibility(false);
            base.InternalUndo();
        }
        #endregion
        #region Main Methods
        private void SetVisibility(bool visibility)
        {
            if (!_multiplesObject)
            {
                if (visibility == false || _target.activeInHierarchy != visibility)
                    _target.SetActive(visibility);
                return;
            }
            foreach (GameObject target in _targets)
            {
                if (visibility == false || target.activeInHierarchy != visibility)
                    target.SetActive(visibility);
            }
        }
        #endregion
    }
}