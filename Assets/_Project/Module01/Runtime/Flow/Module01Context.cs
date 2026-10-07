using System.Collections.Generic;

namespace RideSafe.Module01
{
    /// <summary>
    /// Valores que deciden qué secuencia corre. Se asigna a
    /// <c>TaskSequenceService.ContextLookup</c>, y <c>ContextRequirement</c> los
    /// compara ya normalizados a minúsculas.
    /// <para>
    /// Una clave no publicada devuelve cadena vacía, nunca null: así un requisito
    /// <c>vehicle == ebike</c> simplemente no se cumple y la secuencia no corre, en
    /// vez de reventar o de cargar la equivocada.
    /// </para>
    /// </summary>
    public class Module01Context
    {
        public const string VehicleKey = "vehicle";
        public const string LanguageKey = "language";
        public const string JurisdictionKey = "jurisdiction";

        private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

        public void Set(string key, string value)
        {
            string k = Normalize(key);
            if (k.Length == 0)
                return;
            _values[k] = Normalize(value);
        }

        public string Get(string key) => Lookup(key);

        /// <summary>Firma apta para <c>TaskSequenceService.ContextLookup</c>.</summary>
        public string Lookup(string key)
        {
            string k = Normalize(key);
            return _values.TryGetValue(k, out string value) ? value : string.Empty;
        }

        private static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}
