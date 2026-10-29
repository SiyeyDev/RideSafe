#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using I2.Loc;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Arregla la importación de la hoja de I2 del 2026-10-22, que dejó el módulo 1 sin un solo
/// término utilizable.
/// <para>
/// <b>Tres cosas salieron mal a la vez</b>, y las tres están descritas en <c>Docs/memory.md</c> §9:
/// </para>
/// <list type="number">
/// <item>
/// <b>El prefijo de categoría quedó duplicado</b> en 92 términos:
/// <c>Module1/Module1/Confirm</c>. I2 trata <c>Module1/</c> como categoría, así que si las
/// claves de la hoja ya lo traen, lo vuelve a añadir.
/// </item>
/// <item>
/// <b>La categoría volvió de <c>M1</c> a <c>Module1</c>.</b> El código busca <c>M1/</c>, y
/// además la categoría decide la carpeta del audio: la herramienta escribe en
/// <c>Resources/Sounds/&lt;categoría&gt;/&lt;idioma&gt;/</c>, que es donde lee
/// <c>NarrationPlayer</c>. Con <c>Module1</c> los mp3 irían a otra carpeta y los 40 que ya hay
/// en <c>Sounds/M1/</c> quedarían huérfanos.
/// </item>
/// <item>
/// <b>Los 31 elementos nuevos entraron con el ID de la biblioteca como clave</b> —<c>E01</c>,
/// <c>O03</c>, <c>H12</c>— en vez de con la clave que busca el código
/// (<c>Item_HelmetOk</c>…). El texto es correcto; lo que no servía era el nombre.
/// </item>
/// </list>
/// <para>
/// <b>No se escribe ningún texto aquí: solo se renombran claves.</b> Los textos son los que
/// Charlie escribió en la hoja y se conservan intactos, que es justo el motivo de arreglar en
/// vez de restaurar desde git.
/// </para>
/// <para>Idempotente: correrlo dos veces no cambia nada la segunda.</para>
/// </summary>
public static class Module01I2Repair
{
    private const string AssetPath = "Assets/Resources/I2Languages.asset";

    /// <summary>
    /// La renumeración de Charlie, de su ID al de la biblioteca del cliente.
    /// <para>
    /// Charlie compactó los huecos del Excel —el documento salta de O02 a O04 y de H04 a H06—
    /// así que sus O03..O14 y H05..H12 están corridos. <b>Las 31 filas se verificaron una a una
    /// comparando el texto en español con la columna «Elemento» del Excel: las 31 coinciden
    /// exactas</b>, así que esta tabla no es una suposición sobre el orden.
    /// </para>
    /// </summary>
    private static readonly Dictionary<string, string> Renumbered = new Dictionary<string, string>
    {
        { "E01", "E01" }, { "E02", "E02" }, { "E03", "E03" }, { "E04", "E04" }, { "E05", "E05" },
        { "O01", "O01" }, { "O02", "O02" }, { "O03", "O04" }, { "O04", "O07" }, { "O05", "O08" },
        { "O06", "O09" }, { "O07", "O11" }, { "O08", "O12" }, { "O09", "O13" }, { "O10", "O14" },
        { "O11", "O15" }, { "O12", "O16" }, { "O13", "O18" }, { "O14", "O19" },
        { "H01", "H01" }, { "H02", "H02" }, { "H03", "H03" }, { "H04", "H04" }, { "H05", "H06" },
        { "H06", "H07" }, { "H07", "H08" }, { "H08", "H09" }, { "H09", "H10" }, { "H10", "H11" },
        { "H11", "H12" }, { "H12", "H13" },
    };

