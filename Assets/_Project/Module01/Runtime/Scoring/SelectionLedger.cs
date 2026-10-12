using System;
using System.Collections.Generic;

namespace RideSafe.Module01
{
    /// <summary>
    /// Lo que el aprendiz lleva elegido. Es mutable hasta que envía la sección:
    /// quitar un elemento aquí lo deja fuera del reporte, como exige la narrativa.
    /// <para>
    /// Avisa de cada cambio para que el mundo pueda reaccionar: un elemento que entra en el
    /// libro desaparece de la mesa —el aprendiz ya lo lleva— y vuelve si lo quita. Los avisos
    /// salen <b>solo cuando el contenido cambia de verdad</b>, no en cada llamada: añadir dos
    /// veces el mismo id lo esconderia dos veces y quitarlo una lo dejaria mal.
    /// </para>
    /// </summary>
    public class SelectionLedger
    {
        private readonly List<string> _selected = new List<string>();

        public IReadOnlyList<string> Selected => _selected;

        /// <summary>Un elemento acaba de entrar. Llega con el id ya recortado.</summary>
        public event Action<string> Added;

        /// <summary>Un elemento que estaba acaba de salir.</summary>
        public event Action<string> Removed;

        /// <summary>El libro se vació de golpe, al cambiar de sección.</summary>
        public event Action Cleared;

        public void Add(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return;
            string id = itemId.Trim();
            if (_selected.Contains(id))
                return;
            _selected.Add(id);
            Added?.Invoke(id);
        }

        public void Remove(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return;
            string id = itemId.Trim();
            if (!_selected.Remove(id))
                return;
            Removed?.Invoke(id);
        }

        public bool Contains(string itemId) =>
            !string.IsNullOrWhiteSpace(itemId) && _selected.Contains(itemId.Trim());

        public void Clear()
        {
            if (_selected.Count == 0)
                return;
            _selected.Clear();
            Cleared?.Invoke();
        }
    }
}
