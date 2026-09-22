#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class CustomDebugWindow : EditorWindow
{
    private static string _messages;
    private static CustomDebugWindow _window;

    // Sin [MenuItem]: Unity solo acepta metodos sin parametros (o con MenuCommand) y logueaba
    // "Method CustomDebugWindow.Log has invalid parameters" en cada carga del editor.
    public static void Log(string message)
    {
        _messages = message;
        EnsureWindowIsOpen();
    }
    private static void EnsureWindowIsOpen()
    {
        if (!HasOpenInstances<CustomDebugWindow>())
            ShowWindow();
    }
    public static void ShowWindow() => _window = GetWindow<CustomDebugWindow>("Custom Debug");
    public static void CloseWindow() => _window.Close();
    private static void RepaintAllOpenWindows()
    {
        CustomDebugWindow[] windows = Resources.FindObjectsOfTypeAll<CustomDebugWindow>();
        foreach (CustomDebugWindow window in windows)
        {
            window.Repaint();
        }
    }

    private void OnGUI()
    {
        GUILayout.Label(_messages);
    }
}
#endif