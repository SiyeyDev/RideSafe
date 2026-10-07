using System.Linq;
using I2.Loc;
using RideSafe.Module01;
using RideSafe.Narration;
using RideSafe.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class Module01DemoSetup
{
    public static void Apply()
    {
        var asset=AssetDatabase.LoadAssetAtPath<LanguageSourceAsset>("Assets/Resources/I2Languages.asset");
        var data=asset.SourceData;
        int es=data.GetLanguageIndexFromCode("es-CO"), en=data.GetLanguageIndexFromCode("en");
        foreach(var line in System.IO.File.ReadAllLines("Assets/_Project/Module01/Data/PresentationTexts.txt"))
        { var p=line.Split('|'); if(p.Length!=3)continue;var term=data.AddTerm(p[0]);if(string.IsNullOrEmpty(term.Languages[es]))term.Languages[es]=p[1];if(string.IsNullOrEmpty(term.Languages[en]))term.Languages[en]=p[2]; }
        EditorUtility.SetDirty(asset);AssetDatabase.SaveAssets();
        var exp=Object.FindAnyObjectByType<Module01Experience>();
        var narration=Object.FindAnyObjectByType<NarrationPlayer>();
        var so=new SerializedObject(narration);var lines=so.FindProperty("_lines");lines.arraySize=1;lines.GetArrayElementAtIndex(0).stringValue="M01_Welcome";so.ApplyModifiedPropertiesWithoutUndo();
        while(narration.onStarted.GetPersistentEventCount()>0)UnityEventTools.RemovePersistentListener(narration.onStarted,0);
        while(narration.onFinished.GetPersistentEventCount()>0)UnityEventTools.RemovePersistentListener(narration.onFinished,0);
        while(exp.onOrientation.GetPersistentEventCount()>0)UnityEventTools.RemovePersistentListener(exp.onOrientation,0);
        while(exp.onLeaveOrientation.GetPersistentEventCount()>0)UnityEventTools.RemovePersistentListener(exp.onLeaveOrientation,0);
        UnityEventTools.AddPersistentListener(exp.onOrientation,narration.Play);
        UnityEventTools.AddPersistentListener(exp.onLeaveOrientation,narration.Stop);
        EditorUtility.SetDirty(exp);EditorUtility.SetDirty(narration);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
    }
}
