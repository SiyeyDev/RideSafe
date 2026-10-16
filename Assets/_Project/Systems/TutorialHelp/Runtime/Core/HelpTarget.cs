using System;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.Help
{
    /// <summary>Something the learner can point at and select, as the tutorial sees it.</summary>
    public readonly struct HelpTarget : IEquatable<HelpTarget>
    {
        public HelpTarget(Transform transform, InteractionKind kind, Graphic graphic)
        {
            Transform = transform;
            Kind = kind;
            Graphic = graphic;
        }

        public Transform Transform { get; }
        public InteractionKind Kind { get; }
        /// <summary>The image that pulses (canvas controls only).</summary>
        public Graphic Graphic { get; }

        public Vector3 Position
        {
            get
            {
                if (Transform == null)
                    return Vector3.zero;
                if (Kind == InteractionKind.Object3D)
                {
                    Collider collider = Transform.GetComponentInChildren<Collider>(false);
                    if (collider != null)
                        return collider.transform.position; // bounds lag until physics syncs transforms
                }
                return Transform.position;
            }
        }

        /// <summary>Classifies a transform a module passed in: on a canvas it is a control, otherwise an object.</summary>
        public static HelpTarget From(Transform transform)
        {
            if (transform != null && transform.GetComponentInParent<Canvas>() != null)
            {
                Selectable selectable = transform.GetComponent<Selectable>();
                Graphic graphic = selectable != null && selectable.targetGraphic != null
                    ? selectable.targetGraphic
                    : transform.GetComponent<Graphic>();
                return new HelpTarget(transform, InteractionKind.Interface, graphic);
            }
            return new HelpTarget(transform, InteractionKind.Object3D, null);
        }

        public bool Equals(HelpTarget other) => Transform == other.Transform;
        public override bool Equals(object obj) => obj is HelpTarget other && Equals(other);
        public override int GetHashCode() => Transform != null ? Transform.GetHashCode() : 0;
    }
}
