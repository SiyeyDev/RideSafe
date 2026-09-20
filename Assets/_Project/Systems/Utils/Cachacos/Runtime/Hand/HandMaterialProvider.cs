using UnityEngine;

public class HandMaterialProvider : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer _rightHand;
    [SerializeField] private SkinnedMeshRenderer _leftHand;
    private Material _startRightMaterial;
    private Material _startLeftMaterial;
    private Material _lastRightMaterial;
    private Material _lastLeftMaterial;

    private void Awake()
    {
        _startRightMaterial = _rightHand.material;
        _startLeftMaterial = _leftHand.material;
    }
    public void SetMaterial(Material material)
    {
        _lastRightMaterial = SetHandMaterial(_rightHand, material);
        _lastLeftMaterial = SetHandMaterial(_leftHand, material);
    }
    public void SetRightMaterial(Material material) => _lastRightMaterial = SetHandMaterial(_rightHand, material);
    public void SetLeftMaterial(Material material) => _lastLeftMaterial = SetHandMaterial(_leftHand, material);

    public void RecoverMaterial()
    {
        RecoverHandMaterial(_rightHand, _startRightMaterial);
        RecoverHandMaterial(_leftHand, _startLeftMaterial);
    }
    public void RecoverRightMaterial() => RecoverHandMaterial(_rightHand, _startRightMaterial);
    public void RecoverLeftMaterial() => RecoverHandMaterial(_leftHand, _startLeftMaterial);
    public void RecoverLastMaterial()
    {
        _rightHand.material = _lastLeftMaterial;
        _leftHand.material = _lastRightMaterial;
    }
    public void RecoverLastRightMaterial() => _rightHand.material = _lastLeftMaterial;
    public void RecoverLastLeftMaterial() => _leftHand.material = _lastRightMaterial;

    private Material SetHandMaterial(SkinnedMeshRenderer skinnedMeshRenderer, Material materal)
    {
        Material lastMaterial = skinnedMeshRenderer.material;
        skinnedMeshRenderer.material = materal;
        return lastMaterial;
    }
    private void RecoverHandMaterial(SkinnedMeshRenderer skinnedMeshRenderer, Material materal)
    {
        if (skinnedMeshRenderer.material != materal)
            skinnedMeshRenderer.material = materal;
    }
}
