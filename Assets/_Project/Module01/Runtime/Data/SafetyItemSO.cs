using UnityEngine;

namespace RideSafe.Module01
{
    /// <summary>Un elemento del tablero de protección personal.</summary>
    [CreateAssetMenu(fileName = "SO_SafetyItem", menuName = "RideSafe/Module 01/Safety Item")]
    public class SafetyItemSO : ScriptableObject
    {
        [Tooltip("Id estable, referenciado por las secuencias y el reporte.")]
        [SerializeField] private string _itemId;

        [Tooltip("Clave de localización del nombre. Nunca texto literal.")]
        [SerializeField] private string _nameKey;

        [Tooltip("Clave de localización del mecanismo de riesgo o de la condición.")]
        [SerializeField] private string _riskKey;

        [SerializeField] private SafetyItemCategory _category = SafetyItemCategory.Optional;

        [Tooltip("Objeto que sale al frente en el inspector. Greybox mientras llega 3D.")]
        [SerializeField] private GameObject _displayPrefab;

        [Tooltip("El elemento está deteriorado, no es que sea el objeto equivocado.")]
        [SerializeField] private bool _deteriorated;

        [Tooltip("Locución breve al inspeccionarlo: «Casco deteriorado, ¿deseas añadirlo?». Vacío = en silencio.")]
        [SerializeField] private AudioClip _promptClip;

        [Tooltip("Columna «Peso / puntos» de la biblioteca del cliente. En lo esencial es su peso " +
                 "dentro del pool; en lo peligroso son los puntos que descuenta. 1 = sin peso asignado.")]
        [Min(0f)]
        [SerializeField] private float _weight = 1f;

        [Tooltip("Columna «Grupo / regla» de la biblioteca. Dos peligros del mismo grupo " +
                 "descuentan una sola vez, el mayor de los dos. Vacío = grupo propio.")]
        [SerializeField] private string _hazardGroup;

        [Tooltip("Id del recomendado que anula el bono de este. La hoja solo lo declara entre la " +
                 "prenda fluorescente de día y el chaleco reflectivo de noche. Vacío = ninguno.")]
        [SerializeField] private string _exclusiveBonusWith;

        [Tooltip("Condición del escenario que hace aplicable el bono, p. ej. «rain» o «night». " +
                 "Vacío = siempre aplicable. Columna «Aplicabilidad» de la biblioteca.")]
        [SerializeField] private string _bonusCondition;

        [Tooltip("Valor de contexto del único vehículo con el que aparece —«ebike», «escooter»—. " +
                 "Vacío = con todos. Columna «Aplicabilidad» de la biblioteca.")]
        [SerializeField] private string _onlyForVehicle;

        [Tooltip("Id del elemento al que sustituye: solo es peligro si se elige en lugar de ese. " +
                 "La gorra (H12, «Peligrosa condicional») sustituye al casco. Vacío = siempre cuenta.")]
        [SerializeField] private string _substitutesFor;

        [Tooltip("Lo que se enseña en el pedestal al pulsarlo, si es distinto de lo que hay en la " +
                 "mesa. Los pantalones van doblados sobre el mueble —así caben y se ven ordenados— " +
                 "y se exhiben abiertos, que es como se lee si son ceñidos o anchos. Vacío = se " +
                 "copia lo de la mesa.")]
        [SerializeField] private GameObject _inspectPrefab;

        public string ItemId => string.IsNullOrWhiteSpace(_itemId) ? string.Empty : _itemId.Trim();
        public string NameKey => _nameKey;
        public string RiskKey => _riskKey;
        public SafetyItemCategory Category => _category;
        public GameObject DisplayPrefab => _displayPrefab;

        /// <summary>
        /// El modelo para el pedestal cuando no vale el de la mesa (PO, 2026-10-29). Hasta
        /// ahora el garaje tenía el pantalón doblado <b>y</b> el mismo pantalón de pie como dos
        /// elementos distintos: el doblado valía 5 puntos y el de pie, cero. Era la misma prenda
        /// contada dos veces, y los de pie ocupaban el suelo, que el cliente no quiere.
        /// </summary>
        public GameObject InspectPrefab => _inspectPrefab;

