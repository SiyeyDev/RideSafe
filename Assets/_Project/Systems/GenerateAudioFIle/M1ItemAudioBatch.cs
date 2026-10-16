#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using I2.Loc;
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
/// La voz es <b>Alice — Clear, Engaging Educator</b>, la misma con la que se grabó
/// <c>M01_Welcome</c>: con otra, el módulo sonaría con dos narradores. Los ajustes son los que
/// trae <see cref="VoiceSettings"/> por defecto, que ya coinciden con los de la ventana
/// (estabilidad 0,75 · similitud 0,85 · estilo 0,1 · speaker boost).
/// </para>
/// <para>
/// Deja el avance en <c>Temp/ridesafe-audio.txt</c> para poder seguirlo desde fuera, igual que
/// hacen los tests.
/// </para>
/// </summary>
public static class M1ItemAudioBatch
{
    public const string ResultPath = "Temp/ridesafe-audio.txt";

    private const string VoiceId = "Xb7hH8MSUJpSbSDYk0k2";
    private const string UrlStart = "https://api.elevenlabs.io/v1/text-to-speech/";
    private const string UrlEnd = "/with-timestamps";
    private const string Category = "M1";
    private const string Suffix = "_Risk";

    /// <summary>Lo que se locuta: los nombres de elementos y puntos, sin su «por qué».</summary>
    private static bool EsLocutable(string term) =>
        !term.EndsWith(Suffix) &&
        (term.StartsWith(Category + "/Item_") || term.StartsWith(Category + "/Point_"));

    private static readonly string[] Languages = { "Español", "English" };

    [MenuItem("RideSafe/Módulo 1/Generar locución de los elementos (ES + EN)")]
    public static void Run()
    {
        EditorCoroutineUtility.StartCoroutineOwnerless(Batch());
    }

    private static IEnumerator Batch()
    {
        LanguageSourceAsset asset =
            AssetDatabase.LoadAssetAtPath<LanguageSourceAsset>("Assets/Resources/I2Languages.asset");
        LanguageSourceData data = asset.SourceData;

        List<TermData> terms = new List<TermData>();
        foreach (TermData t in data.mTerms)
            if (EsLocutable(t.Term))
                terms.Add(t);
        terms.Sort((a, b) => string.CompareOrdinal(a.Term, b.Term));

        Directory.CreateDirectory("Temp");
        File.WriteAllText(ResultPath, "RUNNING\n");
        Log($"{terms.Count} terminos x {Languages.Length} idiomas");

        int hechos = 0, fallos = 0;
        foreach (string language in Languages)
        {
            int idx = data.GetLanguageIndex(language);
            if (idx < 0)
            {
                Log($"ERROR: el idioma '{language}' no esta en I2");
                fallos++;
                continue;
            }

            string folder = $"Assets/Resources/Sounds/{Category}/{language}";
            // overrideAudio y overrideTimeStamp en true: si se relanza, rehace. Si no, un
            // intento a medias dejaria mp3 viejos mezclados con nuevos y nadie lo notaria.
            GenerateAudio generator = new GenerateAudio(folder, true, true, new VoiceSettings());

            foreach (TermData term in terms)
            {
                string name = term.Term.Substring(Category.Length + 1);
                string text = term.Languages[idx];
                if (string.IsNullOrWhiteSpace(text))
                {
                    Log($"SALTADO {language}/{name}: sin texto");
                    fallos++;
                    continue;
                }

                yield return EditorCoroutineUtility.StartCoroutineOwnerless(
                    generator.Generate(UrlStart + VoiceId + UrlEnd, text, name));

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
