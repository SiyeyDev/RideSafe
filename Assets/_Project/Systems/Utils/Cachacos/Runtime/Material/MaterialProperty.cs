using System;
using UnityEngine;
using UnityEngine.Rendering;
/// <note>
/// The <see cref="MaterialPropertyPropetyDrawer"/> class is used to draw the `MaterialProperty` fields in the Inspector using a custom property drawer.
/// Any changes to the serialized fields will not be automatically reflected in the Inspector unless the 
/// <see cref="MaterialPropertyPropetyDrawer"/> is updated accordingly. If you add or modify any variables in `MaterialProperty`,
/// ensure the <see cref="MaterialPropertyPropetyDrawer"/> logic is also updated to properly display and handle them in the Inspector.
/// </note>
[Serializable]
public class MaterialProperty
{
    [SerializeField] private Renderer _renderer;
    [SerializeField] private bool _instantiateMaterial = true;
    [SerializeField] private string _materialPropertyName;
    [SerializeField] private string _materialPropertyStringType;
    public ShaderPropertyType propiertyType { get => (ShaderPropertyType)Enum.Parse(typeof(ShaderPropertyType), _materialPropertyStringType); }
    public bool setNewValue;
    [SerializeField] private int _startIntValue;
    [SerializeField] private int _newIntValue;
    [SerializeField] private float _startFloatValue;
    [SerializeField] private float _newFloatValue;
    [SerializeField] [ColorUsage(true, true)] private Color _startColorValue;
    [SerializeField] [ColorUsage(true, true)] private Color _newColorValue;
    [SerializeField] private Vector3 _startVectorValue;
    [SerializeField] private Vector3 _newVectorValue;
    [SerializeField] private Texture _startTextureValue;
    [SerializeField] private Texture _newTextureValue;
    private object startValue;

    public bool PropertyChanged;
    public void Initialize()
    {
        if (!_instantiateMaterial)
            return;
        string lastName = $"{_renderer.sharedMaterial}(instantiated)";
        _renderer.sharedMaterial = GameObject.Instantiate(_renderer.sharedMaterial);
        _renderer.sharedMaterial.name = lastName;
        //SetValue();
    }
    public void ResetValue() => UpdateValue(true);

    public void SetValue() => UpdateValue(false);

    private void UpdateValue(bool reset)
    {
        if (_renderer == null)
            return;
        if (_renderer.sharedMaterial == null)
            return;
        switch (propiertyType)
        {
            case ShaderPropertyType.Int:
                _renderer.sharedMaterial.SetInt(_materialPropertyName, reset ? _startIntValue : _newIntValue);
                break;
            case ShaderPropertyType.Float:
                _renderer.sharedMaterial.SetFloat(_materialPropertyName, reset ? _startFloatValue : _newFloatValue);
                break;
            case ShaderPropertyType.Vector:
                _renderer.sharedMaterial.SetVector(_materialPropertyName, reset ? _startVectorValue : _newVectorValue);
                break;
            case ShaderPropertyType.Color:
                _renderer.sharedMaterial.SetColor(_materialPropertyName, reset ? _startColorValue : _newColorValue);
                break;
            case ShaderPropertyType.Texture:
                _renderer.sharedMaterial.SetTexture(_materialPropertyName, reset ? _startTextureValue : _newTextureValue);
                break;
        }
        if (!reset)
            PropertyChanged = true;
    }
    public void StartSmoothValue()
    {
        startValue = GetValue();
    }
    public void SetSmoothValue(float time)
    {
        switch (propiertyType)
        {
            case ShaderPropertyType.Int:
                int intValue = (int)Mathf.Lerp((int)startValue, _newIntValue, time);
                _renderer.sharedMaterial.SetInt(_materialPropertyName, intValue);
                break;
            case ShaderPropertyType.Float:
                float floatValue = Mathf.Lerp((float)startValue, _newFloatValue, time);
                _renderer.sharedMaterial.SetFloat(_materialPropertyName, floatValue);
                break;
            case ShaderPropertyType.Vector:
                Vector4 vectorValue = Vector4.Lerp((Vector4)startValue, _newVectorValue, time);
                _renderer.sharedMaterial.SetVector(_materialPropertyName, vectorValue);
                break;
            case ShaderPropertyType.Color:
                Color colorValue = Color.Lerp((Color)startValue, _newColorValue, time);
                _renderer.sharedMaterial.SetColor(_materialPropertyName, colorValue);
                break;
            case ShaderPropertyType.Texture:
                _renderer.sharedMaterial.SetTexture(_materialPropertyName, _newTextureValue);
                break;
        }
    }
    public object GetValue()
    {
        switch (propiertyType)
        {
            case ShaderPropertyType.Int:
                return _renderer.sharedMaterial.GetInt(_materialPropertyName);
            case ShaderPropertyType.Float:
                return _renderer.sharedMaterial.GetFloat(_materialPropertyName);
            case ShaderPropertyType.Vector:
                return _renderer.sharedMaterial.GetVector(_materialPropertyName);
            case ShaderPropertyType.Color:
                return _renderer.sharedMaterial.GetColor(_materialPropertyName);
            case ShaderPropertyType.Texture:
                return _renderer.sharedMaterial.GetTexture(_materialPropertyName);
        }
        return null;
    }
}
