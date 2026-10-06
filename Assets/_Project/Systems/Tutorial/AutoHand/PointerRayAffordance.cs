using Autohand;
using Autohand.Demo;
using UnityEngine;

namespace RideSafe.Tutorial.AutoHand
{
    /// <summary>
    /// Visual-only pointing affordance for the very first interaction: a short beam along
    /// the forward direction of the HandCanvasPointer of the PrimarySelect hand.
    /// <para>
    /// AutoHand only draws its ray once the pointer is over a canvas, so a first-time user
    /// cannot see where they are aiming. This beam does no raycast and changes nothing in
    /// AutoHand: it copies the pointer's pose, and hides as soon as AutoHand's own ray takes
    /// over (the pointer has a target). It lives under the Interaction Gate, so it exists
    /// exactly as long as the gate does.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/Tutorial/Pointer Ray Affordance")]
    [RequireComponent(typeof(LineRenderer))]
    public class PointerRayAffordance : MonoBehaviour
    {
        [SerializeField] private AutoHandTutorialInputService _input;
        [SerializeField, Min(0.1f)] private float _length = 1.1f;

        private LineRenderer _line;
        private HandCanvasPointer _pointer;

        public bool IsShowing => _line != null && _line.enabled;

        protected virtual void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = 2;
            _line.enabled = false;
        }

        protected virtual void OnDisable() => _line.enabled = false;

        protected virtual void LateUpdate()
        {
            if (_pointer == null)
                _pointer = ResolvePointer();

            bool show = _pointer != null && _pointer.isActiveAndEnabled && _pointer.currTarget == null;
            _line.enabled = show;
            if (!show)
                return;

            Transform origin = _pointer.transform;
            _line.SetPosition(0, origin.position);
            _line.SetPosition(1, origin.position + origin.forward * _length);
        }

        /// <summary>The UI pointer on the hand that performs PrimarySelect, found through AutoHand's own links.</summary>
        private HandCanvasPointer ResolvePointer()
        {
            HandControllerLink link;
            CommonButton button;
            if (_input == null || !_input.TryGetPhysicalBinding(new ActionId(ActionIds.PrimarySelect), out link, out button) ||
                link.hand == null)
                return null;
            return link.hand.GetComponentInChildren<HandCanvasPointer>();
        }
    }
}
