using System.Collections.Generic;
using UnityEngine;

namespace RideSafe.Module01
{
    /// <summary>Las zonas de protección personal en el orden del recorrido.</summary>
    [CreateAssetMenu(fileName = "SO_Module01Catalog", menuName = "RideSafe/Module 01/Catalog")]
    public class Module01CatalogSO : ScriptableObject
    {
        [SerializeField] private List<ZoneSO> _zones = new List<ZoneSO>();

        public IReadOnlyList<ZoneSO> Zones => _zones;

        public IEnumerable<SafetyItemSO> AllItems
        {
            get
            {
                for (int z = 0; z < _zones.Count; z++)
                {
                    ZoneSO zone = _zones[z];
                    if (zone == null)
                        continue;
                    for (int i = 0; i < zone.Items.Count; i++)
                    {
                        if (zone.Items[i] != null)
                            yield return zone.Items[i];
                    }
                }
            }
        }

        public bool TryGetItem(string itemId, out SafetyItemSO item)
        {
            item = null;
            if (string.IsNullOrWhiteSpace(itemId))
                return false;
            string wanted = itemId.Trim();
            foreach (SafetyItemSO candidate in AllItems)
            {
                if (candidate.ItemId == wanted)
                {
                    item = candidate;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Problemas que harían al módulo comportarse mal en silencio. Un test los
        /// convierte en fallo antes de que la escena llegue a Play.
        /// </summary>
        public List<string> Validate()
        {
            List<string> problems = new List<string>();
            HashSet<string> seen = new HashSet<string>();
            foreach (SafetyItemSO item in AllItems)
            {
                if (string.IsNullOrWhiteSpace(item.ItemId))
                {
                    problems.Add($"Elemento sin id en el catalogo ({item.name}).");
                    continue;
                }
                if (!seen.Add(item.ItemId))
                    problems.Add($"Id duplicado: {item.ItemId}.");
                if (string.IsNullOrWhiteSpace(item.NameKey))
                    problems.Add($"Elemento {item.ItemId} sin clave de localizacion.");
            }
            return problems;
        }

        /// <summary>Solo para tests.</summary>
        public void ConfigureForTests(IEnumerable<ZoneSO> zones) => _zones = new List<ZoneSO>(zones);
    }
}
