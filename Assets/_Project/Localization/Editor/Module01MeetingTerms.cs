using I2.Loc;
using UnityEditor;

/// <summary>
/// Textos de la reunión del 2026-11-11 con el cliente, en es-CO y en (mismo patrón que
/// <see cref="Module01UiTerms"/>). El inglés es el origen; la hoja del Drive no manda.
/// <para>
/// Los nombres de los EPP salen del Excel de EPP del 2026-11-13, la referencia del proyecto; los demás
/// textos, de la hoja del cliente «Textos Modulo 1». No se inventa ninguno (Charlie, 2026-11-18).
/// Unity lee I2 y no esas hojas: esto los lleva al asset.
/// </para>
/// </summary>
public static class Module01MeetingTerms
{
    [MenuItem("RideSafe/Módulo 1/Añadir textos de la reunión del 11-11 (I2)")]
    public static void Apply()
    {
        LanguageSourceAsset asset = AssetDatabase.LoadAssetAtPath<LanguageSourceAsset>("Assets/Resources/I2Languages.asset");
        LanguageSourceData data = asset.SourceData;
        int es = data.GetLanguageIndexFromCode("es-CO");
        int en = data.GetLanguageIndexFromCode("en");
        if (es < 0 || en < 0)
            throw new System.InvalidOperationException("I2Languages has no es-CO or en.");

        // Felicitación al aprobar una sección tras el video, y cierre del módulo.
        Set(data, es, en, "M1/Congrats_Title", "¡Felicitaciones!", "Congratulations!");
        Set(data, es, en, "M1/Congrats_Body", "Seleccionaste de manera correcta.", "You made the right choices.");
        // Cierre tras el video de la inspección (hoja «Feedback Modulo 1», 8:57).
        // El título va corto: «¡Felicitaciones! Completaste el módulo 1» no cabe en el panel (PlayMode).
        Set(data, es, en, "M1/Complete_Title", "¡Felicitaciones!", "Congratulations!");
        Set(data, es, en, "M1/Complete_Body",
            "Completaste el módulo 1. Ya conoces los EPP que siempre debes usar y los componentes que siempre debes inspeccionar antes de conducir.",
            "You completed Module 1. You now know the protective gear you must always wear and the components you must always inspect before riding.");
        Set(data, es, en, "M1/Complete_Continue", "Continuar al módulo 2", "Continue to Module 2");

        // No aprobada la sección tras el video (Charlie, 2026-11-18). El inglés es el que decía el
        // flujo del 2026-11-07; el español, su traducción.
        Set(data, es, en, "M1/Retry_Title", "Intentémoslo de nuevo", "Let's try again");
        Set(data, es, en, "M1/Retry_Epp_Body",
            "Faltó equipo esencial o elegiste algo inadecuado. Vuelve a la primera mesa y elige tu equipo de nuevo.",
            "Some essential gear was missing or unsuitable. Go back to the first table and choose your gear again.");
        Set(data, es, en, "M1/Retry_Vehicle_Body",
            "Algunas partes del vehículo quedaron sin revisar. Revísalo de nuevo, parte por parte.",
            "Some parts of the vehicle were left unchecked. Check it again, part by part.");
        Set(data, es, en, "M1/Retry_Button", "Intentar de nuevo", "Try again");

        // El tutorial de controles del módulo 0 (SO_Tutorial_PointSelect) pedía estas dos claves y
        // no existían: se veía la clave. Para los controles de VR.
        Set(data, es, en, "Tutorial/Point", "Apunta al objetivo con el control derecho.", "Point at the target with your right controller.");
        Set(data, es, en, "Tutorial/Select", "Presiona el gatillo para seleccionarlo.", "Pull the trigger to select it.");

        // Voz en off al empezar la sección de EPP, palabra por palabra de la hoja «Feedback Modulo 1» (1:06).
        Set(data, es, en, "M1/Selection_Instruction",
            "Selecciona los elementos y accesorios para verlos en detalle. Elige únicamente aquellos que consideres necesarios y útiles para protegerte ante cualquier eventualidad.",
            "Select the items and accessories to see them in detail. Choose only the ones you consider necessary and useful to protect yourself in any situation.");
        // Al pulsar Iniciar en el módulo 0: bienvenida y cómo se elige con el control (reunión
        // 2026-11-11, 13:03; Charlie, 2026-11-18).
        Set(data, es, en, "M1/Welcome_Controller",
            "¡Bienvenido a RideSafe! Ahora vas a elegir la configuración de tu entrenamiento. Para seleccionar, apunta con el control derecho a una opción y aprieta el gatillo.",
            "Welcome to RideSafe! Now you'll choose your training settings. To select, point at an option with your right controller and pull the trigger.");
        // La mecánica con el control, tras la instrucción de cada sección del módulo 1 (12:23).
        Set(data, es, en, "M1/Selection_Mechanic",
            "Apunta a cada elemento con el control derecho y aprieta el gatillo para verlo en detalle. Después decide si lo agregas a tu lista.",
            "Point at each item with your right controller and pull the trigger to see it in detail. Then decide whether to add it to your list.");
        Set(data, es, en, "M1/Inspect_Mechanic",
            "Apunta a cada componente marcado con el control derecho y aprieta el gatillo para revisarlo.",
            "Point at each marked component with your right controller and pull the trigger to check it.");

        // Transmisión, solo la bici. El punto lleva el nombre de la hoja del cliente.
        Set(data, es, en, "M1/Zone_Drivetrain", "Transmisión", "Drivetrain");
        Set(data, es, en, "M1/Point_Drivetrain_Risk",
            "Una cadena seca o floja, o una corona gastada, se puede salir o romper mientras pedaleas.",
            "A dry or slack chain, or a worn chainring, can slip or snap while you pedal.");

        // Los textos que el cliente corrigió en su hoja «Textos Modulo 1» (pestaña Module1).
        foreach ((string key, string spanish, string english) in ClientTexts)
            Set(data, es, en, "M1/" + key, spanish, english);

        foreach ((string key, string spanish, string english) in ShortNames)
        {
            // Solo se renombra: una clave que no existe es una errata, no un término nuevo.
            if (data.GetTermData("M1/Item_" + key) == null)
                throw new System.InvalidOperationException("No existe M1/Item_" + key);
            Set(data, es, en, "M1/Item_" + key, spanish, english);
        }

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Lo que el cliente cambió en la pestaña Module1 de su hoja «Textos Modulo 1» respecto a
    /// nuestra hoja de I2 (2026-11-18). Se toma su español y solo se corrigen tildes y erratas
    /// («selecionados», «preparate», y «Tire presure», que puso en inglés en la columna del
    /// español). Su inglés sale de GOOGLETRANSLATE: aquí va revisado. M02_Welcome es la voz de
    /// entrada a la sección del vehículo.
    /// </summary>
    public static readonly (string Key, string Es, string En)[] ClientTexts =
    {
        ("M01_Welcome",
            "¡Bienvenido a RideSafe! En este primer módulo vas a aprender a prepararte para un viaje seguro. Identificarás los elementos básicos de protección personal y los componentes de tu vehículo que debes inspeccionar cada vez que te dispongas a manejar. ¡Comencemos!",
            "Welcome to RideSafe! In this first module, you'll learn how to prepare for a safe ride. You'll identify the basic personal protective equipment and the vehicle components you should inspect every time you're about to ride. Let's get started!"),
        ("M02_Welcome",
            "En esta parte del módulo vamos a identificar los precursores que debes tener en cuenta al momento de comenzar a manejar el vehículo.",
            "In this part of the module, we'll identify the precursors you should keep in mind when you start riding the vehicle."),
        ("Choose_Instruction", "Selecciona un objeto para examinarlo. Puedes editar tus elecciones antes de presentarlas.",
            "Select an object to examine it. You can edit your choices before submitting them."),
        ("Confirm_Question", "¿Quieres añadir este elemento a tu lista?", "Do you want to add this item to your list?"),
        ("Inspect_Instruction", "Inspecciona cada componente antes de conducir.", "Inspect each component before riding."),
        ("Next", "Continuar", "Continue"),
        ("Orientation_Title", "Prepárate antes de manejar", "Get ready before you ride"),
        ("Orientation_Body", "Recorre las estaciones, selecciona los elementos de protección necesarios y evita los elementos distractores.",
            "Walk through the stations, select the protective equipment you need and avoid distracting items."),
        ("Point_Brakes", "Ajuste de frenos", "Brake adjustment"),
        ("Point_Electrics", "Encendido y sistema eléctrico", "Power-on and electrical system"),
        ("Point_Steering", "Dirección de manubrio", "Handlebar steering"),
        ("Point_Wheels", "Presión de llantas", "Tire pressure"),
        ("Point_Drivetrain", "Sistema de transmisión", "Drivetrain"),
        ("Report_Subtitle", "", ""),
        ("Report_Yours", "Resultados de tus selecciones", "Results of your selections"),
        ("Review_Title", "Revisa los elementos seleccionados", "Review the selected items"),
        ("Review_Subtitle", "Verifica los elementos de seguridad seleccionados antes de continuar.",
            "Check the safety items you selected before continuing."),
        ("Sidebar_Title", "Elementos seleccionados", "Selected items"),
        ("Zone_Cockpit", "Freno delantero y trasero", "Front and rear brakes"),
    };

    /// <summary>
    /// El nombre de cada elemento: columna C de «Sistema de puntuación» del Excel del 2026-11-13,
    /// palabra por palabra (clave tras «M1/Item_»). El Excel es la referencia: un nombre que parezca
    /// mal se valida con él, no se corrige aquí (Charlie, 2026-11-18). El inglés es traducción
    /// nuestra, porque el Excel solo trae español.
    /// </summary>
    public static readonly (string Key, string Es, string En)[] ShortNames =
    {
        ("HelmetOk", "Casco certificado, talla correcta y abrochado", "Certified helmet, correct size and fastened"), // E01
        ("LightFront", "Luz delantera blanca", "White front light"), // E02
        ("LightRear", "Luz trasera roja", "Red rear light"), // E03
        ("ShoesOk", "Calzado cerrado", "Closed shoes"), // E04
        ("TrousersFitted", "Pantalones ceñida al cuerpo", "Close-fitting trousers"), // E05
        ("HivizDay", "Prenda superior fluorescente", "Fluorescent top"), // O01
        ("Reflective", "Chaleco o chaqueta retroreflectiva", "Retroreflective vest or jacket"), // O02
        ("Gloves", "Guantes completos", "Full-finger gloves"), // O04
        ("Glasses", "Gafas transparentes", "Clear glasses"), // O07
        ("Sunglasses", "Gafas solares con protección UV", "UV-protection sunglasses"), // O08
        ("Backpack", "Mochila cerrada de dos tirantes", "Closed two-strap backpack"), // O09
        ("CargoSecured", "Candado de seguridad", "Security lock"), // O11
        ("FlatKit", "Kit para reparar pinchazos", "Puncture repair kit"), // O12
        ("Multitool", "Multiherramienta", "Multi-tool"), // O13
        ("PhoneMount", "Soporte universal para teléfono", "Universal phone mount"), // O14
        ("Bell", "Campana", "Bell"), // O15
        ("RainJacket", "Chaqueta impermeable", "Rain jacket"), // O16
        ("Bottle", "Caramañola", "Water bottle"), // O18
        ("Pannier", "Alforja impermeable y reflectiva", "Waterproof reflective pannier"), // O19
        ("HelmetStrap", "Casco con correa de barbilla faltante", "Helmet with missing chin strap"), // H01
        ("HelmetDamaged", "Casco deteriorado", "Damaged helmet"), // H02
        ("Headphones", "Audífonos grandes que ocluyen ambos oídos", "Large headphones covering both ears"), // H03
        ("Sandals", "Sandalias abiertas", "Open sandals"), // H04
        ("Poncho", "Impermeable extragrande", "Oversized raincoat"), // H06
        ("GlassesDamaged", "Gafas con lente severamente rayados", "Glasses with badly scratched lenses"), // H07
        ("Mittens", "Mitones voluminosos", "Bulky mittens"), // H08
        ("Tote", "Tote abierto de una correa", "Open single-strap tote"), // H09
        ("Umbrella", "Paraguas de mano", "Handheld umbrella"), // H10
        ("Drink", "Vaso de café o bebida para llevar en la mano", "Coffee cup or takeaway drink in hand"), // H11
        ("Cap", "cachucha", "Cap"), // H12
        ("Airpods", "Auriculares", "Earbuds"), // H13
        ("CargoHandheld", "Teléfono inteligente", "Smartphone"), // N01
    };

    private static void Set(LanguageSourceData data, int es, int en, string key, string spanish, string english)
    {
        TermData term = data.GetTermData(key) ?? data.AddTerm(key);
        term.Languages[es] = spanish;
        term.Languages[en] = english;
    }
}
