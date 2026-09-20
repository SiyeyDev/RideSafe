#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class FindAndReplaceGO : EditorWindow
{
    private static string textToReplace;
    private static string newText;
    [MenuItem("GameObject/Utilities/Name/Find and replace")]
    public static void ShowWindow()
    {
        GetWindow(typeof(FindAndReplaceGO));
    }
    private void OnGUI()
    {
        GUILayout.Label("Find and replace", EditorStyles.boldLabel);

        textToReplace = EditorGUILayout.TextField("Text to replace:", textToReplace);
        newText = EditorGUILayout.TextField("New text:", newText);
        if (!GUILayout.Button("Find and replace"))
            return;
        GameObject[] selections = Selection.gameObjects;
        if (selections.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", $"Please select GameObjects", "OK");
            return;
        }
        bool finded = false;
        Undo.RecordObjects(selections, "Rename objects");
        foreach (GameObject go in selections)
        {
            if (!go.name.Contains(textToReplace))
                continue;
            go.name = go.name.Replace(textToReplace, newText);
            finded = true;
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        if (!finded)
            EditorUtility.DisplayDialog("Error", $"No GameObject found with {textToReplace}", "OK");
    }
}
#endif
