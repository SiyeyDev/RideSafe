
namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// La biblioteca de EPP del cliente, transcrita.
    /// <para>
    /// Fuente: <c>Docs/RideSafe_VR_equipment_image_reference_library_2026-10-19.xlsx</c>, pestañas
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
        /// Las tres mesas de exhibición de la hoja (columna «Mesa / grupo»). Desde el 2026-10-27
        /// son solo trazabilidad con el cliente: las zonas del catálogo son las paradas
        /// (<see cref="Stops"/>), una por mueble.
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

        /// <summary>
        /// Una parada del recorrido: un mueble del garaje, mirado de frente, con lo que hay encima.
        /// </summary>
        public readonly struct Stop
        {
            public Stop(string id, string titleKey, params string[] itemIds)
            {
                Id = id;
                TitleKey = titleKey;
                ItemIds = itemIds;
            }

            /// <summary>El <c>zoneId</c> del catálogo y el nombre de su ancla (<c>anchor_{id}</c>).</summary>
            public string Id { get; }

            public string TitleKey { get; }

            public System.Collections.Generic.IReadOnlyList<string> ItemIds { get; }
        }

        /// <summary>
        /// Las paradas, en el orden del recorrido desde la puerta: una por mueble (PO, 2026-10-27).
        /// <para>
        /// <b>Por qué no son las tres mesas de la hoja.</b> La «Mesa 1/2/3» de la columna P del
        /// Excel es un grupo temático, no un mueble: «Protección personal» quedaba repartida en
        /// tres muebles y el aprendiz miraba en diagonal media pared sin ver bien nada. Cada
        /// parada es un mueble mirado de frente. El grupo del Excel se conserva en
        /// <see cref="Entry.Table"/> para la trazabilidad con el cliente.
        /// </para>
        /// <para>
        /// <b>Por qué el reparto ya no es el del artista</b> (PO, 2026-10-29). Él agrupó por tipo,
        /// y eso dejaba dos mesas que delataban la respuesta: los <b>cinco</b> esenciales de la
        /// hoja (E01–E05) caían en la mesa de la entrada, y el mostrador de la ropa no tenía ni
        /// un elemento que sumara —diez neutros y siete peligros—. El aprendiz aprendía la
        /// ubicación, no el criterio. Ahora <b>cada parada lleva exactamente un esencial, al
        /// menos dos elementos con bono y al menos dos peligros</b>, y ninguna pasa de ~60 % de
        /// tapa ocupada. Lo comprueba <c>Module01DataTests</c>.
        /// </para>
        /// <para>
        /// <b>Dos peligros por parada y no tres</b>, porque desde que los peligros son solo los
        /// doce del Excel ya no hay para más: 12 entre 5 paradas. Los seis que repartían antes
        /// —ropa suelta, mascada, cordones sueltos, jean ancho, audífonos viejos, chanclas de
        /// baño— los habíamos declarado peligro por cuenta propia y volvieron a neutros.
        /// </para>
        /// <para>
        /// <b>Las chanclas y la gorra están en el mostrador de la ropa</b> y no donde las dejó el
        /// reparto anterior: al volver neutros esos seis, el mostrador se quedó con un esencial y
        /// doce neutros, o sea una parada entera donde no se puede fallar. Entran por intercambio
        /// —salen los tenis a la entrada y el tapabocas al centro— y no sumando, porque ese
        /// mueble ya va al límite de ancho. Las chanclas encajan ahí de todas formas: el calzado
        /// neutro (tenis, botas, cordones sueltos) ya estaba en ese mostrador.
        /// </para>
        /// <para>
        /// <b>El mostrador de la ropa va a diez elementos y no a trece.</b> Medido: sus dos
        /// cuerpos dan 4,89 m de tapa útil y los trece pedían 6,96 m. El reparto no deja hueco
        /// vacío, así que el exceso no se ve como desorden sino como filas que se estorban: la
        /// gorra quedaba detrás de la mascada y el pantalón técnico detrás del rompevientos, y
        /// desde el ancla el rayo del puntero no llegaba a ninguno de los dos. La bufanda, la
        /// mascada y las botas se van a la mesa del fondo, que es la que más holgura tiene
        /// (2,83 m pedidos). Lo comprueba <c>ArtEppPlacementSceneTests</c>.
        /// </para>
        /// <para>
        /// Las dos luces son esenciales en la nota desde el 2026-10-29 (columna D de la hoja),
        /// así que cuentan como esencial también para repartir.
        /// </para>
        /// <para>
        /// Lo que cambia de mueble respecto a la pose del artista lo recoloca
        /// <c>Module01EppLayout</c> en un hueco libre de su parada; lo que está en el suelo
        /// delante de su mesa —la mochila, la alforja, la bolsa, las Crocs y el poncho— no se
        /// toca, porque no descansa sobre ningún mueble.
        /// </para>
        /// </summary>
        public static readonly Stop[] Stops =
        {
            // mesa: la de la pared derecha junto a la puerta. Esencial: el casco.
            new Stop("table_entrance", "M1/Stop_Entrance",
                     "helmet_ok", "poncho", "umbrella", "backpack", "pannier", "gloves", "elbow_pads",
                     "sneakers", "headphones", "glasses", "work_goggles", "bell"),
            // otra mesa: la del centro. Esencial: el calzado.
            new Stop("table_middle", "M1/Stop_Middle",
                     "shoes_ok", "cargo_secured", "mittens", "knee_pads", "tote", "face_mask", "snorkel",
                     "flashlight", "sunglasses", "phone_mount"),
            // mesa nueva: la del fondo. Esencial: la luz delantera.
            new Stop("table_back", "M1/Stop_Back",
                     "light_front", "reflective", "cable_lock", "helmet_damaged", "flat_kit",
                     "shower_slippers", "glasses_damaged", "cargo_handheld", "spare_red_light",
                     "airpods", "clothing_loose", "neck_scarf", "boots"),
            // cabinet (1) + cabinetmid 1: el mostrador de la ropa. Esencial: la ropa ceñida.
            new Stop("counter_clothes", "M1/Stop_Clothes",
                     "trousers_fitted", "sandals", "cap", "hiviz_day", "rain_jacket", "work_vest",
                     "loose_laces", "trousers_loose", "folded_jeans", "trousers_tech"),
            // metal table: el banco bajo el logo. Esencial: la luz trasera.
            new Stop("table_tools", "M1/Stop_Tools",
                     "light_rear", "sport_glasses", "damaged_cable_lock", "fabric_gloves", "latex_gloves",
                     "loose_padlock", "work_gloves", "helmet_strap", "multitool", "old_headphones",
                     "drink", "bottle"),
        };

        /// <summary>La parada de un elemento, o <c>null</c> si no está en ninguna.</summary>
        public static string StopOf(string itemId)
        {
            foreach (Stop stop in Stops)
                foreach (string id in stop.ItemIds)
                    if (id == itemId)
                        return stop.Id;
            return null;
        }

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

        /// <summary>Donde el artista dejó la gorra y el poncho (2026-10-28), junto a las texturas.</summary>
        public const string ArtMaterials = "Assets/_Project/Models/CITY/Materials/";

        public readonly struct Entry
        {
            public Entry(string libraryId, string itemId, SafetyItemCategory category,
                         float weight, string table,
                         string model = null, string hazardGroup = null,
                         string exclusiveBonusWith = null, float targetWidth = 0f,
                         string onlyForVehicle = null, string substitutesFor = null,
                         string[] extraModels = null, string inspectModel = null)
            {
                ExtraModels = extraModels ?? new string[0];
                InspectModel = inspectModel;
                LibraryId = libraryId;
                ItemId = itemId;
                Category = category;
                Weight = weight;
                Table = table;
                Model = model;
                HazardGroup = hazardGroup;
                ExclusiveBonusWith = exclusiveBonusWith;
                TargetWidth = targetWidth;
                OnlyForVehicle = onlyForVehicle;
                SubstitutesFor = substitutesFor;
            }

            /// <summary>
            /// El modelo que sale en el pedestal cuando el de la mesa no sirve. Los pantalones
            /// van doblados sobre el mueble y se exhiben abiertos: así se ve si son ceñidos o
            /// anchos, que es lo que la hoja evalúa, y el garaje no necesita la prenda de pie
            /// ocupando suelo (PO, 2026-10-29).
            /// </summary>
            public string InspectModel { get; }

            /// <summary>
            /// Valor de contexto del único vehículo con el que aparece (<c>ebike</c>,
            /// <c>escooter</c>), o <c>null</c> si aparece con todos. Columna «Aplicabilidad».
            /// </summary>
            public string OnlyForVehicle { get; }

            /// <summary>Id del elemento al que sustituye: solo es peligro si se elige en su lugar.</summary>
            public string SubstitutesFor { get; }

            /// <summary>
            /// Otros FBX que son parte del mismo elemento: el portabotella que el artista puso
            /// rodeando el termo. Se pulsan como una sola cosa con el modelo principal.
            /// </summary>
            public string[] ExtraModels { get; }

            /// <summary>
            /// El ID de la hoja: E01, O04, H13… Es la trazabilidad con el cliente. Lo que añadió la PO
            /// y la hoja no tiene lleva P (P01, P02).
            /// </summary>
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
            /// Cuánto debe medir el objeto <b>de ancho, en metros de mundo</b>: su dimensión
            /// horizontal mayor. 0 = dejarlo como venga.
            /// <para>
            /// <b>Solo para lo que el artista no colocó.</b> Desde el 2026-10-27 un elemento que
            /// el artista dejó en <c>Garage.unity</c> se pulsa sobre ese mismo objeto, con su
            /// tamaño (PO: «exactamente igual»; ver <see cref="Module01ArtEpp"/>). Este ancho
            /// queda para el que haya que instanciar desde el FBX porque el arte no lo trae.
            /// </para>
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
        /// Los 32 elementos de la hoja, más los dos que añadió la PO (P01, P02). El orden es el del documento, para poder cotejarlos
        /// de un vistazo.
        /// <para>
        /// <b>E02 y E03 son esenciales con peso 15, como la columna D de la hoja</b> (PO,
        /// 2026-10-29). Estuvieron como bono de 1 por la decisión de «hoy es día claro»
        /// (2026-10-22), que es lo que dice la hoja para ese escenario; la PO cerró que la nota
        /// sea la de la columna D sin matizar por escenario, así que las dos luces vuelven a
        /// esencial y el pool aplicable pasa de 65 a 95. <b>Consecuencia que hay que saber:</b>
        /// olvidar las dos luces son dos faltas esenciales, y eso solo manda a repetir la
        /// sección de EPP.
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
                      targetWidth: 0.36f,
                      inspectModel: Essentials + "Slim_fit_pants.fbx"),
                      // Doblado en el mostrador, abierto en el pedestal. El de pie era P04 y
                      // valía cero: la misma prenda contada dos veces (PO, 2026-10-29).  // ya llega bien; igual que jean_ajustado_doblado (0,317)

            // ---- Esenciales condicionales (E02, E03) -------------------------------------
            // Peso 15 cada una, como la columna D de «Sistema de puntuación». Estuvieron en 1
            // como bono por la decisión de «hoy es día claro» (2026-10-22); la PO cerró el
            // 2026-10-29 que la nota sea la de la hoja, así que vuelven a esencial. El pool
            // aplicable pasa de 65 a 95, y olvidar una luz deja de costar 1 punto.
            new Entry("E02", "light_front", Core, 15f, Mesa2,
                      model: Models + "luz grontal.fbx",
                      targetWidth: 0.10f),
                      // `luz grontal` en vez de `lintarna`: viene a escala real -0,03 x 0,05 x 0,10-
                      // y trae un hijo `soporte`, que es justo lo que pide la direccion artistica de
                      // E02 en la hoja («soporte de manillar visible»). `lintarna` medía 1,17 m y no
                      // tiene soporte. Charlie, 2026-10-22.
            new Entry("E03", "light_rear", Core, 15f, Mesa2,
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
                      // 1,61 frente a 1,33): con 0,36 de ancho mide 0,58 de alto, lo del viejo.
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
                      model: Essentials + "mochila.fbx", targetWidth: 0.32f,
                      onlyForVehicle: "escooter"),
                      // Mochila de diario real, ~32 cm de ancho. La textura trae un logo que arte
                      // cambia después (PO, 2026-10-26): no afecta al modelo.
                      // Solo con el scooter: la hoja la da para «E-scooter o bicicleta sin
                      // portaequipajes», y nuestra e-bike lleva la alforja, o sea portaequipajes.
            new Entry("O11", "cargo_secured", Opt, 2f, Mesa3,
                      model: Essentials + "candado.fbx", targetWidth: 0.22f),
                      // La hoja pide «Candado con soporte o ubicación de transporte segura»:
                      // un candado, no una guaya. `guaya candado` es una guaya enrollada y se
                      // queda fuera, igual que `guayya dañado`, su pareja deteriorada: ninguna
                      // de las dos tiene fila en el Excel. Desde el 2026-10-27 es el candado con
                      // textura de la carpeta de esenciales, al ancho que tenía el viejo de la
                      // escena (0,22), que se apaga.
            new Entry("O12", "flat_kit", Opt, 2f, Mesa3,
                      model: Essentials + "kit compacto.fbx", targetWidth: 0.18f,
                      onlyForVehicle: "ebike"),
                      // El kit que el artista entregó el 2026-10-27 («faltan 10»): estuche con los
                      // parches a la vista, que es lo que pide la hoja («contenido identificable»).
                      // Sustituye a `cartuchera_low`, que queda de decorado. Solo con la e-bike:
                      // «E-bike propia; trayectos largos o con poca asistencia».
            new Entry("O13", "multitool", Opt, 1f, Mesa3,
                      model: Essentials + "herramienta.fbx", targetWidth: 0.18f),
                      // La multiherramienta que entregó el artista el 2026-10-28, junto al kit. La
                      // `Tool_Tajima` de antes se queda en el banco de herramientas, como las pinzas.
            new Entry("O14", "phone_mount", Opt, 1f, Mesa2,
                      model: Essentials + "soporte celular.fbx", targetWidth: 0.17f),
            new Entry("O15", "bell", Opt, 2f, Mesa2,
                      model: Essentials + "timbre bici.fbx", targetWidth: 0.14f),
                      // Con los dos vehículos: la hoja lo premia si el vehículo no tiene timbre, y
                      // ni la e-bike ni el scooter traen uno modelado.
            new Entry("O16", "rain_jacket", Opt, 2f, Mesa3,
                      model: Essentials + "rompe vientos_capucha.fbx",
                      targetWidth: 0.40f),  // chaqueta doblada; hoy mide 0,05, es la unica demasiado PEQUENA
            new Entry("O18", "bottle", Opt, 1f, Mesa3,
                      model: Essentials + "termo.fbx", targetWidth: 0.09f,
                      extraModels: new[] { Essentials + "soporte botella bici.fbx" }),
                      // Termo de bici con tapa, en su portabotella (2026-10-28): lo que pide la
                      // hoja, «botella sellada en portabotella compatible».
            new Entry("O19", "pannier", Opt, 3f, Mesa3,
                      model: Essentials + "alforja.fbx", targetWidth: 0.71f,
                      onlyForVehicle: "ebike"),
                      // «Solo se muestra para e-bike con portaequipajes compatible».

            // ---- Peligrosos ------------------------------------------------------------
            new Entry("H01", "helmet_strap", Bad, 30f, Mesa1,
                      model: Models + "df_g_helmet_01.fbx", hazardGroup: "helmet_integrity",
                      targetWidth: 0.29f),
                      // No hay casco con la correa cortada. El más parecido es este casco retro, que
                      // no trae correa de barbilla: «correa ... faltante», dice la hoja. Vive en el
                      // decorado del garaje, fuera de toda parada; Module01ArtEpp.Moves lo trae al
                      // mostrador de la ropa (2026-10-28). Pendiente de que el artista haga el bueno.
            new Entry("H02", "helmet_damaged", Bad, 30f, Mesa1,
                      model: Models + "casco 1.fbx", hazardGroup: "helmet_integrity"),
            new Entry("H03", "headphones", Bad, 20f, Mesa2,
                      model: Essentials + "diadema.fbx", hazardGroup: "auditory_occlusion",
                      targetWidth: 0.24f),
                      // Los audífonos de diadema que el artista entregó el 2026-10-27, tipo AirPods
                      // Max como pide la hoja. Los `audifonos` de antes quedan de decorado.
            new Entry("H13", "airpods", Bad, 20f, Mesa2,
                      model: Essentials + "audifonos ipod.fbx", hazardGroup: "auditory_occlusion",
                      targetWidth: 0.11f),
            new Entry("H04", "sandals", Bad, 15f, Mesa1,
                      model: Essentials + "Crocs.fbx", hazardGroup: "footwear_control",
                      targetWidth: 0.34f),
                      // Las Crocs que entregó el artista el 2026-10-28: la referencia de la hoja es
                      // una Crocs Mellow Slide. Las `Slippers` de antes son P30, en el mismo grupo.
            new Entry("H06", "poncho", Bad, 20f, Mesa3,
                      model: ArtMaterials + "poncho h.fbx", hazardGroup: "loose_rainwear",
                      targetWidth: 1.00f),
            new Entry("H07", "glasses_damaged", Bad, 20f, Mesa1,
                      model: Module01DamagedGlasses.PrefabPath, hazardGroup: "visual_obstruction",
                      targetWidth: 0.17f),  // el ancho de las de O07
                      // Las gafas de seguridad de O07 con el material rayado (artista, 2026-10-28:
                      // «es un material»). El artista no las colocó: van a un hueco libre de su mesa.
            new Entry("H08", "mittens", Bad, 15f, Mesa2,
                      model: Essentials + "guante voluminoso.fbx", hazardGroup: "control_access",
                      targetWidth: 0.35f),
            new Entry("H09", "tote", Bad, 15f, Mesa3,
                      model: Models + "bolsa.fbx", hazardGroup: "unstable_carry", targetWidth: 0.22f),
                      // El artista la dejó el 2026-10-28 (c71f243) en el suelo delante de la mesa
                      // de la entrada, que es su parada. Hay otra copia idéntica en Materials/; se
                      // usa la de MODELADOS 3d, que es la que conserva el GUID que usa Garage.
            new Entry("H10", "umbrella", Bad, 30f, Mesa3,
                      model: Models + "sombrilla.fbx", hazardGroup: "one_hand_wind",
                      targetWidth: 0.67f),
            new Entry("H11", "drink", Bad, 20f, Mesa2,
                      model: Models + "Cafe.fbx", hazardGroup: "one_hand_drink",
                      targetWidth: 0.09f),
            new Entry("H12", "cap", Bad, 20f, Mesa1,
                      model: ArtMaterials + "gorra_.fbx", targetWidth: 0.30f,
                      hazardGroup: "helmet_substitution", substitutesFor: "helmet_ok"),
                      // «Peligrosa condicional»: «si se elige en lugar del casco». Junto al casco
                      // no resta.

            // ---- Neutral ---------------------------------------------------------------
            new Entry("N01", "cargo_handheld", Opt, 0f, Mesa2,
                      model: Models + "MobilePhone_01.fbx"),

            // ---- Añadidos por la PO: no están en la hoja ----------------------------------
            // Prefijo P y no O: un O inventado se leería como una fila del Excel del cliente.
            // «Recomendada condicional» como los O de la hoja: bono si se eligen, nunca restan.
            // El peso lo puse yo, igual que los guantes (O04, +2): protección de un punto de
            // contacto en una caída. Pendiente de que el cliente lo confirme.
            new Entry("P01", "elbow_pads", Opt, 2f, Mesa1,
                      model: Models + "codera.fbx"),
            new Entry("P02", "knee_pads", Opt, 2f, Mesa1,
                      model: Models + "rodillera.fbx"),

            // ---- Lo demás que modeló el artista (PO, 2026-10-28) --------------------------
            // «Todos los que ha hecho el artista son interactuables». Lo que la hoja no evalúa
            // es NEUTRAL, como el teléfono (N01): se examina y se elige, pero no suma ni resta y
            // en el reporte sale «No necesario». No se inventa una nota que el cliente no puso.
            new Entry("P03", "flashlight", Opt, 0f, Mesa2, model: Essentials + "lintarna.fbx"),
            new Entry("P05", "snorkel", Opt, 0f, Mesa1, model: Models + "snorquel.fbx"),
            new Entry("P06", "sport_glasses", Opt, 0f, Mesa1, model: Essentials + "gafas_deportivas.fbx"),
            new Entry("P07", "work_vest", Opt, 0f, Mesa2, model: Models + "chaleco refectivo.fbx"),
            new Entry("P08", "work_goggles", Opt, 0f, Mesa1, model: Models + "gafas.fbx"),
            new Entry("P09", "spare_red_light", Opt, 0f, Mesa2, model: Models + "luz roja 1.fbx"),
            new Entry("P10", "loose_padlock", Opt, 0f, Mesa3, model: Models + "candado.fbx"),
            new Entry("P11", "cable_lock", Opt, 0f, Mesa3, model: Models + "guaya candado.fbx"),
            new Entry("P12", "damaged_cable_lock", Opt, 0f, Mesa3, model: Models + "guayya dañado.fbx"),
            new Entry("P13", "work_gloves", Opt, 0f, Mesa1, model: Models + "guante.fbx"),
            new Entry("P14", "latex_gloves", Opt, 0f, Mesa1,
                      model: Models + "Robbers_T3ANI210_LatexGloves_Abdilla.fbx"),
            new Entry("P15", "fabric_gloves", Opt, 0f, Mesa1, model: Models + "bloves 02.fbx"),
            new Entry("P16", "face_mask", Opt, 0f, Mesa1, model: Models + "tapabocas.fbx"),
            new Entry("P17", "folded_jeans", Opt, 0f, Mesa3, model: Models + "jean_ajustado_doblado 1.fbx",
                      inspectModel: Models + "jean ajustado.fbx"),
            new Entry("P18", "trousers_tech", Opt, 0f, Mesa3, model: Models + "pantalon deportivo doblado.fbx",
                      inspectModel: Models + "pant low.fbx"),
                      // Emparejado por forma: el `pant low` de pie no tiene doblado propio y el
                      // deportivo doblado no tenía abierto. Confirmar con el artista.
            new Entry("P21", "sneakers", Opt, 0f, Mesa1, model: Models + "tenis_deportivos.fbx"),
            new Entry("P22", "boots", Opt, 0f, Mesa1, model: Models + "botas.fbx"),
                      // Vienen dentro del prefab de decoración del garaje, no sueltas: se
                      // reconocen por su malla (Module01SceneEntities).
            // P23 era el estuche (`cartuchera_low`): desde que el artista trajo el kit compacto y la
            // multiherramienta a la mesa del centro (2026-10-28), el estuche y la Tool_Tajima son
            // del banco de herramientas, como las pinzas, y no se traen.

            // PELIGROS: solo los del Excel. El 2026-10-29 la PO cerró que «los incorrectos sean
            // los mismos que el Excel»: los doce H de la hoja, ni uno más. Estos seis los
            // habíamos declarado peligro por cuenta propia —ropa suelta, audífonos viejos,
            // chanclas— con pesos que yo mismo propuse. Vuelven a NEUTRAL: se examinan y se
            // eligen, no suman ni restan, y en el reporte salen «No necesario», igual que el
            // teléfono. La hoja del cliente es la que puntúa.
            // −15 y un solo grupo, así que llevarse varias prendas sueltas descuenta una vez, como
            // los grupos de la hoja. El peso lo propuse yo: el mismo que las chanclas (H04).
            new Entry("P24", "clothing_loose", Opt, 0f, Mesa1, model: Models + "bufanda 01.fbx"),
            new Entry("P25", "neck_scarf", Opt, 0f, Mesa1, model: Models + "mascada.fbx"),
            new Entry("P26", "trousers_loose", Opt, 0f, Mesa3, model: Models + "jean ancho doblado.fbx",
                      inspectModel: Models + "pantalon ancho.fbx"),
                      // El ancho se lee abierto o no se lee: doblado parece uno ceñido más.
            new Entry("P28", "loose_laces", Opt, 0f, Mesa1, model: Models + "tenis_cordones_sueltos.fbx"),
            // Los audífonos de antes son lo mismo que H03: peligro en su mismo grupo, que descuenta
            // una sola vez aunque se lleve los dos. Neutrales enseñarían que se puede rodar con ellos.
            new Entry("P29", "old_headphones", Opt, 0f, Mesa2, model: Models + "audifonos.fbx"),
            // Las chanclas de antes: H04 pasó a las Crocs (2026-10-28). Mismo peligro y mismo grupo,
            // así que llevarse las dos descuenta una vez.
            new Entry("P30", "shower_slippers", Opt, 0f, Mesa1, model: Models + "Slippers.fbx"),
        };
    }
}
