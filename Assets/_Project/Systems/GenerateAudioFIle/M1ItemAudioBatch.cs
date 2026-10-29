#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using I2.Loc;
using Newtonsoft.Json.Linq;
using RideSafe.Module01;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Genera de una tacada la locución de los 18 elementos clicables del módulo 1 —los 13 EPP y
/// los 5 puntos del vehículo—, en los dos idiomas.
/// <para>
/// Se locuta <b>el nombre</b> y nada más. El «por qué» de cada elemento existe como texto
/// (<c>_Risk</c>) y se enseña al final, pero decirlo en voz alta mientras el aprendiz decide
/// si se lleva el casco es darle la respuesta.
/// </para>
/// <para>
/// Hace lo mismo que la ventana <c>Tools/Cachacos/GenerateAudio</c> —de hecho usa su misma
/// clase <see cref="GenerateAudio"/>—, pero sin tener que seleccionar 18 términos a mano dos
/// veces. Usarla y no reimplementar la llamada importa: el <c>.json</c> que acompaña al mp3
/// lleva los tiempos convertidos de carácter a <b>palabra</b>, y es lo que lee
/// <c>NarrationPlayer</c> para revelar el subtítulo. Reescribir esa conversión y desviarse un
/// poco deja los subtítulos desfasados sin que nada falle.
/// </para>
/// <para>
/// <b>Una voz por idioma, y esto importa.</b> Hasta el 2026-10-22 las dos tandas se generaban
/// con <b>Alice — Clear, Engaging Educator</b>, que es una locutora <b>inglesa</b>: en inglés
/// sonaba bien y en español sonaba robótica, porque era una voz inglesa leyendo español. Ahora
/// el inglés sigue con Alice y el español usa <b>Daniela — Friendly Host</b>, latinoamericana.
/// Dentro de un mismo idioma la voz no cambia, que es lo que evita que el módulo suene con dos
/// narradores.
/// </para>
/// <para>
/// Los ajustes son los que trae <see cref="VoiceSettings"/> por defecto, que ya coinciden con
/// los de la ventana (estabilidad 0,75 · similitud 0,85 · estilo 0,1 · speaker boost).
/// </para>
/// <para>
/// Deja el avance en <c>Temp/ridesafe-audio.txt</c> para poder seguirlo desde fuera, igual que
/// hacen los tests.
/// </para>
/// </summary>
public static class M1ItemAudioBatch
{
    public const string ResultPath = "Temp/ridesafe-audio.txt";

    private const string UrlStart = "https://api.elevenlabs.io/v1/text-to-speech/";
    private const string UrlEnd = "/with-timestamps";
    private const string Category = "M1";
    private const string Suffix = "_Risk";

    /// <summary>Alice — Clear, Engaging Educator. Locutora inglesa.</summary>
    public const string VoiceAlice = "Xb7hH8MSUJpSbSDYk0k2";

    /// <summary>Daniela — Friendly Host. Español latinoamericano.</summary>
    public const string VoiceDaniela = "ajOR9IDAaubDK5qtLUqQ";

    /// <summary>
    /// Lo que se locuta: los nombres de los elementos y de los puntos del vehículo, sin su
    /// «por qué».
    /// <para>
    /// Los <c>_Risk</c> se quedan fuera a propósito: el porqué de cada elemento se enseña al
    /// final como texto, pero decirlo en voz alta mientras el aprendiz decide si se lleva el
    /// casco es darle la respuesta.
    /// </para>
    /// <para>
    /// Los títulos de estación (<c>Zone_</c>) también se quedan fuera: Charlie los descartó el
    /// 2026-10-22. Solo existe el audio de <c>Zone_Clothing</c> en español, de una tanda suelta
    /// anterior, y sobra.
    /// </para>
    /// </summary>
    private static bool EsLocutable(string term) =>
        !term.EndsWith(Suffix) &&
        (term.StartsWith(Category + "/Item_") || term.StartsWith(Category + "/Point_"));

    /// <summary>(idioma tal como se llama en I2, voz con la que se locuta).</summary>
    private static readonly (string Language, string VoiceId)[] Languages =
    {
        ("Español", VoiceDaniela),
        ("English", VoiceAlice),
    };

    public const string PlanPath = "Temp/ridesafe-audio-plan.txt";

    [MenuItem("RideSafe/Módulo 1/Generar locución de los elementos (ES + EN)")]
    public static void Run()
    {
        EditorCoroutineUtility.StartCoroutineOwnerless(Batch(false));
    }

    /// <summary>
    /// Solo lo que hace falta: los nombres del catálogo de hoy sin audio, o con un audio que ya
    /// no dice el texto de I2. El 2026-10-28 faltaban 24 de 61 elementos y los que había decían el
    /// nombre largo del Excel («Casco certificado, íntegro, talla correcta y abrochado») cuando el
    /// popup ya muestra el corto: voz y subtítulo contra la pantalla. Rehacerlo todo costaría
    /// créditos por los que están bien y por términos retirados.
    /// </summary>
    [MenuItem("RideSafe/Módulo 1/Generar la locución que falta o cambió (ES + EN)")]
    public static void RunMissing()
    {
        EditorCoroutineUtility.StartCoroutineOwnerless(Batch(true));
    }

