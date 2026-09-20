#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class RenameGO : EditorWindow
{
    private static string newName;
    private static bool increaseNumber;
    private static int integerNumber = 1;
    private static int numberPosition;
    static List<GameObject> selectedObjects = new List<GameObject>();
    [MenuItem("GameObject/Utilities/Name/Rename")]
    public static void ShowWindow()
    {
        GetWindow(typeof(RenameGO));
    }
    private void OnGUI()
    {
        GUILayout.Label("Rename", EditorStyles.boldLabel);
        newName = EditorGUILayout.TextField("New name:", newName);
        if (string.IsNullOrEmpty(newName))
        {
            EditorGUILayout.LabelField("Please type new name");
            return;
        }
        increaseNumber = EditorGUILayout.Toggle("Count:", increaseNumber);
        if (increaseNumber)
        {
            integerNumber = EditorGUILayout.IntField("Amount of integers:", integerNumber);
            numberPosition = EditorGUILayout.IntSlider("Number position", numberPosition, 0, newName.Length);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Number position:");
            string start = newName.Substring(0, numberPosition);
            string end = newName.Substring(numberPosition);
            string numberText = "#";
            for (int i = 1; i < integerNumber; i++)
                numberText = $"{numberText}#";
            EditorGUILayout.LabelField($"{start}{numberText}{end}");
            EditorGUILayout.EndHorizontal();
        }
        if (!GUILayout.Button("Rename"))
            return;
        GameObject[] selections = Selection.gameObjects.OrderBy(obj => obj.transform.GetSiblingIndex()).ToArray();
        if (selections.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", $"Please select GameObjects", "OK");
            return;
        }
        Undo.RecordObjects(selections, "Rename objects");
        for (int i = 0; i < selections.Length; i++)
        {
            string name = newName;
            if (increaseNumber)
                name = $"{name}{(i + 1).ToString("D" + integerNumber)}";
            selections[i].name = name;
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
#endif