        /// <summary>
        /// La columna «Peso / puntos» de
        /// <c>RideSafe_VR_equipment_image_reference_library_2026-10-19.xlsx</c>, que es la hoja
        /// que entregó el cliente.
        /// <para>
        /// Significa tres cosas distintas según la categoría, igual que en la hoja, porque allí
        /// también es una sola columna. En lo <b>esencial</b> es el peso dentro del pool —casco
        /// 40, calzado 20, ropa ceñida 5— y lo que cuenta es la proporción, porque el bloque se
        /// normaliza a 70. En lo <b>peligroso</b> son los puntos que descuenta de los 30 del
        /// bloque de riesgo, en valor absoluto: el casco roto 30, los audífonos 20, las chanclas
        /// 15. En lo <b>recomendado</b> son los puntos que suma al bono de preparación: el
        /// chaleco reflectivo 3, los guantes 2, la multiherramienta 1.
        /// </para>
        /// <para>
        /// Un peso de 0 deja el elemento fuera de la nota sin sacarlo del catálogo: es el caso
        /// del teléfono, que la hoja declara neutral («no cambia el resultado»). El valor por
        /// defecto es 1, que es lo que vale mientras un elemento no tenga peso de la biblioteca.
        /// </para>
        /// </summary>
        public float Weight => _weight < 0f ? 0f : _weight;

        /// <summary>
        /// La columna «Grupo / regla» de la biblioteca. Dos peligros del mismo grupo descuentan
        /// <b>una sola vez</b>, el mayor de los dos: la hoja dice «usa la penalización máxima
        /// por grupo».
        /// <para>
        /// Es lo que impide que llevarse los dos cascos rotos —<c>helmet_integrity</c>, 30 cada
        /// uno— descuente 60 por el mismo error cometido dos veces. Lo mismo con los audífonos
        /// y los AirPods, que son los dos <c>auditory_occlusion</c>.
        /// </para>
        /// <para>Vacío = el elemento es su propio grupo, que es el caso de los nueve restantes.</para>
        /// </summary>
        public string HazardGroup =>
            string.IsNullOrWhiteSpace(_hazardGroup) ? ItemId : _hazardGroup.Trim();

        /// <summary>
        /// El recomendado cuyo bono anula el de este, de la columna «Regla para programador»
        /// (<c>exclusive_bonus_with</c>). Vacío si no hay ninguno.
        /// <para>
        /// La hoja solo lo declara en un par: la prenda fluorescente de día (O01, +2) y el
        /// chaleco retroreflectivo de noche (O02, +3). Son la misma precaución para dos
        /// escenarios distintos, así que llevarse las dos no se cobra dos veces; cuenta la mejor.
        /// </para>
        /// <para>
        /// <b>Ojo:</b> esto no es lo mismo que <see cref="HazardGroup"/>. Hay recomendados que
        /// comparten grupo en la hoja —las gafas transparentes y las de sol son las dos
        /// <c>eye_protection</c>— y aun así <b>no</b> están declarados excluyentes, así que sus
        /// bonos suman. Por eso son dos campos y no uno.
        /// </para>
        /// </summary>
        public string ExclusiveBonusWith =>
            string.IsNullOrWhiteSpace(_exclusiveBonusWith) ? string.Empty : _exclusiveBonusWith.Trim();

        /// <summary>
        /// La condición de escenario que hace aplicable el bono: la hoja dice «suma de bonos
        /// <b>aplicables</b> seleccionados», y cada recomendado trae la suya en la columna
        /// «Aplicabilidad» —la chaqueta impermeable solo con lluvia, las gafas de sol solo en día
        /// soleado—. Vacío = aplicable siempre.
        /// <para>
        /// <b>Pendiente:</b> el módulo todavía no tiene modelo de escenario —solo sabe que es día
        /// claro— así que hoy esto está vacío en todo el catálogo y todos los bonos aplican. El
        /// mecanismo queda puesto para cuando se decidan las condiciones.
        /// </para>
        /// </summary>
        public string BonusCondition =>
            string.IsNullOrWhiteSpace(_bonusCondition) ? string.Empty : _bonusCondition.Trim();

