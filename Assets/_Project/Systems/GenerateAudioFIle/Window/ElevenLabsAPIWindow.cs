#if UNITY_EDITOR
using Cachacos;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class ElevenLabsAPIWindow : BaseWindow
{
    public readonly string apiKey = "sk_6a403c9f83f8dd84c9c0610755576173ea7b2b5f4d65385c";
    public readonly string voicesURL = "https://api.elevenlabs.io/v1/voices";
    public readonly string generateURLStart = "https://api.elevenlabs.io/v1/text-to-speech/";
    public readonly string generateURLEnd = "/with-timestamps";
    public string GenerateURL
    {
        get
        {
            if (_voiceNames == null || _voiceNames.Count == 0)
                return null;
            return $"{generateURLStart}{_voices[_selectedVoiceIndex].voice_id}{generateURLEnd}";
        }
    }
    public VoiceSettings VoiceSettings { get; private set; }
    private List<Voice> _voices = new List<Voice>();
    private List<string> _voiceNames = new List<string>();
    private int _selectedVoiceIndex = 0;
    private StringWrapper _selectedVoiceName;
    public string CurrentVoce => _voices[_selectedVoiceIndex].name;
    private Button _voiceBtn;
    private VisualElement _voiceSettingsContainer;

    public ElevenLabsAPIWindow() : base()
    {
        EditorCoroutineUtility.StartCoroutine(FetchVoices(), this);
        VoiceSettings = new VoiceSettings();
    }
    #region BaseWindow Methods
    public override void Show()
    {
        if (_voiceNames.Count == 0)
        {
            _voiceSettingsContainer.style.display = DisplayStyle.None;
            return;
        }
        _voiceSettingsContainer.style.display = DisplayStyle.Flex;
        if(_voiceBtn.text == _selectedVoiceName.value) 
            return;
        _voiceBtn.text = _selectedVoiceName.value;
        _selectedVoiceIndex = _voiceNames.IndexOf(_selectedVoiceName.value);
    }
    public override void CreateGUI(EditorWindow window, VisualElement root, string buttonName)
    {
        base.CreateGUI(window, root, buttonName);
        _voiceBtn = AddButton("VoiceBtn", SelectVoice);
        _voiceBtn.text = "Fetching voices..";
        if (_voiceNames != null && _voiceNames.Count > 0)
            _voiceBtn.text = _voiceNames[0];
        Slider speed = AddSlider("Speed", Speed);
        speed.value = (float)VoiceSettings.speed;
        Slider stability = AddSlider("Stability", Stability);
        stability.value = (float)VoiceSettings.stability;
        Slider smililarity = AddSlider("Similarity", Similarity);
        smililarity.value = (float)VoiceSettings.similarity_boost;
        Slider style = AddSlider("Style", Style);
        style.value = (float)VoiceSettings.style;
        AddSlideToogle("SpeakBoostToggle", SpeakBoost);
        _voiceSettingsContainer = root.Q<VisualElement>("ElevenLabsVoiceSetting");
    }
    #endregion
    private IEnumerator FetchVoices()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(voicesURL))
        {
            request.SetRequestHeader("xi-api-key", apiKey);
            Debug.Log("SendRequest");
            yield return request.SendWebRequest();
            Debug.Log("Try get Voices");
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error fetching voices: {request.error}\n{request.downloadHandler.text}");
                yield break;
            }
            string jsonResponse = request.downloadHandler.text;
            try
            {
                VoicesResponse response = JsonConvert.DeserializeObject<VoicesResponse>(jsonResponse);
                _voices = response.voices;
                _voiceNames = new List<string>();
                foreach (Voice voice in _voices)
                    _voiceNames.Add(voice.name);
                _voiceBtn.text = _voiceNames[0];
                _selectedVoiceName = new StringWrapper(_voiceNames[0]);
                Debug.Log("Voices fetched successfully.");
            }
            catch (JsonException e)
            {
                Debug.LogError($"Error parsing voices JSON: {e.Message}\nRaw JSON: {jsonResponse}");
            }
        }
    }
    private void SelectVoice()
    {
        if (_voiceNames != null && _voiceNames.Count > 0)
            SelcectPopUp.Open<SelcectPopUp>(GetRect(_voiceBtn), _voiceNames.ToArray(), ref _selectedVoiceName, _voiceBtn.resolvedStyle.color);
    }
    private void Speed(ChangeEvent<float> evt) => VoiceSettings.speed = evt.newValue;
    private void Stability(ChangeEvent<float> evt) => VoiceSettings.stability = evt.newValue;
    private void Similarity(ChangeEvent<float> evt) => VoiceSettings.similarity_boost = evt.newValue;
    private void Style(ChangeEvent<float> evt) => VoiceSettings.style = evt.newValue;
    private void SpeakBoost(ChangeEvent<bool> evt) => VoiceSettings.use_speaker_boost = evt.newValue;
}
#endif
