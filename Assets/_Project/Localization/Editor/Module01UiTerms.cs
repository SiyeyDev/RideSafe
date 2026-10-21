using I2.Loc;
using UnityEditor;

/// <summary>
/// Texts the 2026-10-16 UI review found untranslated, in es-CO and en (same pattern as
/// TutorialHelpTerms). The orientation tag read «MODULE 1 · READINESS» in a Spanish session.
/// They still have to reach the official Google Sheet.
/// </summary>
public static class Module01UiTerms
{
    [MenuItem("RideSafe/Módulo 1/Añadir textos de la revisión de UI (I2)")]
    public static void Apply()
    {
        LanguageSourceAsset asset = AssetDatabase.LoadAssetAtPath<LanguageSourceAsset>("Assets/Resources/I2Languages.asset");
        LanguageSourceData data = asset.SourceData;
        int es = data.GetLanguageIndexFromCode("es-CO");
        int en = data.GetLanguageIndexFromCode("en");
        if (es < 0 || en < 0)
            throw new System.InvalidOperationException("I2Languages has no es-CO or en.");
        Set(data, es, en, "M1/Orientation_Tag", "Módulo 1 · Preparación", "Module 1 · Readiness");
        Set(data, es, en, "M1/MyChoice_Empty", "No elegiste ningún elemento en esta sección.", "You didn't choose any item in this section.");
        Set(data, es, en, "M1/Decision_Hint", "Elige la opción que mejor describe el vehículo.", "Choose the option that best describes the vehicle.");
        // The video's «Reproduciendo» tag was dropped (PO, 2026-10-16): its term goes too.
        data.RemoveTerm("M1/Video_Playing");
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
