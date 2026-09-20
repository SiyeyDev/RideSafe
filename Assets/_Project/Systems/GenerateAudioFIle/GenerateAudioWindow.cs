#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class GenerateAudioWIndow : EditorWindow
{
    private SerializedObject _serializedObject;


    private string _path;
    private I2LocalizeWindow _localizeWindow;
    private ElevenLabsAPIWindow _elevenLabsAPIWindow;
    private TestingWindow _testingWindow;
    private GenerateWindow _generateWindow;

    private static EditorWindow _window;

    [MenuItem("Tools/Cachacos/GenerateAudio", priority = 1)]
    public static void ShowWindow()
    {
        _window = GetWindow(typeof(GenerateAudioWIndow), false, "Generate Audio");
        _window.minSize = new Vector2(1024, 512);
        _window.position = new Rect(_window.position.position, _window.minSize);

    }
    private void OnGUI()
    {
        if (_window == null)
        {
            EditorWindow.GetWindow<GenerateAudioWIndow>().Close();
            return;
        }
        if (_serializedObject == null)
            _serializedObject = new SerializedObject(_window);
        _serializedObject.Update();
        _elevenLabsAPIWindow.Show();
        _localizeWindow.Show();
        _generateWindow.Show();
        _testingWindow.Show();
        _serializedObject.ApplyModifiedProperties();
    }
    private void CreateGUI()
    {
        if (_path == null)
            _path = $"{this.GetPathByFoldePath("", "", "", true).Replace(this.ToString(), "")}/GenerateAudio_VisualTree.uxml";
        if (_window == null)
            _window = this;
        VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(_path);
        VisualElement root = visualTree.Instantiate();
        rootVisualElement.Add(root);
        _elevenLabsAPIWindow = new ElevenLabsAPIWindow();
        _elevenLabsAPIWindow.CreateGUI(_window, rootVisualElement, "ElevenLabs");
        _elevenLabsAPIWindow.SetVisibility(false);
        _localizeWindow = new I2LocalizeWindow();
        _localizeWindow.CreateGUI(_window, rootVisualElement, "Localize");
        _localizeWindow.SetVisibility(false);
        _generateWindow = new GenerateWindow(_localizeWindow, _elevenLabsAPIWindow);
        _generateWindow.CreateGUI(_window, rootVisualElement, "Generate");
        _generateWindow.SetVisibility(false);
        _testingWindow = new TestingWindow();
        _testingWindow.CreateGUI(_window, rootVisualElement, "Test");
        _testingWindow.SetVisibility(false);
    }
}
#endif