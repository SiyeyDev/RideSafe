using Sirenix.OdinInspector;
using UnityEngine;

public class ChangeFloatShareData : MonoBehaviour
{
    [SerializeField] private FloatShareDataSO _sharedSOData; 
    [SerializeField, HideReferenceObjectPicker] private SharedData<float> _value; 
    [SerializeField] private bool _add;

    private void Start()
    {
        
    }

    public void SetValue() {
        if(_add)
        {
            _sharedSOData.Value += _value.GetValue();
            return;
        }
        _sharedSOData.Value = _value.GetValue();
    }
}
