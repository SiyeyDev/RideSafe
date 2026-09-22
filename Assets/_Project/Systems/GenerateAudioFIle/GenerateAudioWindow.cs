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
        // _window es static: tras un domain reload (recompilar / entrar a Play) vuelve a null.
        // Nunca llamar GetWindow() aqui: crearia una ventana nueva dentro del OnGUI.
        if (_window == null)
            _window = this;
        if (_elevenLabsAPIWindow == null || _localizeWindow == null || _generateWindow == null || _testingWindow == null)
            return;
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
        if (visualTree == null)
        {
            Debug.LogError($"GenerateAudio: no se encontro el UXML en '{_path}'.");
            return;
        }
        VisualElement root = visualTree.Instantiate();
        // El TemplateContainer que devuelve Instantiate() no estira por defecto, y el UXML usa
        // alturas en % (65/35). Sin esto la altura del padre queda indefinida, los % no resuelven
        // y toda la herramienta se aplasta contra el borde superior de la ventana.
        root.style.flexGrow = 1f;
        root.style.flexShrink = 1f;
        root.style.height = new StyleLength(Length.Percent(100));
        AttachStyleSheets(root);
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

    /// <summary>
    /// Las etiquetas &lt;Style src="..."&gt; del UXML guardan la referencia al StyleSheet resuelta en
    /// tiempo de import. Al traer la tool desde otro proyecto esas referencias quedan rotas aunque
    /// el GUID del disco sea correcto, y la hoja simplemente no se aplica. Cargarlas por GUID aqui
    /// hace que la tool no dependa de eso. Si el UXML ya las trae, se aplican las mismas reglas.
    /// </summary>
    private static void AttachStyleSheets(VisualElement root)
    {
        // Mismo orden que el UXML: la hoja general primero, la del SlideToggle despues.
        AttachStyleSheet(root, "09c87fe9d088fa243ae779c041ea59ab", "GenerateAudio_StyleSheet.uss");
        AttachStyleSheet(root, "c8c2573c2f87673489ab2bdf6fee8779", "SlideToggle.uss");
    }

    private static void AttachStyleSheet(VisualElement root, string guid, string label)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        StyleSheet sheet = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
        if (sheet == null)
        {
            Debug.LogWarning($"GenerateAudio: no se pudo cargar '{label}' (guid {guid}).");
            return;
        }
        if (!root.styleSheets.Contains(sheet))
            root.styleSheets.Add(sheet);
    }
}
#endif