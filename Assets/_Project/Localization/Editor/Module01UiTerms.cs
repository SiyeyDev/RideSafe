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

        // The stops, one per piece of furniture (PO, 2026-10-27): the artist spread each table of
        // the sheet over several, so a stop is what is on one of them, seen from the front.
        Set(data, es, en, "M1/Stop_Entrance", "Mesa de la entrada", "Entrance table");
        Set(data, es, en, "M1/Stop_Middle", "Mesa del centro", "Middle table");
        Set(data, es, en, "M1/Stop_Back", "Mesa del fondo", "Back table");
        Set(data, es, en, "M1/Stop_Clothes", "Mostrador de la ropa", "Clothing counter");
        // The tool bench is a stop again (PO, 2026-10-29): the other four held 61 items and the
        // back table was at 94 % of its worktop while this one stood empty.
        Set(data, es, en, "M1/Stop_Tools", "Mesa de herramientas", "Tool bench");

        // Everything the artist modelled can be chosen (PO, 2026-10-28). The pads came back as
        // «recommended conditional»; the rest is neutral, or a hazard when it obviously is one.
        // Their names were missing from I2, so the popup read «M1/Item_KneePads».
        foreach (var (key, es_, en_, riskEs, riskEn) in Items)
        {
            Set(data, es, en, "M1/Item_" + key, es_, en_);
            Set(data, es, en, "M1/Item_" + key + "_Risk", riskEs, riskEn);
        }
        foreach (string key in NeutralCopies)
            Set(data, es, en, "M1/Item_" + key + "_Risk", "No cambia el resultado.", "It does not change the result.");

        // Module 0 location screen (PO, 2026-10-29): a location to pick, not a rule pack to read; the
        // cards name the region over its flag. The keys keep their ids (florida / bogota).
        Set(data, es, en, "M0/Location_Title", "Elige tu ubicación", "Choose your location");
        Set(data, es, en, "M0/Location_Florida", "EE. UU.", "United States");
        Set(data, es, en, "M0/Location_Bogota", "Latinoamérica", "Latin America");

        // Narratives §4.8.2: the scooter also checks its deck and fasteners; the e-bike has none.
        Set(data, es, en, "M1/Zone_Deck", "Plataforma", "Deck");
        Set(data, es, en, "M1/Point_Deck", "Plataforma y fijaciones", "Deck and fasteners");
        Set(data, es, en, "M1/Point_Deck_Risk", "Una plataforma agrietada o con tornillos flojos puede ceder bajo tu peso.", "A cracked deck or loose bolts can give way under your weight.");
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

        // Panel de la nota final (PO, 2026-10-29): el promedio de las dos secciones y si pasa.
        Set(data, es, en, "M1/FinalScore_Title", "Puntuación final", "Final score");
        Set(data, es, en, "M1/FinalScore_Gear", "Equipo", "Gear");
        Set(data, es, en, "M1/FinalScore_Vehicle", "Vehículo", "Vehicle");
        Set(data, es, en, "M1/FinalScore_CanContinue",
            "Puedes continuar al Módulo 2.", "You can continue to Module 2.");
        Set(data, es, en, "M1/FinalScore_MustRepeat",
            "Necesitas más de 70 para continuar. Repite el módulo.",
            "You need more than 70 to continue. Repeat the module.");
        // The video's «Reproduciendo» tag was dropped (PO, 2026-10-16): its term goes too.
        data.RemoveTerm("M1/Video_Playing");
        // The pouch stopped being an item (2026-10-28). «M1/Stop_Tools» was dropped with it and
        // is set again above: the bench came back as a stop on 2026-10-29.
        foreach (string gone in new[] { "M1/Item_Pouch", "M1/Item_Pouch_Risk" })
            data.RemoveTerm(gone);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }

    /// <summary>(key after «M1/Item_», name es, name en, why es, why en).</summary>
    private static readonly (string Key, string Es, string En, string RiskEs, string RiskEn)[] Items =
    {
        ("ElbowPads", "Coderas", "Elbow pads",
         "Las coderas amortiguan el primer punto de contacto en una caída.", "Elbow pads cushion the first point of contact in a fall."),
        ("KneePads", "Rodilleras", "Knee pads",
         "Las rodilleras protegen la articulación contra el suelo y el marco.", "Knee pads protect the joint against the ground and the frame."),
        ("Flashlight", "Linterna de mano", "Hand torch",
         "Una linterna en la mano no sustituye a la luz del vehículo.", "A torch in your hand is no substitute for the vehicle's light."),
        ("TrousersStanding", "Pantalón ajustado", "Fitted trousers",
         "No cambia el resultado.", "It does not change the result."),
        ("Snorkel", "Máscara de snorkel", "Snorkel mask",
         "No es equipo para circular: no cambia el resultado.", "It is not riding gear: it does not change the result."),
        ("SportGlasses", "Gafas deportivas", "Sport glasses",
         "No cambian el resultado.", "They do not change the result."),
        ("WorkVest", "Chaleco de trabajo", "Work vest",
         "No cambia el resultado.", "It does not change the result."),
        ("WorkGoggles", "Gafas de trabajo", "Work goggles",
         "No cambian el resultado.", "They do not change the result."),
        ("SpareRedLight", "Luz roja de repuesto", "Spare red light",
         "Una luz de repuesto en la mesa no ilumina el vehículo.", "A spare light on the table does not light the vehicle."),
        ("LoosePadlock", "Candado suelto", "Loose padlock",
         "Sin soporte no hay dónde llevarlo seguro: no cambia el resultado.", "Without a mount there is nowhere safe to carry it: it does not change the result."),
        ("CableLock", "Guaya con candado", "Cable lock",
         "No cambia el resultado.", "It does not change the result."),
        ("DamagedCableLock", "Guaya dañada", "Damaged cable lock",
         "Una guaya rota no asegura el vehículo: no cambia el resultado.", "A broken cable does not secure the vehicle: it does not change the result."),
        ("WorkGloves", "Guantes de trabajo", "Work gloves",
         "No cambian el resultado.", "They do not change the result."),
        ("LatexGloves", "Guantes de látex", "Latex gloves",
         "No protegen en una caída: no cambian el resultado.", "They give no protection in a fall: they do not change the result."),
        ("FabricGloves", "Guantes de tela", "Fabric gloves",
         "No cambian el resultado.", "They do not change the result."),
        ("FaceMask", "Tapabocas", "Face mask",
         "No cambia el resultado.", "It does not change the result."),
        ("FoldedJeans", "Jean ajustado doblado", "Folded fitted jeans",
         "No cambia el resultado.", "It does not change the result."),
        ("TrousersTech", "Pantalón deportivo", "Sport trousers",
         "El pantalón deportivo no se engancha, pero no protege la piel en una caída.", "Sport trousers do not catch, but they do not protect the skin in a fall."),
        ("Jeans", "Jean ajustado", "Fitted jeans",
         "No cambia el resultado.", "It does not change the result."),
        ("LowRiseTrousers", "Pantalón de tiro bajo", "Low-rise trousers",
         "No cambia el resultado.", "It does not change the result."),
        ("Sneakers", "Tenis deportivos", "Sports trainers",
         "No cambian el resultado.", "They do not change the result."),
        ("Boots", "Botas rojas", "Red boots",
         "No cambian el resultado.", "They do not change the result."),
        ("ShowerSlippers", "Chanclas de baño", "Shower slippers",
         "Sin talón ni sujeción, el pie se sale del pedal.", "With no heel or strap, the foot slips off the pedal."),
        ("ClothingLoose", "Bufanda suelta", "Loose scarf",
         "La ropa suelta llega a la cadena y a los rayos.", "Loose clothing reaches the chain and the spokes."),
        ("NeckScarf", "Pañuelo suelto", "Loose neckerchief",
         "Un pañuelo suelto se puede enganchar en la cadena o en la rueda.", "A loose neckerchief can catch in the chain or the wheel."),
        ("TrousersLoose", "Pantalón ancho", "Loose trousers",
         "La pernera ancha se puede enganchar en la cadena o en la rueda y bloquearla.", "A wide leg can catch in the chain or the wheel and lock it."),
        ("WideTrousers", "Pantalón muy ancho", "Very wide trousers",
         "La pernera ancha se puede enganchar en la cadena o en la rueda y bloquearla.", "A wide leg can catch in the chain or the wheel and lock it."),
        ("LooseLaces", "Tenis con cordones sueltos", "Trainers with loose laces",
         "Un cordón suelto se enreda en la cadena o en el pedal.", "A loose lace tangles in the chain or the pedal."),
        ("OldHeadphones", "Audífonos de casco", "On-ear headphones",
         "Tapan los dos oídos: no oyes el tráfico.", "They cover both ears: you cannot hear the traffic."),
        // Variants the artist placed in v5 on 2026-10-29 (PO decided how each one scores).
        ("HelmetCracked", "Casco certificado agrietado", "Cracked certified helmet",
         "Un casco agrietado ya no absorbe el siguiente golpe.", "A cracked helmet no longer absorbs the next impact."),
        ("VestGrey", "Chaleco gris", "Grey vest",
         "No es reflectivo: no cambia el resultado.", "It is not reflective: it does not change the result."),
    };

    /// <summary>
    /// Identical copies of an item on another table (PO, 2026-10-29): they keep the original's
    /// name and voice-over, but are neutral, so their explanation is their own.
    /// </summary>
    private static readonly string[] NeutralCopies = { "GlovesSpare", "GlovesSpare2", "ShoesSpare", "ReflectiveSpare" };

    private static void Set(LanguageSourceData data, int es, int en, string key, string spanish, string english)
    {
        TermData term = data.GetTermData(key) ?? data.AddTerm(key);
        term.Languages[es] = spanish;
        term.Languages[en] = english;
    }
}
