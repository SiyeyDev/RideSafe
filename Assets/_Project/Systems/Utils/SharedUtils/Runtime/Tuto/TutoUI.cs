using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

public class TutoUI : MonoBehaviour
{
    [SerializeField] private TutoGuideData _guideData;
    [SerializeField] private TextMeshProUGUI _textMeshPro;
    [SerializeField] private VideoPlayer _videoPlayer;

    private void OnEnable()
    {
        SetData();
        _videoPlayer.Play();
    }
    [Button("UpdateData")]
    private void SetData()
    {
        _textMeshPro.SetText(_guideData.GetText());
        _videoPlayer.clip = _guideData.GetClip();
    }

    public void UpdateGuideData(TutoGuideData guideData)
    {
        _guideData = guideData;
        SetData();
    }
    public TutoGuideData GetCurrentData() => _guideData;
}
