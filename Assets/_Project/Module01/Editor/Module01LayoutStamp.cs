using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Dónde dejó el layout cada elemento la última vez que corrió, para no volver a deshacer
    /// lo que una persona haya colocado a mano.
    /// <para>
    /// <b>El problema que resuelve</b> (PO, 2026-10-29). La PO repartió los EPP a mano, usando
    /// el estante alto de la mesa de la entrada y el entrepaño del banco de herramientas. Yo
    /// ejecuté <c>LayoutOpenScene</c> tres veces encima y se lo llevé por delante las tres: se
    /// perdió y no estaba en ningún commit. <c>Module01EppLayout.Mine</c> devuelve <b>toda</b>
    /// la zona sin filtro, así que no había forma de salvar nada.
    /// </para>
    /// <para>
    /// <b>Cómo lo evita.</b> Al colocar, el layout apunta aquí la posición exacta que escribió.
    /// En la corrida siguiente compara: si el elemento está donde él lo dejó, es suyo y puede
    /// recolocarlo; si está en otro sitio, lo movió una persona y <b>no se toca</b>. No hace
    /// falta marcar nada ni acordarse de nada: mover el objeto en el editor ya lo protege.
    /// </para>
    /// <para>
    /// Va en un fichero de texto dentro de <c>ProjectSettings</c> y no en un asset ni en un
    /// componente por objeto: no ensucia la escena —que es lo que se revisa en el diff— ni
    /// obliga a tocar sesenta objetos, y al no ser un asset no se lo lleva un reimport.
    /// </para>
    /// </summary>
    internal static class Module01LayoutStamp
    {
        private const string Fichero = "ProjectSettings/Module01LayoutStamp.txt";

        /// <summary>
        /// Cuánto puede moverse un elemento y seguir contando como «donde lo dejó el layout».
        /// Un milímetro es ruido de serialización; un centímetro ya es alguien arrastrándolo.
        /// </summary>
        private const float Tolerancia = 0.01f;

        private static Dictionary<string, Vector3> _cache;

        private static Dictionary<string, Vector3> Cargar()
        {
            if (_cache != null)
                return _cache;

            _cache = new Dictionary<string, Vector3>();
            if (!File.Exists(Fichero))
                return _cache;

            foreach (string linea in File.ReadAllLines(Fichero))
            {
                string[] partes = linea.Split('\t');
                if (partes.Length != 4)
                    continue;
                if (float.TryParse(partes[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                 && float.TryParse(partes[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)
                 && float.TryParse(partes[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
                    _cache[partes[0]] = new Vector3(x, y, z);
            }
            return _cache;
        }

        /// <summary>
        /// ¿Lo movió una persona desde la última corrida? Un elemento que el layout nunca ha
        /// tocado cuenta como <b>no</b> movido: la primera corrida tiene que poder colocarlo.
        /// </summary>
        public static bool LoMovioUnaPersona(string itemId, Vector3 posicionActual)
        {
            if (string.IsNullOrEmpty(itemId))
                return false;
            if (!Cargar().TryGetValue(itemId, out Vector3 donde))
                return false;
            return Vector3.Distance(donde, posicionActual) > Tolerancia;
        }

        /// <summary>Apunta dónde acaba de dejar el layout este elemento.</summary>
        public static void Apuntar(string itemId, Vector3 posicion)
        {
            if (string.IsNullOrEmpty(itemId))
                return;
            Cargar()[itemId] = posicion;
        }

        /// <summary>Vuelca lo apuntado. Se llama una vez al final de la corrida.</summary>
        public static void Guardar()
        {
            if (_cache == null)
                return;
            var lineas = _cache
                .OrderBy(p => p.Key)
                .Select(p => string.Format(CultureInfo.InvariantCulture, "{0}\t{1:0.0000}\t{2:0.0000}\t{3:0.0000}",
                                           p.Key, p.Value.x, p.Value.y, p.Value.z));
            File.WriteAllLines(Fichero, lineas.ToArray());
        }

        /// <summary>
        /// Toma como «colocación del layout» lo que haya ahora mismo en la escena.
        /// <para>
        /// Hace falta la primera vez y después de cualquier reparto hecho antes de que existiera
        /// este registro: con el fichero vacío, <see cref="LoMovioUnaPersona"/> no puede saber
        /// qué movió nadie y el layout volvería a repartirlo todo. Sembrado con lo de ahora, a
        /// partir de aquí cualquier cosa que alguien arrastre queda protegida sola.
        /// </para>
        /// </summary>
        [MenuItem("RideSafe/Módulo 1/Dar por buena la colocación actual (la protege del layout)")]
        public static void SembrarConLoQueHayAhora()
        {
            var entidades = Object.FindObjectsByType<WorldObjectTaskEntity>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            int n = 0;
            foreach (WorldObjectTaskEntity e in entidades)
            {
                if (string.IsNullOrEmpty(e.ItemId))
                    continue;
                Apuntar(e.ItemId, e.transform.position);
                n++;
            }
            Guardar();
            Debug.Log($"[Module01] Apuntada la colocación de {n} elementos. "
                    + "Lo que muevas a partir de ahora, el layout ya no lo toca.");
        }

        /// <summary>
        /// Olvida lo apuntado: la próxima corrida vuelve a repartirlo todo. Es la salida para
        /// cuando se quiere justamente eso, que es raro y por eso está en el menú y no por
        /// defecto.
        /// </summary>
        [MenuItem("RideSafe/Módulo 1/Olvidar qué colocó el layout (vuelve a repartirlo todo)")]
        public static void Olvidar()
        {
            if (!EditorUtility.DisplayDialog(
                    "Olvidar la colocación del layout",
                    "La próxima vez que coloques las mesas, el layout moverá TODOS los EPP, "
                  + "incluidos los que hayas puesto a mano.\n\n¿Seguro?",
                    "Sí, olvidar", "Cancelar"))
                return;

            _cache = new Dictionary<string, Vector3>();
            if (File.Exists(Fichero))
                File.Delete(Fichero);
            Debug.Log("[Module01] Olvidado el registro del layout: la próxima corrida reparte todo.");
        }
    }
}