    [MenuItem("RideSafe/Módulo 1/Arreglar la importación de I2 (prefijo, categoría y claves)")]
    public static void Repair()
    {
        LanguageSourceAsset asset = AssetDatabase.LoadAssetAtPath<LanguageSourceAsset>(AssetPath);
        if (asset == null) { Debug.LogError("[I2] No carga " + AssetPath); return; }
        LanguageSourceData data = asset.SourceData;

        // ID de la biblioteca -> clave que busca el código, desde la tabla única del catálogo.
        Dictionary<string, string> keyByLibraryId = RideSafe.Module01.Editor.Module01Library.All
            .ToDictionary(e => e.LibraryId, e => e.NameKey);

        int dobles = 0, categoria = 0, claves = 0, choques = 0, sustituidos = 0;
        HashSet<string> taken = new HashSet<string>(data.mTerms.Select(t => t.Term));

        foreach (TermData term in data.mTerms.ToList())
        {
            string nuevo = term.Term;

            // 1) Module1/Module1/X -> Module1/X
            if (nuevo.StartsWith("Module1/Module1/"))
            {
                nuevo = "Module1/" + nuevo.Substring("Module1/Module1/".Length);
                dobles++;
            }

            // 2) Module1/X -> M1/X
            if (nuevo.StartsWith("Module1/"))
            {
                nuevo = "M1/" + nuevo.Substring("Module1/".Length);
                categoria++;
            }

            // 3) M1/<ID de Charlie> -> la clave de verdad
            if (nuevo.Length == "M1/E01".Length && nuevo.StartsWith("M1/"))
            {
                string suyo = nuevo.Substring(3);
                if (Renumbered.TryGetValue(suyo, out string libraryId)
                    && keyByLibraryId.TryGetValue(libraryId, out string key))
                {
                    nuevo = key;
                    claves++;
                }
            }

            if (nuevo == term.Term) continue;

            if (taken.Contains(nuevo))
            {
                // Choque: el asset traía el término viejo Y el nuevo de la hoja para el mismo
                // elemento. Gana el de la hoja, que es la redacción del cliente —y en algún caso
                // arregla un inglés roto: «Secured load» había vuelto del traductor automático
                // como «Cargo insurance»—. El viejo se borra, y su texto queda en el log para
                // que la sustitución sea visible y no un cambio silencioso.
                TermData previo = data.mTerms.FirstOrDefault(t => t.Term == nuevo);
                if (previo == null) { choques++; continue; }

                Debug.Log($"[I2] {nuevo}: se sustituye «{Texto(previo, data)}» "
                          + $"por «{Texto(term, data)}» (de la hoja del cliente).");

                data.mTerms.Remove(previo);
                taken.Remove(nuevo);
                sustituidos++;
            }

            taken.Remove(term.Term);
            taken.Add(nuevo);
            term.Term = nuevo;
        }

        // El índice de I2 es `[NonSerialized]` y se construye a partir de los nombres: tras
        // renombrarlos hay que forzar su reconstrucción, o las búsquedas seguirían resolviendo
        // con los nombres viejos hasta la siguiente recarga de dominio.
        data.UpdateDictionary(true);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();

        Debug.Log($"[I2] Arreglado: {dobles} con prefijo duplicado, {categoria} de categoría "
                  + $"Module1→M1, {claves} con ID crudo → clave, {sustituidos} textos viejos sustituidos por los de la hoja. Choques sin resolver: {choques}.");

        Report(data, keyByLibraryId);
    }

    /// <summary>El español de un término, o el inglés si no lo tiene. Solo para el log.</summary>
    private static string Texto(TermData term, LanguageSourceData data)
    {
        int es = data.GetLanguageIndex("Español");
        if (es >= 0 && es < term.Languages.Length && !string.IsNullOrWhiteSpace(term.Languages[es]))
            return term.Languages[es];
        int en = data.GetLanguageIndex("English");
        if (en >= 0 && en < term.Languages.Length && !string.IsNullOrWhiteSpace(term.Languages[en]))
            return term.Languages[en];
        return "(vacío)";
    }

    /// <summary>Qué pide el código y qué hay, para no descubrirlo viendo claves crudas en pantalla.</summary>
    private static void Report(LanguageSourceData data, Dictionary<string, string> keyByLibraryId)
    {
        int es = data.GetLanguageIndex("Español");
        int en = data.GetLanguageIndex("English");
        Dictionary<string, TermData> byTerm = data.mTerms.ToDictionary(t => t.Term, t => t);

        List<string> faltan = new List<string>();
        List<string> sinIdioma = new List<string>();

        void Check(string key)
        {
            if (!byTerm.TryGetValue(key, out TermData t)) { faltan.Add(key); return; }
            bool okEs = es >= 0 && es < t.Languages.Length && !string.IsNullOrWhiteSpace(t.Languages[es]);
            bool okEn = en >= 0 && en < t.Languages.Length && !string.IsNullOrWhiteSpace(t.Languages[en]);
            if (!okEs || !okEn)
                sinIdioma.Add($"{key} (ES:{(okEs ? "ok" : "falta")} EN:{(okEn ? "ok" : "falta")})");
        }

        foreach (var e in RideSafe.Module01.Editor.Module01Library.All)
        {
            Check(e.NameKey);
            Check(e.RiskKey);
        }
        foreach (var t in RideSafe.Module01.Editor.Module01Library.Tables)
            Check(t.TitleKey);

        Debug.Log($"[I2] Lo que pide el catálogo: faltan {faltan.Count} términos, "
                  + $"{sinIdioma.Count} a medio traducir.");
        if (faltan.Count > 0)
            Debug.LogWarning("[I2] FALTAN:\n  " + string.Join("\n  ", faltan));
        if (sinIdioma.Count > 0)
            Debug.LogWarning("[I2] A MEDIAS:\n  " + string.Join("\n  ", sinIdioma));
    }
}
#endif
