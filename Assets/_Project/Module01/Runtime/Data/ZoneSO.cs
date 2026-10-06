using System.Collections.Generic;
using UnityEngine;

namespace RideSafe.Module01
{
    /// <summary>Una estación del recorrido con los elementos que presenta.</summary>
    [CreateAssetMenu(fileName = "SO_Zone", menuName = "RideSafe/Module 01/Zone")]
    public class ZoneSO : ScriptableObject
    {
        [Tooltip("Id estable. Coincide con el nombre del ancla en la escena.")]
        [SerializeField] private string _zoneId;

        [Tooltip("Clave de localización del título de la zona.")]
        [SerializeField] private string _titleKey;

        [SerializeField] private List<SafetyItemSO> _items = new List<SafetyItemSO>();

        public string ZoneId => string.IsNullOrWhiteSpace(_zoneId) ? string.Empty : _zoneId.Trim();
        public string TitleKey => _titleKey;
        public IReadOnlyList<SafetyItemSO> Items => _items;

        /// <summary>Solo para tests.</summary>
        public void ConfigureForTests(string zoneId, string titleKey, IEnumerable<SafetyItemSO> items)
        {
            _zoneId = zoneId;
            _titleKey = titleKey;
            _items = new List<SafetyItemSO>(items);
        }
    }
}
