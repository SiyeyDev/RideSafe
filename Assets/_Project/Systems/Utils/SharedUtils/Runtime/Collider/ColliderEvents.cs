using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(10)]
public class ColliderEvents : MonoBehaviour
{
    [SerializeField] private LayerMask _layerMask;
    [FoldoutGroup("Collision")]
    public UnityEvent<Collision> onCollisionEnter;
    [FoldoutGroup("Collision")]
    public UnityEvent<Collision> onCollisionStay;
    [FoldoutGroup("Collision")]
    public UnityEvent<Collision> onCollisionExit;
    [FoldoutGroup("Trigger")]
    public UnityEvent<Collider> onTriggerEnter;
    [FoldoutGroup("Trigger")]
    public UnityEvent<Collider> onTriggerStay;
    [FoldoutGroup("Trigger")]
    public UnityEvent<Collider> onTriggerExit;

    #region Collision
    private void OnCollisionEnter(Collision collision)
    {
        if (!LayerUtilities.IsSameLayer(_layerMask, collision.gameObject.layer))
            return;
        onCollisionEnter?.Invoke(collision);
    }
    private void OnCollisionStay(Collision collision)
    {
        if (!LayerUtilities.IsSameLayer(_layerMask, collision.gameObject.layer))
            return;
        onCollisionStay?.Invoke(collision);
    }
    private void OnCollisionExit(Collision collision)
    {
        if (!LayerUtilities.IsSameLayer(_layerMask, collision.gameObject.layer))
            return;
        onCollisionExit?.Invoke(collision);
    }
    #endregion
    #region Trigger
    private void OnTriggerEnter(Collider other)
    {
        if (!LayerUtilities.IsSameLayer(_layerMask, other.gameObject.layer))
            return;
        onTriggerEnter?.Invoke(other);
    }
    private void OnTriggerStay(Collider other)
    {
        if (!LayerUtilities.IsSameLayer(_layerMask, other.gameObject.layer))
            return;
        onTriggerStay?.Invoke(other);
    }
    private void OnTriggerExit(Collider other)
    {
        if (!LayerUtilities.IsSameLayer(_layerMask, other.gameObject.layer))
            return;
        onTriggerExit?.Invoke(other);
    }
    #endregion
}
