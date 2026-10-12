using System;
using System.Collections.Generic;
using UnityEngine;

namespace RideSafe.Tutorial.Presentation
{
    /// <summary>
    /// One controller model (left or right), its pressable parts and how to hold it up so a
    /// given control is clearly visible. The mapping control -> part and the view poses live
    /// on the prefab, so another controller model only needs its own prefab.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("RideSafe/Tutorial/Controller Model Visual")]
    public class ControllerModelVisual : MonoBehaviour
    {
        /// <summary>
        /// Local pose of the model inside the hint view while a control is explained. The view
        /// faces the learner along its +Z, so a pose turns the explained control toward +Z.
        /// </summary>
        [Serializable]
        public struct ControlPose
        {
            public ControllerControl Control;
            [Tooltip("Local rotation of the model, in degrees.")]
            public Vector3 Euler;
            [Tooltip("Local offset of the model, in the view's units.")]
            public Vector3 Offset;

            public Pose ToPose() => new Pose(Offset, Quaternion.Euler(Euler));
        }

        [SerializeField] private XRNodeRole _hand = XRNodeRole.RightHand;

        [Tooltip("Pressable parts. Empty collects every ControllerButtonVisual under this model.")]
        [SerializeField] private List<ControllerButtonVisual> _buttons = new List<ControllerButtonVisual>();

        [Header("View poses")]
        [Tooltip("Used for any control without its own pose.")]
        [SerializeField] private ControlPose _defaultPose;
        [Tooltip("e.g. Trigger view, Grip view, face buttons view, thumbstick view.")]
        [SerializeField] private List<ControlPose> _poses = new List<ControlPose>();

        public XRNodeRole Hand => _hand;

        public bool TryGetButton(ControllerControl control, out ControllerButtonVisual button)
        {
            if (_buttons.Count == 0)
                GetComponentsInChildren(true, _buttons);

            for (int i = 0; i < _buttons.Count; i++)
            {
                if (_buttons[i] != null && _buttons[i].Control == control)
                {
                    button = _buttons[i];
                    return true;
                }
            }
            button = null;
            return false;
        }

        /// <summary>The pose that shows <paramref name="control"/>, or the default pose.</summary>
        public Pose PoseFor(ControllerControl control)
        {
            for (int i = 0; i < _poses.Count; i++)
            {
                if (_poses[i].Control == control)
                    return _poses[i].ToPose();
            }
            return _defaultPose.ToPose();
        }
    }
}
