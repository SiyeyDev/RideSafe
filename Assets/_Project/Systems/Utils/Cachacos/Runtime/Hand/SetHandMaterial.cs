using Sirenix.OdinInspector;
using UnityEngine;

public class SetHandMaterial     : MonoBehaviour
{
    [SerializeField] private Material _prefabMaterial;
    [SerializeField] private bool _changeColor;
    [ShowIf("_changeColor")][SerializeField] private Color _color;
    [ShowIf("_changeColor")][SerializeField] private string _materialColorName = "_BaseColor";

    private Material _material;
    private HandMaterialProvider _provider;

    private void OnDestroy()
    {
        Recover();
    }
    public void Set()
    {
        if (_material == null)
        {
            _material = Instantiate(_prefabMaterial);
            _material.SetColor(_materialColorName, _color);
        }
        if(_provider == null)
            _provider = FindObjectOfType<HandMaterialProvider>();
        _provider.SetMaterial(_material);
    }
    public void Recover()
    {
        if (_provider == null)
            _provider = FindObjectOfType<HandMaterialProvider>();
        _provider?.RecoverMaterial();
    }
}
