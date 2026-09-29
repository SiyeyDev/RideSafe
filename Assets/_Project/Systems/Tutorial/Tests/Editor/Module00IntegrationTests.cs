using System.Collections.Generic;
using Cachacos;
using NUnit.Framework;
using RideSafe.Tutorial.AutoHand;
using RideSafe.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace RideSafe.Tutorial.Tests
{
    /// <summary>
    /// Guards the generated PF_Module00_UI: if RideSafeUIPrefabBuilder renames something the
    /// tutorial binds to, or adds a text without a term, these fail in CI instead of the
    /// onboarding silently breaking in the headset.
    /// </summary>
    public class Module00IntegrationTests
    {
        private const string PrefabPath = "Assets/DriveSafe/UI/Prefabs/PF_Module00_UI.prefab";

        private static readonly HashSet<string> Autonyms = new HashSet<string> { "English", "Español" };
        private static readonly HashSet<string> RuntimeTexts = new HashSet<string>
        {
            "StateCaption", "SubtitlesState", "TextSizeValue", "SelectedText"
        };

        private GameObject _prefab;

        [SetUp]
        public void SetUp()
        {
            _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(_prefab, PrefabPath);
        }

        [Test]
        public void Prefab_HasEverythingTheTutorialBindsTo()
        {
            List<string> missing = Module00TutorialBinding.FindMissing(_prefab.GetComponent<ModuleUI>());
            CollectionAssert.IsEmpty(missing, "PF_Module00_UI changed: " + string.Join(", ", missing));
        }

        [Test]
        public void EveryVisibleText_ResolvesAnI2Term_InEnglishAndSpanish()
        {
            List<string> terms = new List<string>();
            List<string> unlocalized = new List<string>();

            foreach (TMP_Text text in _prefab.GetComponentsInChildren<TMP_Text>(true))
            {
                if (RuntimeTexts.Contains(text.name) || string.IsNullOrEmpty(text.text) || Autonyms.Contains(text.text))
                    continue;
                // I2 lives in Assembly-CSharp, which a test asmdef cannot reference: read it by name.
                Component localize = text.GetComponent("Localize");
                string term = localize != null ? new SerializedObject(localize).FindProperty("mTerm").stringValue : null;
                if (string.IsNullOrEmpty(term))
                    unlocalized.Add(text.text);
                else
                    terms.Add(term);
            }

            foreach (SelectableStyle style in _prefab.GetComponentsInChildren<SelectableStyle>(true))
                AddRuntimeTerms(new SerializedObject(style), terms, "_onCaption", "_onText", "_offText");
            foreach (ChoicePanel panel in _prefab.GetComponentsInChildren<ChoicePanel>(true))
            {
                SerializedObject so = new SerializedObject(panel);
                AddRuntimeTerms(so, terms, "_confirmationFormat");
                SerializedProperty options = so.FindProperty("_options");
                for (int i = 0; i < options.arraySize; i++)
                {
                    string name = options.GetArrayElementAtIndex(i).FindPropertyRelative("DisplayName").stringValue;
                    if (!Autonyms.Contains(name))
                        terms.Add(name);
                }
            }

            CollectionAssert.IsEmpty(unlocalized, "Module 0 texts without an I2 term");
            Assert.That(terms.Count, Is.GreaterThan(30));
            AssertAllResolve(terms, "en");
            AssertAllResolve(terms, "es");
        }

        [Test]
        public void Placement_UsesTheHead_NotAssumedHeights()
        {
            GameObject target = new GameObject("placed");
            try
            {
                // Seated learner, eyes at 1.15 m, turned 90 degrees and glancing down.
                Pose seated = new Pose(new Vector3(2f, 1.15f, 3f), Quaternion.Euler(30f, 90f, 0f));
                HeadRelativePlacement.Place(target.transform, seated, 1.1f, -0.12f);
                AssertClose(new Vector3(3.1f, 1.03f, 3f), target.transform.position);
                AssertClose(Vector3.right, target.transform.forward);

                // Standing learner, eyes at 1.7 m: same distance, follows the eyes.
                Pose standing = new Pose(new Vector3(0f, 1.7f, 0f), Quaternion.identity);
                HeadRelativePlacement.Place(target.transform, standing, 2.1f, -0.1f);
                AssertClose(new Vector3(0f, 1.6f, 2.1f), target.transform.position);
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        private static void AddRuntimeTerms(SerializedObject so, List<string> terms, params string[] fields)
        {
            foreach (string field in fields)
            {
                string value = so.FindProperty(field).stringValue;
                if (!string.IsNullOrEmpty(value))
                    terms.Add(value);
            }
        }

        private static void AssertAllResolve(List<string> terms, string languageCode)
        {
            ILocalizationProvider localization = ServiceLocator.Instance.RequestService<ILocalizationProvider>();
            Assert.IsNotNull(localization, "I2 localization provider is registered in the editor");
            try
            {
                Assert.IsTrue(localization.SetLanguage(languageCode), languageCode);
                List<string> missing = new List<string>();
                foreach (string term in terms)
                {
                    if (string.IsNullOrEmpty(localization.GetTranslation(term)))
                        missing.Add(term);
                }
                CollectionAssert.IsEmpty(missing, "terms without a " + languageCode + " translation");
            }
            finally
            {
                localization.SetLanguage("en");
            }
        }

        private static void AssertClose(Vector3 expected, Vector3 actual) =>
            Assert.That(Vector3.Distance(expected, actual), Is.LessThan(0.001f), "expected " + expected + " got " + actual);
    }
}
