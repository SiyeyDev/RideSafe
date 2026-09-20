using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Check
{
    public class FOVCheck : BaseCheck
    {
        [FoldoutGroup("Visibility  Settings", true)]
        [SerializeField] private Transform _source;
        [FoldoutGroup("Visibility  Settings", true)]
        [SerializeField] private Transform _target;
        [FoldoutGroup("Visibility  Settings", true)]
        [SerializeField] private float _angle;

        public Transform Source => _source;
        public Transform Target => _target;
        public float Angle => _angle;

        #region Check Methods
        protected override object GetValue1() => _source.position.Angle(_source.forward, _target.position);
        protected override object GetValue2() => _angle / 2;
        #endregion

        protected override void InteralDraw(bool wire) => DrawHook?.Invoke(this, wire);
        protected override bool Validate(ref string message) =>
            ValidateHook != null ? ValidateHook(this, ref message) : base.Validate(ref message);

        public static Action<FOVCheck, bool> DrawHook;
        public delegate bool ValidateDelegate(FOVCheck check, ref string message);
        public static ValidateDelegate ValidateHook;
    }
}
