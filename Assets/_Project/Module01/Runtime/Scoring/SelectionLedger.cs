using System.Collections.Generic;

namespace RideSafe.Module01
{
    /// <summary>
    /// Lo que el aprendiz lleva elegido. Es mutable hasta que envía la sección:
    /// quitar un elemento aquí lo deja fuera del reporte, como exige la narrativa.
    /// </summary>
    public class SelectionLedger
    {
        private readonly List<string> _selected = new List<string>();

        public IReadOnlyList<string> Selected => _selected;

        public void Add(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return;
            string id = itemId.Trim();
            if (!_selected.Contains(id))
                _selected.Add(id);
        }

        public void Remove(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return;
            _selected.Remove(itemId.Trim());
        }

        public bool Contains(string itemId) =>
            !string.IsNullOrWhiteSpace(itemId) && _selected.Contains(itemId.Trim());

        public void Clear() => _selected.Clear();
    }
}
