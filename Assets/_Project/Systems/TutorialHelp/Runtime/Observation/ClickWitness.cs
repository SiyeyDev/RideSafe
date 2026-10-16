using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RideSafe.Help
{
    /// <summary>
    /// Added at runtime next to a target's own click handler, so it receives the same click
    /// (uGUI calls every IPointerClickHandler on the object) from AutoHand, the mouse or a
    /// test. Never saved, never shown, never changes what the module does with the click.
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public sealed class ClickWitness : MonoBehaviour, IPointerClickHandler
    {
        public InteractionKind Kind { get; private set; }
        public event Action<ClickWitness> Clicked;

        public static ClickWitness Attach(GameObject target, InteractionKind kind)
        {
            ClickWitness witness = target.GetComponent<ClickWitness>();
            if (witness == null)
            {
                witness = target.AddComponent<ClickWitness>();
                witness.hideFlags = HideFlags.DontSave | HideFlags.HideInInspector;
            }
            witness.Kind = kind;
            return witness;
        }

        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke(this);
    }
}
