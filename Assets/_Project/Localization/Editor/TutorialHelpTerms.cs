using I2.Loc;
using RideSafe.Help;
using UnityEditor;

/// <summary>The tutorial's two short first-time texts, in es-CO and en (same pattern as Module01DemoSetup).</summary>
public static class TutorialHelpTerms
{
    [MenuItem("RideSafe/Tutorial Help/Añadir textos (I2)")]
    public static void Apply()
    {
        LanguageSourceAsset asset = AssetDatabase.LoadAssetAtPath<LanguageSourceAsset>("Assets/Resources/I2Languages.asset");
        LanguageSourceData data = asset.SourceData;
        int es = data.GetLanguageIndexFromCode("es-CO");
        int en = data.GetLanguageIndexFromCode("en");
        if (es < 0 || en < 0)
            throw new System.InvalidOperationException("I2Languages has no es-CO or en.");
        Set(data, es, en, HelpTextKeys.SelectInterface, "Apunta y presiona el gatillo", "Point and press the trigger");
        Set(data, es, en, HelpTextKeys.SelectObject, "Apunta al objeto y presiona el gatillo", "Point at the object and press the trigger");
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }

    private static void Set(LanguageSourceData data, int es, int en, string key, string spanish, string english)
    {
        TermData term = data.GetTermData(key) ?? data.AddTerm(key);
        term.Languages[es] = spanish;
        term.Languages[en] = english;
    }
}
