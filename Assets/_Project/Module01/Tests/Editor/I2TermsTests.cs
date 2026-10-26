using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RideSafe.Module01.Editor;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Que cada clave que el módulo 1 enseña exista en I2, en los dos idiomas.
    /// <para>
    /// Una clave que falta no da error: <c>UIText.Resolve</c> devuelve la clave tal cual y el
    /// aprendiz lee «M1/MyChoice_Title». Pasó el 2026-10-22: la importación de la hoja
    /// (<c>febeb75</c>) borró 24 términos y nadie lo vio hasta recorrer el módulo entero.
    /// </para>
    /// <para>
    /// Se lee el asset como texto porque I2 no tiene asmdef y este ensamblado no puede
    /// referenciarlo.
    /// </para>
    /// </summary>
    public class I2TermsTests
    {
        private const string AssetPath = "Assets/Resources/I2Languages.asset";

        /// <summary>Las que el código pide como literal y no salen de ningún catálogo.</summary>
        private static readonly string[] UiKeys =
        {
            "M1/MyChoice_Title", "M1/MyChoice_Note", "M1/MyChoice_Empty",
            "M1/Legend_Correct", "M1/Legend_Incorrect", "M1/Legend_NoEffect",
            "M1/Legend_Omission", "M1/Legend_AtRisk", "M1/Conditional",
            "M1/Decision_Hint", "M1/ContinueModule2", "M1/RepeatModule",
            "M1/Remove_Question", "M1/Orientation_Tag",
            "TutorialHelp/SelectInterface", "TutorialHelp/SelectObject",
        };

        private static IEnumerable<string> RequiredKeys()
        {
            foreach (string key in UiKeys)
                yield return key;
            foreach (var table in Module01Library.Tables)
                yield return table.TitleKey;
            foreach (PerformanceRank rank in Enum.GetValues(typeof(PerformanceRank)))
            {
                yield return rank.FeedbackKey();
                yield return rank.TitleKey();
            }
        }

        [Test]
        public void Toda_clave_que_se_ve_tiene_texto_en_ingles_y_espanol()
        {
            Dictionary<string, string[]> terms = ReadTerms(File.ReadAllText(AssetPath));
            List<string> missing = RequiredKeys().Distinct()
                .Where(k => !terms.TryGetValue(k, out string[] texts) || texts.Any(string.IsNullOrWhiteSpace))
                .ToList();
            Assert.IsEmpty(missing, "Sin texto en I2 (se verían como clave):\n  " + string.Join("\n  ", missing));
        }

        [Test]
        public void El_estado_no_necesario_se_llama_asi()
        {
            // Lo pidió la PO el 2026-10-26: «correcto, no necesario o incorrecto».
            Assert.AreEqual("No necesario", ReadTerms(File.ReadAllText(AssetPath))["M1/Legend_NoEffect"][1]);
        }

        /// <summary>Término → [inglés, español] del asset de I2 del proyecto.</summary>
        internal static Dictionary<string, string[]> ReadProjectTerms() => ReadTerms(File.ReadAllText(AssetPath));

        /// <summary>Término → [inglés, español], en el orden de idiomas del asset.</summary>
        private static Dictionary<string, string[]> ReadTerms(string yaml)
        {
            var terms = new Dictionary<string, string[]>();
            string key = null;
            List<string> languages = null;
            bool reading = false;
            int itemIndent = -1;
            foreach (string raw in yaml.Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                string trimmed = line.TrimStart();
                int indent = line.Length - trimmed.Length;
                if (trimmed.StartsWith("- Term: "))
                {
                    Store(terms, key, languages);
                    key = trimmed.Substring("- Term: ".Length).Trim();
                    languages = null;
                    reading = false;
                }
                else if (key != null && trimmed == "Languages:")
                {
                    languages = new List<string>();
                    reading = true;
                    itemIndent = indent;
                }
                else if (reading && trimmed.StartsWith("- ") && indent == itemIndent)
                    languages.Add(trimmed.Substring(2));
                // A long text folds onto deeper-indented lines; YAML joins them with a space.
                else if (reading && languages.Count > 0 && indent > itemIndent && trimmed.Length > 0)
                    languages[languages.Count - 1] += " " + trimmed;
                else
                    reading = false;
            }
            Store(terms, key, languages);
            return terms;
        }

        private static void Store(Dictionary<string, string[]> terms, string key, List<string> languages)
        {
            if (key != null && languages != null && languages.Count >= 2)
                terms[key] = new[] { Clean(languages[0]), Clean(languages[1]) };
        }

        /// <summary>Quita las comillas del YAML y deshace los \xNN de Unity.</summary>
        private static string Clean(string value)
        {
            value = value.Trim();
            if (value.Length >= 2 && (value[0] == '"' || value[0] == '\'') && value[value.Length - 1] == value[0])
                value = value.Substring(1, value.Length - 2);
            return Regex.Replace(value, @"\\x([0-9A-Fa-f]{2})", m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
        }
    }
}