    /// <summary>Lo que haría <see cref="RunMissing"/>, en <see cref="PlanPath"/>, sin llamar a ElevenLabs.</summary>
    public static void DryRunMissing()
    {
        LanguageSourceData data = Source();
        HashSet<string> catalog = CatalogTerms();
        var plan = new System.Text.StringBuilder();
        int count = 0;
        foreach ((string language, string _) in Languages)
        {
            int idx = data.GetLanguageIndex(language);
            foreach (TermData term in Locutables(data, catalog))
            {
                string name = term.Term.Substring(Category.Length + 1);
                string why = Stale($"Assets/Resources/Sounds/{Category}/{language}", name, term.Languages[idx]);
                if (why == null)
                    continue;
                plan.AppendLine($"{language}/{name}: {why} -> «{term.Languages[idx]}»");
                count++;
            }
        }
        plan.Insert(0, $"{count} audios por generar" + System.Environment.NewLine);
        File.WriteAllText(PlanPath, plan.ToString());
    }

    private static LanguageSourceData Source() =>
        AssetDatabase.LoadAssetAtPath<LanguageSourceAsset>("Assets/Resources/I2Languages.asset").SourceData;

    /// <summary>Los nombres que el juego enseña hoy: el de cada elemento y punto del catálogo.</summary>
    private static HashSet<string> CatalogTerms()
    {
        var terms = new HashSet<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:SafetyItemSO"))
        {
            SafetyItemSO item = AssetDatabase.LoadAssetAtPath<SafetyItemSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (item != null && !string.IsNullOrEmpty(item.NameKey))
                terms.Add(item.NameKey);
        }
        return terms;
    }

    private static List<TermData> Locutables(LanguageSourceData data, HashSet<string> only)
    {
        List<TermData> terms = data.mTerms.Where(t => EsLocutable(t.Term) && (only == null || only.Contains(t.Term))).ToList();
        terms.Sort((a, b) => string.CompareOrdinal(a.Term, b.Term));
        return terms;
    }

    /// <summary>
    /// Por qué hay que rehacer este audio, o <c>null</c> si dice el texto de hoy. El <c>.json</c>
    /// guarda las palabras locutadas: unidas con un espacio tienen que dar el texto de I2.
    /// </summary>
    private static string Stale(string folder, string name, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        if (!File.Exists($"{folder}/{name}.mp3"))
            return "falta";
        if (!File.Exists($"{folder}/{name}.json"))
            return "sin tiempos";
        JToken words = JObject.Parse(File.ReadAllText($"{folder}/{name}.json"))["characters"];
        string said = words == null ? string.Empty : string.Join(" ", words.Select(w => (string)w));
        return Normalize(said) == Normalize(text) ? null : $"dice «{said}»";
    }

    private static string Normalize(string text) => Regex.Replace(text ?? string.Empty, @"\s+", " ").Trim();

    private static IEnumerator Batch(bool onlyStale)
    {
        LanguageSourceData data = Source();
        List<TermData> terms = Locutables(data, onlyStale ? CatalogTerms() : null);

        Directory.CreateDirectory("Temp");
        File.WriteAllText(ResultPath, "RUNNING\n");
        Log($"{terms.Count} terminos x {Languages.Length} idiomas");

        int hechos = 0, fallos = 0;
        foreach ((string language, string voiceId) in Languages)
        {
            int idx = data.GetLanguageIndex(language);
            if (idx < 0)
            {
                Log($"ERROR: el idioma '{language}' no esta en I2");
                fallos++;
                continue;
            }

            Log($"--- {language} con la voz {voiceId}");
            string folder = $"Assets/Resources/Sounds/{Category}/{language}";
            // overrideAudio y overrideTimeStamp en true: si se relanza, rehace. Si no, un
            // intento a medias dejaria mp3 viejos mezclados con nuevos y nadie lo notaria.
            GenerateAudio generator = new GenerateAudio(folder, true, true, new VoiceSettings());

            foreach (TermData term in terms)
            {
                string name = term.Term.Substring(Category.Length + 1);
                string text = term.Languages[idx];
                if (onlyStale && !string.IsNullOrWhiteSpace(text) && Stale(folder, name, text) == null)
                    continue;
                if (string.IsNullOrWhiteSpace(text))
                {
                    Log($"SALTADO {language}/{name}: sin texto");
                    fallos++;
                    continue;
                }

                yield return EditorCoroutineUtility.StartCoroutineOwnerless(
                    generator.Generate(UrlStart + voiceId + UrlEnd, text, name));

                bool ok = File.Exists($"{folder}/{name}.mp3");
                if (ok) hechos++; else fallos++;
                Log($"{(ok ? "ok" : "FALLO")} {language}/{name}");
            }
        }

        AssetDatabase.Refresh();
        File.AppendAllText(ResultPath, $"DONE hechos={hechos} fallos={fallos}\n");
        Debug.Log($"[M1 audio] hechos={hechos} fallos={fallos}");
    }

    private static void Log(string line)
    {
        File.AppendAllText(ResultPath, line + "\n");
    }
}
#endif
