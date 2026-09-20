using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class SetTextByFloatSharedData : MonoBehaviour
{
    [SerializeField] private TextMeshPro _textMeshPro;
    [SerializeField] private FloatShareDataSO _floatData;
    [SerializeField] private bool _useMultiplier;
    [ShowIf("_useMultiplier")]
    [SerializeField] private SharedData<float> _multiplier;

    private void Update()
    {
        float value = _floatData.Value;
        if(_useMultiplier)
            value *= _multiplier.GetValue();
        _textMeshPro.SetText(value.ToString("#"));
    }
}
