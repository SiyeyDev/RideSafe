using System;
using System.Collections.Generic;
using I2.Loc;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace RideSafe.UI.EditorTools
{
    /// <summary>
    /// Module 0 localization: every visible text of PF_Module00_UI resolves an I2 term (M0/...).
    /// <para>
    /// Static texts get an I2 <see cref="Localize"/> component. Texts that panels write at
    /// runtime (card caption, On/Off, the vehicle confirmation sentence) store the term in
    /// the serialized field and resolve it through <see cref="UIText"/>.
    /// </para>
    /// <para>
    /// The pass is strict: a Module 0 text without a term fails the build instead of
    /// shipping untranslated. Add the English text and its term here and the term to I2.
    /// </para>
    /// </summary>
    internal sealed partial class UIModules
    {
        /// <summary>Storyboard English text -> I2 term. The English column of I2 holds the same text.</summary>
        private static readonly Dictionary<string, string> M0Terms = new Dictionary<string, string>
        {
            { "Better decisions in motion.", "M0/Welcome_Tagline" },
            { "Begin", "M0/Button_Begin" },
            { "Back", "M0/Button_Back" },
            { "Continue", "M0/Button_Continue" },
            { "Change", "M0/Button_Change" },
            { "Confirm", "M0/Button_Confirm" },
            { "Start Module 1", "M0/Button_StartModule1" },
            { "Selected", "M0/Caption_Selected" },
            { "On", "M0/Switch_On" },
            { "Off", "M0/Switch_Off" },

            { "Choose your language", "M0/Language_Title" },
            { "You can change this later.", "M0/Language_Subtitle" },

            { "Which rules apply to your riding?", "M0/Location_Title" },
            { "This sets the rule pack the whole session is judged against.", "M0/Location_Subtitle" },
            { "Florida", "M0/Location_Florida" },
            { "United States · state e-bike and scooter rule pack", "M0/Location_Florida_Description" },
            { "Bogotá", "M0/Location_Bogota" },
            { "Colombia · city cycling and micromobility rule pack", "M0/Location_Bogota_Description" },

            { "Choose your vehicle", "M0/Vehicle_Title" },
            { "Look at each vehicle before you choose.", "M0/Vehicle_Subtitle" },
            { "E-bike", "M0/Vehicle_EBike" },
            { "Pedal assist · seated", "M0/Vehicle_EBike_Description" },
            { "Standing e-scooter", "M0/Vehicle_EScooter" },
            { "Standing · throttle", "M0/Vehicle_EScooter_Description" },
            { "{0} selected —", "M0/Vehicle_SelectedFormat" },
            { "confirm to continue?", "M0/Vehicle_ConfirmPrompt" },

            { "Set up your comfort", "M0/Comfort_Title" },
            { "Nothing here affects your results.", "M0/Comfort_Subtitle" },
            { "Riding posture", "M0/Comfort_Posture" },
            { "Sets eye height and handlebar reach", "M0/Comfort_Posture_Detail" },
            { "Seated", "M0/Comfort_Seated" },
            { "Standing", "M0/Comfort_Standing" },
            { "Subtitles", "M0/Comfort_Subtitles" },
            { "Every spoken line, placed below the instruction panel", "M0/Comfort_Subtitles_Detail" },
            { "Text size", "M0/Comfort_TextSize" },
            { "Scales all world-space type together", "M0/Comfort_TextSize_Detail" },
            { "Dominant hand", "M0/Comfort_Hand" },
            { "Mirrors mounted controls and the ray hand", "M0/Comfort_Hand_Detail" },
            { "Left", "M0/Comfort_Left" },
            { "Right", "M0/Comfort_Right" }
        };

        /// <summary>Language names are shown in their own language and never translated.</summary>
        private static readonly HashSet<string> M0Autonyms = new HashSet<string> { "English", "Español" };

        /// <summary>Texts written at runtime from a term or a number; they must not get a Localize component.</summary>
        private static readonly HashSet<string> M0RuntimeTexts = new HashSet<string>
        {
            "StateCaption", "SubtitlesState", "TextSizeValue", "SelectedText"
        };

        private static void LocalizeModule00(GameObject module)
        {
            List<string> missing = new List<string>();

            foreach (TMP_Text text in module.GetComponentsInChildren<TMP_Text>(true))
            {
                if (M0RuntimeTexts.Contains(text.name) || string.IsNullOrEmpty(text.text) || M0Autonyms.Contains(text.text))
                    continue;
                string term;
                if (M0Terms.TryGetValue(text.text, out term))
                    text.gameObject.AddComponent<Localize>().Term = term;
                else
                    missing.Add("'" + text.text + "' (" + text.name + ")");
            }

            foreach (SelectableStyle style in module.GetComponentsInChildren<SelectableStyle>(true))
                ToTerms(style, missing, "_onCaption", "_onText", "_offText");

            foreach (ChoicePanel panel in module.GetComponentsInChildren<ChoicePanel>(true))
            {
                ToTerms(panel, missing, "_confirmationFormat");
                SerializedObject so = new SerializedObject(panel);
                SerializedProperty options = so.FindProperty("_options");
                for (int i = 0; i < options.arraySize; i++)
                    ToTerm(options.GetArrayElementAtIndex(i).FindPropertyRelative("DisplayName"), missing);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            if (missing.Count > 0)
                throw new InvalidOperationException("[RideSafe.UI] Module 00 texts without an I2 term:\n  " +
                                                    string.Join("\n  ", missing));
        }

        private static void ToTerms(UnityEngine.Object target, List<string> missing, params string[] fields)
        {
            SerializedObject so = new SerializedObject(target);
            foreach (string field in fields)
                ToTerm(so.FindProperty(field), missing);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ToTerm(SerializedProperty property, List<string> missing)
        {
            string value = property.stringValue;
            if (string.IsNullOrEmpty(value) || M0Autonyms.Contains(value) || value.StartsWith("M0/", StringComparison.Ordinal))
                return;
            string term;
            if (M0Terms.TryGetValue(value, out term))
                property.stringValue = term;
            else
                missing.Add("'" + value + "' (" + property.serializedObject.targetObject.name + "." + property.name + ")");
        }
    }
}
