using I2.Loc;
using UnityEditor;

/// <summary>
/// Texts the 2026-10-16 UI review found untranslated, in es-CO and en (same pattern as
/// TutorialHelpTerms). The orientation tag read «MODULE 1 · READINESS» in a Spanish session.
/// They still have to reach the official Google Sheet.
/// <para>
/// 2026-10-26: the sheet import in <c>febeb75</c> dropped the report's terms, and the three
/// tables never had one, so the report and the station signs read «M1/...». The dropped texts
/// come back as they were before that commit; I2TermsTests keeps them there.
/// </para>
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

        // The three tables of the client's sheet, named as its «Mesa / grupo» column.
        Set(data, es, en, "M1/Zone_Protection", "Protección personal", "Personal protection");
        Set(data, es, en, "M1/Zone_Control", "Visibilidad y control", "Visibility and control");
        Set(data, es, en, "M1/Zone_Readiness", "Preparación del trayecto", "Trip readiness");
        Set(data, es, en, "M1/Remove_Question", "¿Quieres quitar este elemento de tu preparación?", "Remove this item from your preparation?");

        // «Mi Elección» and the final report.
        Set(data, es, en, "M1/MyChoice_Title", "Mi Elección", "My choice");
        Set(data, es, en, "M1/MyChoice_Note", "Los elementos opcionales no penalizan: solo cuentan el equipo de núcleo y lo que no corresponde llevar.", "Optional items carry no penalty: only core gear and unsuitable choices count.");
        // The three words of the status column (PO, 2026-10-26: «correcto, no necesario o incorrecto»).
        Set(data, es, en, "M1/Legend_Correct", "Correcto", "Correct");
        Set(data, es, en, "M1/Legend_NoEffect", "No necesario", "Not needed");
        Set(data, es, en, "M1/Legend_Incorrect", "Incorrecto", "Incorrect");
        Set(data, es, en, "M1/Legend_Omission", "Omisión", "Omission");
        Set(data, es, en, "M1/Legend_AtRisk", "En riesgo", "At risk");
        Set(data, es, en, "M1/Rank_Knowledgeable_Title", "Conocedor", "Knowledgeable");
        Set(data, es, en, "M1/Rank_Knowledgeable", "Has preparado bien el recorrido. Sigues con el Módulo 2.", "You prepared the ride well. You can move on to Module 2.");
        Set(data, es, en, "M1/Rank_NeedsImprovement_Title", "Por mejorar", "Needs improvement");
        Set(data, es, en, "M1/Rank_NeedsImprovement", "Vas por buen camino, pero se te escapó equipo de núcleo. Repite el módulo y fíjate en lo que quedó sin marcar.", "You are on the right track, but you missed core gear. Repeat the module and look at what you left unchecked.");
        Set(data, es, en, "M1/Rank_Reconsider_Title", "Recapacitar", "Reconsider");
        Set(data, es, en, "M1/Rank_Reconsider", "Faltó buena parte del equipo imprescindible. Vuelve a recorrer el garaje sin prisa antes de circular.", "A good part of the essential gear was missing. Walk the garage again, without rushing, before riding.");
        Set(data, es, en, "M1/Rank_Inexperienced_Title", "Inexperto", "Inexperienced");
        Set(data, es, en, "M1/Rank_Inexperienced", "Casi nada de lo imprescindible quedó cubierto. Repite el módulo y revisa cada estación con calma.", "Almost none of the essentials were covered. Repeat the module and go through each station calmly.");
        Set(data, es, en, "M1/ContinueModule2", "Continuar al Módulo 2", "Continue to Module 2");
        Set(data, es, en, "M1/RepeatModule", "Repetir Módulo", "Repeat module");
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
