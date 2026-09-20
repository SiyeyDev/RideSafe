using Autohand;
using UnityEngine;

public class SetHands : MonoBehaviour
{
    [SerializeField] private OpenXRAutoHandTracking _leftTracking;
    [SerializeField] private OpenXRAutoHandTracking _rightTracking;
    [SerializeField] private HandMaterialProvider _materialProvider;
    [SerializeField] private Material _transparentMaterial;

    public void Set(bool active)
    {
        _rightTracking.enabled = active;
        _leftTracking.enabled = active;
        if (active)
            _materialProvider.RecoverLastMaterial();
        else
            _materialProvider.SetMaterial(_transparentMaterial);
    }

}
