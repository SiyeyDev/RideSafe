using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RideSafe.Module00.Tests
{
    /// <summary>
    /// The vehicle name cards of PF_Module00_UI hold the Spanish names on one line
    /// ("Bicicleta eléctrica", "Patineta eléctrica de pie": +35% string reserve of the UI system)
    /// and stay centred over their vehicles.
    /// </summary>
    public class VehicleLabelLayoutTests
    {
        private const string PrefabPath = "Assets/_Project/UI/Prefabs/PF_Module00_UI.prefab";
        private const float Pad = 16f / 2.8f; // UIBuilderKit.PadFor(2.8): slice padding around the visible card

        [TestCase("Option_ebike", 200f)]
        [TestCase("Option_escooter", 830f)]
        public void VehicleCard_FitsItsContent_AroundItsVehicle(string name, float centre)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            RectTransform card = Find(prefab.transform, name);
            Assert.AreEqual(centre, card.anchoredPosition.x + card.sizeDelta.x / 2f, 0.5f, name + " centre");
            RideSafe.UI.FitCardToContent fit = card.GetComponent<RideSafe.UI.FitCardToContent>();
            Assert.IsNotNull(fit, name + " sizes itself to its text and selection");
            SerializedObject serialized = new SerializedObject(fit);
            Assert.AreSame(card.GetComponent<UnityEngine.UI.Toggle>(), serialized.FindProperty("_toggle").objectReferenceValue);
            Assert.AreSame(Find(card, "Title").GetComponent<TMPro.TMP_Text>(), serialized.FindProperty("_title").objectReferenceValue);
            Assert.AreSame(Find(card, "Description").GetComponent<TMPro.TMP_Text>(), serialized.FindProperty("_description").objectReferenceValue);
            Assert.AreSame(Find(card, "Check"), serialized.FindProperty("_check").objectReferenceValue);
            Assert.AreSame(Find(card, "StateCaption"), serialized.FindProperty("_caption").objectReferenceValue);
        }

        [Test]
        public void ConfirmationBar_TextsStackOnTwoLines_LeftOfTheButtons()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            RectTransform bar = Find(prefab.transform, "ConfirmationBar");
            RectTransform selected = Find(bar, "SelectedText");
            RectTransform prompt = Find(bar, "Prompt");
            float change = Find(bar, "ChangeButton").anchoredPosition.x;
            Assert.LessOrEqual(selected.anchoredPosition.x + selected.sizeDelta.x, change, "vehicle sentence clear of Change");
            Assert.LessOrEqual(prompt.anchoredPosition.x + prompt.sizeDelta.x, change, "prompt clear of Change");
            float selectedBottom = selected.anchoredPosition.y - selected.sizeDelta.y;
            Assert.LessOrEqual(prompt.anchoredPosition.y, selectedBottom + 0.01f, "the prompt sits below the sentence, not on it");
            TMPro.TMP_Text sentence = selected.GetComponent<TMPro.TMP_Text>();
            Assert.IsTrue(sentence.enableAutoSizing, "a long vehicle name shrinks instead of wrapping");
            Assert.AreEqual(TMPro.TextWrappingModes.NoWrap, sentence.textWrappingMode);
        }

        private static RectTransform Find(Transform root, string name)
        {
            foreach (RectTransform t in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (t.name == name)
                    return t;
            }
            Assert.Fail(root.name + " has no " + name);
            return null;
        }
    }
}
