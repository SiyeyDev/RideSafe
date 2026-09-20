using TMPro;
using UnityEngine;

public class Guide : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;
    public void Set(Vector3 position, float value)
    {
        _text.text = value.ToString();
        transform.localPosition = position;
    }
}
