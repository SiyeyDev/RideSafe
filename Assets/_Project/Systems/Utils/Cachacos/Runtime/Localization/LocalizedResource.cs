using UnityEngine;

namespace Cachacos
{
    /// <summary>
    /// Carga de <c>Resources</c> el asset que corresponde al idioma activo.
    /// <para>
    /// Genérico a propósito. Hoy lo usa la locución de los elementos
    /// (<see cref="AudioClip"/>), y los videos del módulo van a tener exactamente el mismo
    /// problema en cuanto lleguen en dos idiomas: hoy son dos <c>VideoClip</c> sueltos
    /// apuntando al mismo fichero. Lo que cambia entre un caso y otro es el tipo, no la regla.
    /// </para>
    /// <para>
    /// La ruta la fija la herramienta que genera los audios, que escribe en
    /// <c>Resources/Sounds/&lt;categoría&gt;/&lt;idioma&gt;/&lt;término sin categoría&gt;</c>. El idioma es
    /// el <b>nombre</b> con el que lo llama I2 —«Español», «English»—, que es como se llaman
    /// las carpetas.
    /// </para>
    /// </summary>
    public static class LocalizedResource
    {
        /// <summary>
        /// Ruta dentro de <c>Resources</c>, o null si falta el idioma o la clave. Null y no una
        /// ruta a medias: <c>Resources.Load</c> con «Sounds/M1//clave» no falla, simplemente no
        /// encuentra nada, y eso se diagnostica fatal.
        /// </summary>
        public static string PathFor(string folder, string language, string key)
        {
            if (string.IsNullOrWhiteSpace(language) || string.IsNullOrWhiteSpace(key))
                return null;

            // El fichero se llama como el término SIN su categoría: la herramienta hace
            // term.Replace("<categoria>/", "") antes de escribirlo.
            int slash = key.LastIndexOf('/');
            string name = slash >= 0 ? key.Substring(slash + 1) : key;
            if (name.Length == 0)
                return null;

            string root = (folder ?? string.Empty).TrimEnd('/');
            return root.Length == 0 ? language + "/" + name : root + "/" + language + "/" + name;
        }

        /// <summary>
        /// El asset del idioma activo, o null si no existe. Null es un caso normal —todavía no
        /// se ha grabado esa locución— y el que llama se queda callado, no revienta.
        /// </summary>
        public static T Load<T>(string folder, string language, string key) where T : Object
        {
            string path = PathFor(folder, language, key);
            return string.IsNullOrEmpty(path) ? null : Resources.Load<T>(path);
        }
    }
}
