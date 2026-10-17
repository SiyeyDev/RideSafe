using UnityEngine;

namespace RideSafe.UI
{
    /// <summary>
    /// Keeps this panel just below another one, centred under it with a small gap, whatever
    /// canvas each lives on: used for the module 1 final buttons, which sat at a fixed height and
    /// ended up over the last rows of a long «Mi Elección» list. Works in world space; does
    /// nothing while the target is hidden.
    /// </summary>
    [AddComponentMenu("RideSafe/UI/Follow Below")]
    [DefaultExecutionOrder(1100)] // after Enclose (1000): it may follow a panel's box
    public class FollowBelow : MonoBehaviour
    {
        [SerializeField] private RectTransform _target;
        [Tooltip("Gap under the target, in the target canvas's pixels.")]
        [SerializeField, Min(0f)] private float _gap = 12f;

        private readonly Vector3[] _corners = new Vector3[4];

        public void Configure(RectTransform target, float gap)
        {
            _target = target;
            _gap = gap;
        }

        private void LateUpdate() => Tick();

        public void Tick()
        {
            if (_target == null || !_target.gameObject.activeInHierarchy)
                return;
            RectTransform self = (RectTransform)transform;

            _target.GetWorldCorners(_corners); // 0 bottom-left, 1 top-left, 2 top-right, 3 bottom-right
            Vector3 targetBottom = (_corners[0] + _corners[3]) / 2f;
            Vector3 down = -_target.up;
            Vector3 wanted = targetBottom + down * (_gap * _target.lossyScale.y);

            self.GetWorldCorners(_corners);
            Vector3 selfTop = (_corners[1] + _corners[2]) / 2f;
            Vector3 move = wanted - selfTop;
            // Writing the position dirties the canvas and rebuilds it; skip when nothing moved.
            if (move.sqrMagnitude > 1e-10f)
                self.position += move;
        }
    }
}
