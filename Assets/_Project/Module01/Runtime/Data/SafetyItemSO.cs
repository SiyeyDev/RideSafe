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

        public string ItemId => string.IsNullOrWhiteSpace(_itemId) ? string.Empty : _itemId.Trim();
        public string NameKey => _nameKey;
        public string RiskKey => _riskKey;
        public SafetyItemCategory Category => _category;
        public GameObject DisplayPrefab => _displayPrefab;

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
        public void ConfigureForTests(string itemId, string nameKey, string riskKey, SafetyItemCategory category)
        {
            _itemId = itemId;
            _nameKey = nameKey;
            _riskKey = riskKey;
            _category = category;
        }

        /// <summary>Solo para tests: marca el elemento como deteriorado.</summary>
        public void MarkDeterioratedForTests() => _deteriorated = true;
    }
}
