#if UNITY_EDITOR
using Cachacos;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class GenerateWindow : BaseWindow
{
    private I2LocalizeWindow _localizeWindow;
    private ElevenLabsAPIWindow _elevenLabsAPIWindow;

    private Button _createBtn;
    private Label _pathLabel;
    private SlideToggle _audioToggle;
    private SlideToggle _timeStampToggle;
    public GenerateWindow(I2LocalizeWindow localizeWindow, ElevenLabsAPIWindow elevenLabsAPIWindow) : base()
    {
        _localizeWindow = localizeWindow;
        _elevenLabsAPIWindow = elevenLabsAPIWindow;
    }
    #region BaseWindow Methods
    public override void Show()
    {
        string displayText = GetFolderRouth();
        _createBtn.style.display = DisplayStyle.Flex;
        if (displayText == null)
        {
            displayText = "Please select a category";
            _createBtn.style.display = DisplayStyle.None;
        }
        if (_localizeWindow.SelectedTerms == null || _localizeWindow.SelectedTerms.Count == 0)
        {
            displayText = "Please select terms";
            _createBtn.style.display = DisplayStyle.None;
        }
        _pathLabel.text = displayText;
    }
    public override void CreateGUI(EditorWindow window, VisualElement root, string buttonName)
    {
        base.CreateGUI(window, root, buttonName);
        _audioToggle = AddSlideToogle("AudioToggle");
        _timeStampToggle = AddSlideToogle("TimeStampToggle");
        _pathLabel = root.Q<Label>("PathRouthLabel");
        _pathLabel.text = "";
        _createBtn = AddButton("CreateBtn", Create);

    }
    #endregion

    private string GetFolderRouth()
    {
        string basePath = FolderUtilities.GetPath(_localizeWindow.Category, "folder", parentPath: "Resources/Sounds/");
        if (basePath == null)
            return null;
        return $"{basePath}/{_localizeWindow.Language}";
    }
    private void Create() => EditorCoroutineUtility.StartCoroutine(StartGeneration(), this);
    private IEnumerator StartGeneration()
    {
        string url = _elevenLabsAPIWindow.GenerateURL;
        Debug.Log($"Star generate audio with {_elevenLabsAPIWindow.CurrentVoce} voice");
        if (url == null)
        {
            Debug.LogError($"Invalid Url");
            yield break;
        }
        GenerateAudio generateAudio = new GenerateAudio(GetFolderRouth(), _audioToggle.value, _timeStampToggle.value, _elevenLabsAPIWindow.VoiceSettings);
        foreach (KeyValuePair<string, string> kvp in _localizeWindow.GetTexts())
            yield return EditorCoroutineUtility.StartCoroutine(generateAudio.Generate(url, kvp.Value, kvp.Key), this);
        Notification.Open("Complete");
    }
}
#endif