#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class TestingWindow : BaseWindow
{
    private AudioClip _audioClip;
    private TextAsset _jsonTextAsset;
    private ObjectField _timeStampObjectField;
    private Label _warningLabel;
    private Button _testBtn;

    #region BaseWindow Methods
    public override void Show()
    {
        _testBtn.text = !TypingTextDisplayWindow.Opened ? "Start" : "Stop";
        if (_jsonTextAsset == null)
        {
            SetButtonVisibility(false);
            return;
        }
        SetButtonVisibility(true);
    }
    public override void CreateGUI(EditorWindow window, VisualElement root, string buttonName)
    {
        base.CreateGUI(window, root, buttonName);
        AddObjectField("AudioClip", AddClip);
        _timeStampObjectField = AddObjectField("TimeStamp");
        _timeStampObjectField.style.display = DisplayStyle.None;
        _timeStampObjectField.SetEnabled(false);
        _warningLabel = root.Q<Label>("WarningLabel");
        _testBtn = AddButton("StartTestBtn", StartTest);
        _testBtn.style.display = DisplayStyle.None;
        _testBtn.text = "Start";
    }
    #endregion
    private void SetButtonVisibility(bool visiblity)
    {
        _timeStampObjectField.style.display = visiblity ? DisplayStyle.Flex : DisplayStyle.None;
        _testBtn.style.display = visiblity ? DisplayStyle.Flex : DisplayStyle.None;
        _warningLabel.style.display = visiblity ? DisplayStyle.None : DisplayStyle.Flex;
    }
    private void AddClip(ChangeEvent<Object> evt)
    {
        _audioClip = evt.newValue as AudioClip;
        string jsonFilePath = AssetDatabase.GetAssetPath(_audioClip).Replace(Constants.k_audioFileExtension, Constants.k_jsonExtension);
        _jsonTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonFilePath);
        _timeStampObjectField.value = _jsonTextAsset;
    }

    private void StartTest()
    {
        if (!TypingTextDisplayWindow.Opened)
        {
            Rect rect = GetRect(_testBtn);
            rect.y += rect.height / 2;
            TypingTextDisplayWindow.OpenTyping(_audioClip, _jsonTextAsset.text, rect);
            return;
        }
        TypingTextDisplayWindow.CloseTyping();
    }

}
#endif