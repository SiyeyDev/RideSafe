using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RideSafe.Module00.Tests
{
    /// <summary>
    /// The vehicle name cards of PF_Module00_UI hold the Spanish names on one line
    /// ("Bicicleta eléctrica", "Patineta eléctrica": +35% string reserve of the UI system)
    /// and stay centred over their vehicles.
    /// </summary>
    public class VehicleLabelLayoutTests
    {
        private const string PrefabPath = "Assets/_Project/UI/Prefabs/PF_Module00_UI.prefab";
        private const float Pad = 16f / 2.8f; // UIBuilderKit.PadFor(2.8): slice padding around the visible card

        // Out at the door's sides so the vehicles under them leave «RideSafe» clear (PO, 2026-11-07).
        [TestCase("Option_ebike", -30f)]
        [TestCase("Option_escooter", 1032f)]
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

        /// <summary>
        /// Only the vehicle's name (PO, 2026-11-04): «E-bike» / «E-scooter», no «Pedal assist · seated»
        /// line under it, and the name and check centred in a one-row card.
        /// </summary>
        [TestCase("Option_ebike")]
        [TestCase("Option_escooter")]
        public void VehicleCard_IsTheNameOnly(string name)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            RectTransform card = Find(prefab.transform, name);
            Assert.IsFalse(Find(card, "Description").gameObject.activeSelf, name + " has no description line");
            float middle = card.sizeDelta.y / 2f;
            foreach (string child in new[] { "Title", "Check" })
            {
                RectTransform rect = Find(card, child);
                Assert.AreEqual(middle, -rect.anchoredPosition.y + rect.sizeDelta.y / 2f, 0.5f, name + "/" + child + " is centred");
            }
        }

        /// <summary>
        /// No «Selected» word on any module 0 option (PO, 2026-11-04): the coral card, its fold and its
        /// check already say it.
        /// </summary>
        [Test]
        public void Options_NeverWriteSelected()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            foreach (RideSafe.UI.SelectableStyle style in prefab.GetComponentsInChildren<RideSafe.UI.SelectableStyle>(true))
                Assert.IsEmpty(new SerializedObject(style).FindProperty("_onCaption").stringValue, style.name);
        }

        /// <summary>
        /// The confirmation is its two buttons (PO, 2026-11-07: «quitar el texto duplicado; bastan los
        /// botones»), centred under «RideSafe» on the door, not over it.
        /// </summary>
        [Test]
        public void ConfirmationBar_IsOnlyChangeAndConfirm_UnderTheBrand()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            RectTransform bar = Find(prefab.transform, "ConfirmationBar");
            Assert.IsFalse(Find(bar, "SelectedText").gameObject.activeSelf, "no «E-bike selected —»");
            Assert.IsFalse(Find(bar, "Prompt").gameObject.activeSelf, "no «confirm to continue?»");
            RectTransform change = Find(bar, "ChangeButton"), confirm = Find(bar, "ConfirmButton");
            Assert.LessOrEqual(change.anchoredPosition.x + change.sizeDelta.x, confirm.anchoredPosition.x + 1f, "Change left of Confirm");
            Assert.LessOrEqual(confirm.anchoredPosition.x + confirm.sizeDelta.x, bar.sizeDelta.x + 0.5f, "both inside the bar");
            Assert.AreEqual(501f, bar.anchoredPosition.x + bar.sizeDelta.x / 2f, 1f, "centred on the door");
            Assert.GreaterOrEqual(-bar.anchoredPosition.y, 520f, "below the word «RideSafe» (frame y 405–520)");
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
