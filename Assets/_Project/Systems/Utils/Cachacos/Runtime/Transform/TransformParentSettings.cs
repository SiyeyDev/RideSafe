using UnityEngine;

public struct TransformParentSettings
{
    public Transform parentTransform;
    public Vector3 localPosition;
    public Quaternion localRotation;
    public Vector3 localScale;

    public TransformParentSettings(Transform selfTransform, bool defaultValues = false)
    {
        if (defaultValues)
        {
            parentTransform = selfTransform;
            localPosition = Vector3.zero;
            localRotation = Quaternion.identity;
            localScale = Vector3.one;
            return;
        }
        parentTransform = selfTransform.parent;
        localPosition = selfTransform.localPosition;
        localRotation = selfTransform.localRotation;
        localScale = selfTransform.localScale;
    }
    public void SetParent(Transform selfTransfrom, bool keepGlobal = false)
    {
        selfTransfrom.parent = parentTransform;
        if (keepGlobal)
            return;
        selfTransfrom.localPosition = localPosition;
        selfTransfrom.localRotation = localRotation;
        selfTransfrom.localScale = localScale;
    }
}
