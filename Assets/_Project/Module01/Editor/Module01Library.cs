
namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// La biblioteca de EPP del cliente, transcrita.
    /// <para>
    /// Fuente: <c>RideSafe_VR_equipment_image_reference_library_2026-10-19.xlsx</c>, pestañas
    /// «Biblioteca» y «Sistema de puntuación». <b>Esa hoja manda</b> (Charlie, 2026-10-22): lo
    /// que no esté aquí no va a la escena, por mucho que exista el modelo.
    /// </para>
    /// <para>
    /// <b>Por qué existe este archivo.</b> Antes el catálogo vivía repartido en tres tablas
    /// —<c>Module01Greybox.Items</c> la categoría, <c>Module01SceneEntities</c> el objeto de
    /// arte y <c>Module01RealModels</c> el FBX— y nada ataba las tres al documento del cliente.
    /// Así es como la escena acabó con cinco elementos que Daniel modeló para adelantar trabajo
    /// —la bufanda, el jean ancho, el pantalón deportivo, las coderas y las rodilleras— y que la
    /// hoja no pide. Aquí cada fila lleva su <see cref="Entry.LibraryId"/>, así que un elemento
    /// sin ID de la hoja no se puede colar sin que se note.
    /// </para>
    /// <para>
    /// <b>Los ids del motor se conservan</b> donde ya existían (<c>helmet_ok</c>,
    /// <c>shoes_ok</c>…). No es cosmética: el id es la clave de I2 y el nombre del mp3 de la
    /// locución, así que renombrarlos tiraría ocho términos traducidos y sus veinte audios.
    /// </para>
    /// </summary>
    public static class Module01Library
    {
        /// <summary>
        /// Las tres mesas de exhibición de la hoja (columna «Mesa / grupo»). Son también los
        /// <c>zoneId</c> del catálogo, así que entran en el id de cada entidad
        /// (<c>module01.{zona}.{elemento}</c>).
        /// <para>
        /// <b>La Mesa 2 se llama <c>control</c> y no <c>visibility</c> a propósito:</b> la zona
        /// de «Luces y visibilidad» del vehículo ya ocupa ese id, y dos zonas con el mismo
        /// nombre colisionarían en los ids de entidad sin que nada avisara hasta que una de las
        /// dos dejara de recibir clics.
        /// </para>
        /// </summary>
        public const string Mesa1 = "protection";
        public const string Mesa2 = "control";
        public const string Mesa3 = "readiness";

        /// <summary>(id de la mesa, clave del título, nombre en la hoja).</summary>
        public static readonly (string Id, string TitleKey, string SheetName)[] Tables =
        {
            (Mesa1, "M1/Zone_Protection", "Mesa 1 — Protección personal"),
            (Mesa2, "M1/Zone_Control",    "Mesa 2 — Visibilidad y control"),
            (Mesa3, "M1/Zone_Readiness",  "Mesa 3 — Preparación del trayecto"),
        };

        /// <summary>Los elementos de una mesa, en el orden de la hoja.</summary>
        public static System.Collections.Generic.List<Entry> Of(string table)
        {
            var list = new System.Collections.Generic.List<Entry>();
            foreach (Entry e in All)
                if (e.Table == table)
                    list.Add(e);
            return list;
        }

        /// <summary>Carpeta de los modelos; vacío en <see cref="Entry.Model"/> = todavía sin 3D.</summary>
        public const string Models = "Assets/_Project/Models/CITY/MODELADOS 3d/";

        /// <summary>La carpeta que Daniel entregó con los esenciales buenos.</summary>
        public const string Essentials = Models + "EPPS ESCENCIALES/";

        public readonly struct Entry
        {
            public Entry(string libraryId, string itemId, SafetyItemCategory category,
                         float weight, string table,
                         string model = null, string hazardGroup = null,
                         string exclusiveBonusWith = null, string sceneObject = null,
                         float targetWidth = 0f)
            {
                LibraryId = libraryId;
                ItemId = itemId;
                Category = category;
                Weight = weight;
                Table = table;
                Model = model;
                HazardGroup = hazardGroup;
                ExclusiveBonusWith = exclusiveBonusWith;
                SceneObject = sceneObject;
                TargetWidth = targetWidth;
            }

            /// <summary>El ID de la hoja: E01, O04, H13… Es la trazabilidad con el cliente.</summary>
            public string LibraryId { get; }

            /// <summary>El id del catálogo, que es también la clave de I2 y el nombre del mp3.</summary>
            public string ItemId { get; }

            public SafetyItemCategory Category { get; }

            /// <summary>Columna «Peso / puntos» de la hoja.</summary>
            public float Weight { get; }

            /// <summary>Mesa de exhibición: <see cref="Mesa1"/>, <see cref="Mesa2"/> o <see cref="Mesa3"/>.</summary>
            public string Table { get; }

            /// <summary>Ruta del FBX, o <c>null</c> si 3D todavía no lo ha entregado.</summary>
            public string Model { get; }

            /// <summary>Columna «Grupo / regla»; solo para los peligros.</summary>
            public string HazardGroup { get; }

            /// <summary><c>exclusive_bonus_with</c> de la hoja.</summary>
            public string ExclusiveBonusWith { get; }

            /// <summary>
            /// Nombre del objeto que el arte dejó en la escena, si lo hay. Cuando es
            /// <c>null</c> el elemento se monta como marcador: el signo «!», igual que los
            /// puntos del vehículo que no tienen pieza propia.
            /// </summary>
            public string SceneObject { get; }

            /// <summary>
            /// Cuánto debe medir el objeto <b>de ancho, en metros de mundo</b>: su dimensión
            /// horizontal mayor. 0 = dejarlo como venga.
            /// <para>
            /// <b>Por qué existe.</b> Los modelos de la carpeta de esenciales llegan a escalas
            /// disparatadas: el casco mide 1,02 m de ancho, el kit de pinchazos 2,06 y la
            /// chaqueta impermeable 5 cm. Medido en la escena el 2026-10-22, y con eso
            /// «Protección personal» pedía 4,61 m sobre una tapa de 3,34.
            /// </para>
            /// <para>
            /// <b>Se declara el tamaño, no un factor de escala</b>, y eso importa: el montador
            /// calcula <c>escala = objetivo / lo que mide</c>, así que el día que 3D reexporte
            /// a escala real el factor sale 1 solo y esta tabla no hay que deshacerla. Un factor
            /// fijo habría que quitarlo a mano y nadie se acordaría.
            /// </para>
            /// <para>
            /// De dónde salen los números: los cinco que tienen equivalente viejo se midieron
            /// contra él —el casco contra <c>casco 1</c> (0,35), el calzado contra <c>botas</c>
            /// (0,35), las gafas contra <c>gafas</c> (0,17), el chaleco contra <c>chaleco
            /// refectivo</c> (0,45)—. Los demás son el tamaño real del objeto físico, puesto por
            /// Claude y pendiente de que 3D lo confirme.
            /// </para>
            /// </summary>
            public float TargetWidth { get; }

            /// <summary>Todavía no hay modelo: va con el signo «!».</summary>
            public bool NeedsMarker => string.IsNullOrEmpty(Model);

            public string NameKey => "M1/Item_" + ToPascal(ItemId);

            private static string ToPascal(string id)
            {
                var sb = new System.Text.StringBuilder(id.Length);
                bool up = true;
                foreach (char c in id)
                {
                    if (c == '_') { up = true; continue; }
                    sb.Append(up ? char.ToUpperInvariant(c) : c);
                    up = false;
                }
                return sb.ToString();
            }
        }

        private const SafetyItemCategory Core = SafetyItemCategory.Core;
        private const SafetyItemCategory Opt = SafetyItemCategory.Optional;
        private const SafetyItemCategory Bad = SafetyItemCategory.Inappropriate;

        /// <summary>
        /// Los 32 elementos de la hoja. El orden es el del documento, para poder cotejarlos
        /// de un vistazo.
        /// <para>
        /// <b>E02 y E03 entran como recomendados, no como esenciales, y es literal de la
        /// hoja.</b> Son «esencial condicional»: valen 15 de peso esencial <i>cuando aplican</i>
        /// —noche, lluvia, niebla— y la propia hoja dice «+1 de bono en día claro». La escena es
        /// de día claro, así que hoy son bono de 1. El día que haya escenario nocturno pasan a
        /// <see cref="SafetyItemCategory.Core"/> con peso 15 y el pool esencial sube de 65 a 95.
        /// </para>
        /// <para>
        /// <b>N01, el teléfono, es neutral.</b> La hoja lo saca de la evaluación —«no cambia el
        /// resultado»— y lo manda al video de feedback. Va como recomendado con peso 0: sigue en
        /// la mesa y en el reporte, no penaliza, no da bono y no cuenta para el contador de
        /// peligros. Ojo: <b>hasta hoy sí penalizaba</b>, era <c>cargo_handheld</c> inapropiado.
        /// </para>
        /// </summary>
        public static readonly Entry[] All =
        {
            // ---- Esenciales ------------------------------------------------------------
            new Entry("E01", "helmet_ok", Core, 40f, Mesa1,
                      model: Essentials + "Casco certificado.fbx",
                      targetWidth: 0.30f),  // medido contra casco 1 (0,35) y df_g_helmet_01 (0,285)
            new Entry("E04", "shoes_ok", Core, 20f, Mesa1,
                      model: Essentials + "zapatos de seguridad.fbx",
                      targetWidth: 0.35f),  // medido contra botas (0,349)
            new Entry("E05", "trousers_fitted", Core, 5f, Mesa3,
                      model: Essentials + "Slim_fit_pants_doblado.fbx",
                      targetWidth: 0.36f),  // ya llega bien; igual que jean_ajustado_doblado (0,317)

            // ---- Esenciales condicionales: hoy, con día claro, valen +1 de bono ---------
            new Entry("E02", "light_front", Opt, 1f, Mesa2,
                      model: Models + "luz grontal.fbx",
                      targetWidth: 0.10f),
                      // `luz grontal` en vez de `lintarna`: viene a escala real -0,03 x 0,05 x 0,10-
                      // y trae un hijo `soporte`, que es justo lo que pide la direccion artistica de
                      // E02 en la hoja («soporte de manillar visible»). `lintarna` medía 1,17 m y no
                      // tiene soporte. Charlie, 2026-10-22.
            new Entry("E03", "light_rear", Opt, 1f, Mesa2,
                      model: Essentials + "luz_tracera.fbx",
                      targetWidth: 0.09f),  // luz de bici real; hoy mide 0,98

            // ---- Recomendados ----------------------------------------------------------
            new Entry("O01", "hiviz_day", Opt, 2f, Mesa2,
                      model: Models + "rompe veintos.fbx",
                      exclusiveBonusWith: "reflective",
                      targetWidth: 0.45f),
                      // `rompe veintos` NO es «otro rompevientos»: renderizado se ve que es una
                      // chaqueta amarillo fluor con bandas reflectivas, que es literalmente O01.
                      // El de la capucha (`rompe vientos_capucha`) se queda para O16.
            new Entry("O02", "reflective", Opt, 3f, Mesa2,
                      model: Essentials + "chaleco reflectivo.fbx",
                      exclusiveBonusWith: "hiviz_day",
                      targetWidth: 0.36f),  // el chaleco nuevo es mas esbelto que el viejo (relacion alto/ancho
                      // 1,61 frente a 1,33), asi que con 0,45 de ancho media 0,73 de alto y
                      // StationLayout.FloorHeight (0,70) lo plantaba en el suelo. Con 0,36
                      // mide 0,58 y vuelve a la mesa, que es donde estaba el viejo.
            new Entry("O04", "gloves", Opt, 2f, Mesa1,
                      model: Essentials + "gloves.fbx",
                      targetWidth: 0.26f),  // ya llega bien
            new Entry("O07", "glasses", Opt, 1f, Mesa1,
                      model: Models + "gafdas de seguridad.fbx",
                      targetWidth: 0.17f),  // medido contra gafas (0,174)
            new Entry("O08", "sunglasses", Opt, 1f, Mesa1,
                      model: Essentials + "gafas de sol.fbx", targetWidth: 0.17f),
                      // Montura negra tipo Holbrook, la que pide la hoja: llegó el 2026-10-26 y
                      // reemplaza a las `gafas` oscuras de siempre, que se apagan. El ancho es el
                      // de aquellas (0,17). Las transparentes siguen siendo O07 (PO: las
                      // `gafas_deportivas` no la reemplazan).
            new Entry("O09", "backpack", Opt, 2f, Mesa3,
                      model: Essentials + "mochila.fbx", targetWidth: 0.32f),
                      // Mochila de diario real, ~32 cm de ancho. La textura trae un logo que arte
                      // cambia después (PO, 2026-10-26): no afecta al modelo.
            new Entry("O11", "cargo_secured", Opt, 2f, Mesa3,
                      model: Essentials + "candado.fbx", targetWidth: 0.22f),
                      // La hoja pide «Candado con soporte o ubicación de transporte segura»:
                      // un candado, no una guaya. `guaya candado` es una guaya enrollada y se
                      // queda fuera, igual que `guayya dañado`, su pareja deteriorada: ninguna
                      // de las dos tiene fila en el Excel. Desde el 2026-10-27 es el candado con
                      // textura de la carpeta de esenciales, al ancho que tenía el viejo de la
                      // escena (0,22), que se apaga.
            new Entry("O12", "flat_kit", Opt, 2f, Mesa3,
                      model: Models + "cartuchera_low.fbx",
                      targetWidth: 0.20f),
                      // `cartuchera_low` es un estuche con cremallera -visto renderizado-, que
                      // lee como «kit compacto» mucho mejor que `parch`, una lamina suelta.
            new Entry("O13", "multitool", Opt, 1f, Mesa3,
                      model: Models + "Tool_Tajima_lowpoly_02.fbx",
                      targetWidth: 0.12f),  // multiherramienta cerrada; hoy mide 1,82
            new Entry("O14", "phone_mount", Opt, 1f, Mesa2),
            new Entry("O15", "bell", Opt, 2f, Mesa2),
            new Entry("O16", "rain_jacket", Opt, 2f, Mesa3,
                      model: Essentials + "rompe vientos_capucha.fbx",
                      targetWidth: 0.40f),  // chaqueta doblada; hoy mide 0,05, es la unica demasiado PEQUENA
            new Entry("O18", "bottle", Opt, 1f, Mesa3),
            new Entry("O19", "pannier", Opt, 3f, Mesa3),

            // ---- Peligrosos ------------------------------------------------------------
            new Entry("H01", "helmet_strap", Bad, 30f, Mesa1,
                      hazardGroup: "helmet_integrity"),
            new Entry("H02", "helmet_damaged", Bad, 30f, Mesa1,
                      model: Models + "casco 1.fbx", hazardGroup: "helmet_integrity",
                      sceneObject: "casco 1"),
            new Entry("H03", "headphones", Bad, 20f, Mesa2,
                      model: Models + "audifonos.fbx", hazardGroup: "auditory_occlusion",
                      targetWidth: 0.18f),  // auriculares de diadema; hoy mide 0,13
            new Entry("H13", "airpods", Bad, 20f, Mesa2,
                      hazardGroup: "auditory_occlusion"),
            new Entry("H04", "sandals", Bad, 15f, Mesa1,
                      model: Models + "Slippers.fbx", hazardGroup: "footwear_control",
                      targetWidth: 0.30f),  // par de chanclas; hoy mide 0,43
            new Entry("H06", "poncho", Bad, 20f, Mesa3,
                      hazardGroup: "loose_rainwear"),
            new Entry("H07", "glasses_damaged", Bad, 20f, Mesa1,
                      hazardGroup: "visual_obstruction"),
            new Entry("H08", "mittens", Bad, 15f, Mesa2,
                      hazardGroup: "control_access"),
            new Entry("H09", "tote", Bad, 15f, Mesa3,
                      hazardGroup: "unstable_carry"),
            new Entry("H10", "umbrella", Bad, 30f, Mesa3,
                      hazardGroup: "one_hand_wind"),
            new Entry("H11", "drink", Bad, 20f, Mesa2,
                      hazardGroup: "one_hand_drink"),
            new Entry("H12", "cap", Bad, 20f, Mesa1,
                      hazardGroup: "helmet_substitution"),

            // ---- Neutral ---------------------------------------------------------------
            new Entry("N01", "cargo_handheld", Opt, 0f, Mesa2,
                      model: Models + "MobilePhone_01.fbx", sceneObject: "MobilePhone_01"),
        };

        /// <summary>
        /// Lo que Daniel modeló para adelantar trabajo y la hoja <b>no</b> pide. Se quedan en
        /// disco —no se borra arte— pero salen del catálogo y de la escena.
        /// <para>
        /// Está escrito y no solo borrado para que la próxima vez que alguien vea la bufanda en
        /// la carpeta sepa que su ausencia es una decisión y no un olvido.
        /// </para>
        /// </summary>
        public static readonly string[] RetiredItemIds =
        {
            "clothing_loose",   // bufanda 01
            "trousers_loose",   // jean ancho doblado
            "trousers_tech",    // pantalon deportivo doblado
            "elbow_pads",       // codera (par)
            "knee_pads",        // rodillera (par)
        };

        /// <summary>
        /// Objetos de arte que <b>salen del ejercicio</b> y hay que apagar en la escena: o su
        /// elemento se retiró, o el elemento sigue pero ahora usa otro modelo.
        /// <para>
        /// Apagarlos no es cosmético. Los EPP nuevos se colocan sobre las mismas tapas —la
        /// `mesa` pasó de llevar tres pantalones a llevar los once de «Preparación del
        /// trayecto»— así que dejar el arte viejo encima significa objetos superpuestos: el
        /// aprendiz ve dos pantalones y solo uno responde al clic. Ya pasó con los pantalones de
        /// pie del v9.
        /// </para>
        /// <para>
        /// <b>Se apagan, no se borran.</b> Son modelos del equipo de arte y cada entrega del
        /// garaje los vuelve a traer; apagarlos es reversible con un clic y repetir el comando
        /// los deja apagados otra vez.
        /// </para>
        /// <para>
        /// No están aquí <c>casco 1</c>, <c>guaya candado</c> ni <c>MobilePhone_01</c>: esos
        /// tres siguen siendo el modelo de su elemento y se declaran en
        /// <see cref="Entry.SceneObject"/>.
        /// </para>
        /// </summary>
        public static readonly string[] RetiredSceneObjects =
        {
            // Siguen en el catálogo, pero con el modelo bueno de la carpeta de esenciales.
            "df_g_helmet_01",               // helmet_ok -> Casco certificado
            "botas",                        // shoes_ok  -> zapatos de seguridad
            "chaleco refectivo",            // reflective -> chaleco reflectivo (sin la errata)
            "jean_ajustado_doblado",        // trousers_fitted -> Slim_fit_pants_doblado
            "gafas",                        // sunglasses -> gafas de sol (2026-10-26)
            "candado",                      // cargo_secured -> candado con textura (2026-10-27)
            // Retirados: la hoja no los pide.
            "bufanda 01",
            "jean ancho doblado",
            "pantalon deportivo doblado",
            "coderas",
            "rodilleras",
        };
    }
}
