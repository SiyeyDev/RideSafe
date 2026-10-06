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

        public string ItemId => string.IsNullOrWhiteSpace(_itemId) ? string.Empty : _itemId.Trim();
        public string NameKey => _nameKey;
        public string RiskKey => _riskKey;
        public SafetyItemCategory Category => _category;
        public GameObject DisplayPrefab => _displayPrefab;

        /// <summary>Solo para tests: evita tener que crear assets en disco.</summary>
        public void ConfigureForTests(string itemId, string nameKey, string riskKey, SafetyItemCategory category)
        {
            _itemId = itemId;
            _nameKey = nameKey;
            _riskKey = riskKey;
            _category = category;
        }
    }
}
