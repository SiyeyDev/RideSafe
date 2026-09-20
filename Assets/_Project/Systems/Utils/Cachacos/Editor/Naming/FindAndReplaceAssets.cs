#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class FindAndReplaceAssets : EditorWindow
{
    private static string textToReplace;
    private static string newText;

    [MenuItem("Assets/Utilities/Find and Replace Name", false, 2000)]
    public static void ShowWindow()
    {
        GetWindow(typeof(FindAndReplaceAssets));
    }

    private void OnGUI()
    {
        GUILayout.Label("Find and Replace Asset Names", EditorStyles.boldLabel);

        textToReplace = EditorGUILayout.TextField("Text to Replace:", textToReplace);
        newText = EditorGUILayout.TextField("New Text:", newText);

        if (!GUILayout.Button("Find and Replace"))
            return;

        Object[] selectedAssets = Selection.objects;

        if (selectedAssets.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "Please select one or more assets.", "OK");
            return;
        }

        bool found = false;

        foreach (Object obj in selectedAssets)
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            if (!fileName.Contains(textToReplace)) continue;

            string newFileName = fileName.Replace(textToReplace, newText);
            string newAssetPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(assetPath), newFileName + System.IO.Path.GetExtension(assetPath));

            AssetDatabase.RenameAsset(assetPath, newFileName);
            found = true;
        }

        AssetDatabase.Refresh();

        if (!found)
            EditorUtility.DisplayDialog("Error", $"No asset names found containing '{textToReplace}'.", "OK");
        else
            EditorUtility.DisplayDialog("Success", "Assets renamed successfully!", "OK");
    }
}
#endif