        /// <summary>
        /// Distingue «esto está roto» de «esto no es lo que toca». Las dos cosas son
        /// <see cref="SafetyItemCategory.Inappropriate"/> y las dos restan igual, pero en el
        /// reporte son mensajes distintos: el casco dañado enseña a revisar el estado del
        /// equipo, y el móvil en la mano enseña a no llevar carga suelta. La reunión pidió
        /// las dos marcas por separado en la leyenda.
        /// </summary>
        public bool IsDeteriorated => _deteriorated;

        /// <summary>
        /// Lo que se oye al sacar el elemento al pedestal, si lo hay.
        /// <para>
        /// Hoy no hay ni un clip de audio en el proyecto, así que esto está siempre vacío y el
        /// inspector se queda callado sin quejarse. El enchufe existe para que, cuando lleguen
        /// las locuciones, baste con arrastrarlas al asset: ni una línea de código.
        /// </para>
        /// </summary>
        public AudioClip PromptClip => _promptClip;

        /// <summary>Solo para tests: evita tener que crear assets en disco.</summary>
        /// <summary>Solo para tests y para el generador de datos.</summary>
        public void ConfigureDisplayForTests(GameObject prefab) => _displayPrefab = prefab;

        public void ConfigureForTests(string itemId, string nameKey, string riskKey, SafetyItemCategory category)
        {
            _itemId = itemId;
            _nameKey = nameKey;
            _riskKey = riskKey;
            _category = category;
        }

        /// <summary>Solo para tests: marca el elemento como deteriorado.</summary>
        public void MarkDeterioratedForTests() => _deteriorated = true;

        /// <summary>Solo para tests y para el generador de datos: fija el peso de la biblioteca.</summary>
        public void ConfigureWeightForTests(float weight, string hazardGroup = null)
        {
            _weight = weight < 0f ? 0f : weight;
            _hazardGroup = hazardGroup;
        }

        /// <summary>Solo para tests y para el generador: las dos reglas del bono.</summary>
        public void ConfigureBonusForTests(string exclusiveWith = null, string condition = null)
        {
            _exclusiveBonusWith = exclusiveWith;
            _bonusCondition = condition;
        }

        /// <summary>
        /// El único vehículo con el que aparece, o vacío si aparece con todos. La hoja lo pide
        /// para cuatro recomendados: la alforja y el kit de pinchazos solo con la e-bike, la mochila
        /// solo con el scooter (PO, 2026-10-28).
        /// </summary>
        public string OnlyForVehicle =>
            string.IsNullOrWhiteSpace(_onlyForVehicle) ? string.Empty : _onlyForVehicle.Trim().ToLowerInvariant();

        /// <summary>¿Aparece con este vehículo? Sin vehículo elegido, aparece todo.</summary>
        public bool AppliesTo(string vehicleContextValue) =>
            OnlyForVehicle.Length == 0 || string.IsNullOrWhiteSpace(vehicleContextValue)
            || OnlyForVehicle == vehicleContextValue.Trim().ToLowerInvariant();

        /// <summary>
        /// El elemento al que sustituye, o vacío. Uno que sustituye a otro solo es peligro si se
        /// elige <b>en lugar</b> de él: la gorra junto al casco no resta (hoja, H12).
        /// </summary>
        public string SubstitutesFor =>
            string.IsNullOrWhiteSpace(_substitutesFor) ? string.Empty : _substitutesFor.Trim();

        /// <summary>Solo para tests y para el generador: con qué vehículo aparece y a qué sustituye.</summary>
        public void ConfigureRulesForTests(string onlyForVehicle = null, string substitutesFor = null)
        {
            _onlyForVehicle = onlyForVehicle;
            _substitutesFor = substitutesFor;
        }
    }
}
