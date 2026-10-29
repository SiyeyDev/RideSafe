using NUnit.Framework;
using RideSafe.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Every panel title carries the company mark at its top left (Cindy, 2026-10-21): the navy
    /// mark on the light boxes, the white one on the blue ones (station sign, module 0 titles). The title keeps
    /// clear of it, and «Elige tu vehículo» gets a box of its own: it got lost against the garage.
    /// One mark in view, by the title: not on the panels that open beside a station's sign.
    /// </summary>
    public class BrandMarkLayoutTests
    {
        private const string Module00 = "Assets/_Project/UI/Prefabs/PF_Module00_UI.prefab";
        private const string Module01 = "Assets/_Project/UI/Prefabs/PF_Module01_UI.prefab";
        private const string ScenePath = "Assets/_Project/Scene/RideSafe_Garaje_v5.unity";
        private const string Navy = "RideSafe_Mark_Navy";
        private const string White = "RideSafe_Mark_White";

        private static readonly (string prefab, string title)[] LightTitles =
        {
            (Module01, "Orientation/OrientationPanel/ModuleTag"), // on the «Módulo 1» line above the title (PO)
            (Module01, "ReviewChoices/Modal/Title"),
            (Module01, "Comparison/Header/TitleBar/Title")
        };

        /// <summary>The mark beside <paramref name="title"/>, checked to point at it and to leave the text clear.</summary>
        private static Image AssertMarked(Transform title)
        {
            TitleMark mark = null;
            foreach (TitleMark candidate in title.parent.GetComponentsInChildren<TitleMark>(true))
                if (new SerializedObject(candidate).FindProperty("_title").objectReferenceValue == title.GetComponent<TMP_Text>())
                    mark = candidate;
            Assert.IsNotNull(mark, title.name + " in " + title.parent.name + " has no company mark");
            Image image = mark.GetComponent<Image>();
            Assert.IsFalse(image.raycastTarget, "the mark never takes the ray from the buttons");
            // The same size on every panel, 4.5 cm: one that grew with each title read as a different logo.
            Assert.AreEqual(22.5f, ((RectTransform)mark.transform).sizeDelta.y, 0.01f, title.parent.name + "/" + title.name);

            SerializedObject serialized = new SerializedObject(mark);
            float offset = serialized.FindProperty("_offsetX").floatValue;
            float width = ((RectTransform)mark.transform).sizeDelta.x;
            TMP_Text text = title.GetComponent<TMP_Text>();
            if (serialized.FindProperty("_inline").boolValue)
            {
                // Since 2026-10-29 a centred title and its mark centre as one (PO: «distribución de
                // pesos»): room for the mark before the first letter, none mirrored on the right.
                // The room is a left margin, or (the station sign) its HugText's indent of the heading only.
                HugText hug = title.GetComponentInParent<HugText>();
                float room = text.margin.x;
                if (hug != null)
                    room = Mathf.Max(room, new SerializedObject(hug).FindProperty("_firstLineIndent").floatValue);
                Assert.GreaterOrEqual(room, width, title.parent.name + "/" + title.name + " makes room for the mark");
                Assert.AreEqual(0f, text.margin.z, 0.01f, title.parent.name + "/" + title.name + ": no empty strip on the right");
                return image;
            }
            Assert.GreaterOrEqual(text.margin.x, offset + width, title.parent.name + "/" + title.name + " starts after the mark");
            if (text.horizontalAlignment == HorizontalAlignmentOptions.Center)
                Assert.AreEqual(text.margin.x, text.margin.z, 0.01f, "a centred title stays centred in its box");
            return image;
        }

        [Test]
        public void TheLightPanels_CarryTheNavyMark_ClearOfTheirTitles()
        {
            foreach ((string path, string title) in LightTitles)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Transform found = prefab.transform.Find(title);
                Assert.IsNotNull(found, path + " has no " + title);
                Assert.AreEqual(Navy, AssertMarked(found).sprite.name, title);
            }
        }

        [Test]
        public void TheSidebars_BesideTheStationSign_CarryNoMark()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Module01);
            foreach (string list in new[] { "Preparation/ChecklistPanel", "Diagnostic/ChecklistPanel" })
            {
                Assert.IsNull(prefab.transform.Find(list + "/BrandMark"), list + ": the station sign in view carries the mark");
                Assert.AreEqual(0f, prefab.transform.Find(list + "/Title").GetComponent<TMP_Text>().margin.x, list);
            }
        }

        [Test]
        public void ElegirVehiculo_HasABox_BehindItsTitleOnly()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Module00);
            Transform frame = TitleBox(prefab, "Vehicle");
            Assert.IsNotNull(frame, "the title got lost against the garage door");
            SerializedProperty pieces = new SerializedObject(frame.GetComponent<Enclose>()).FindProperty("_pieces");
            // The module 0 subtitles are off since 2026-10-29 (PO: «no son relevantes»).
            Assert.AreEqual(1, pieces.arraySize, "the box holds the title only, not the vehicles");
            Assert.AreEqual("Title", pieces.GetArrayElementAtIndex(0).objectReferenceValue.name);
        }

        /// <summary>Visible bottom of a kit box, top-down: its rect carries the atlas padding around the art.</summary>
        private static float VisibleBottom(RectTransform box) =>
            -box.anchoredPosition.y + box.sizeDelta.y - new SerializedObject(box.GetComponent<Enclose>()).FindProperty("_atlasPadding").floatValue;

        private static RectTransform TitleBox(GameObject prefab, string screen)
        {
            RectTransform header = (RectTransform)prefab.transform.Find(screen + "/HeaderFrame");
            Assert.IsNotNull(header, screen + " has no box for its title");
            SerializedProperty pieces = new SerializedObject(header.GetComponent<Enclose>()).FindProperty("_pieces");
            Assert.AreEqual(1, pieces.arraySize, screen + ": the box holds the title (no subtitle since 2026-10-29)");
            // The blue of the station sign, with white text and the white mark (PO, 2026-10-21).
            Assert.AreEqual("Panel_Modal_Dark", header.GetComponent<Image>().sprite.name, screen);
            Transform title = prefab.transform.Find(screen + "/Header/Title");
            Assert.AreEqual(Color.white, title.GetComponent<TMP_Text>().color, screen + ": white title on the blue");
            Assert.AreEqual(White, AssertMarked(title).sprite.name, screen + ": white mark on the blue");
            return header;
        }

        private static float VisibleLeft(RectTransform rect) => rect.anchoredPosition.x + Pad(rect);
        private static float VisibleRight(RectTransform rect) => rect.anchoredPosition.x + rect.sizeDelta.x - Pad(rect);
        private static float VisibleTop(RectTransform rect) => -rect.anchoredPosition.y + Pad(rect);

        private static float Pad(RectTransform rect)
        {
            Image image = rect.GetComponent<Image>();
            return image != null && image.type == Image.Type.Sliced ? 16f / image.pixelsPerUnitMultiplier : 0f;
        }

        [Test]
        public void TheChoiceScreens_AreLaidOutLikeTheVehicleScreen()
        {
            // PO, 2026-10-21: «the vehicle screen has the right distribution». Crammed under a wide
            // title box, the language cards sat on the bars of the mark painted on the door.
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Module00);
            RectTransform vehicleTitle = (RectTransform)prefab.transform.Find("Vehicle/Header/Title");
            foreach (string screen in new[] { "Language", "Jurisdiction" })
            {
                TitleBox(prefab, screen);
                Assert.IsNull(prefab.transform.Find(screen + "/Frame"), screen + ": the options have no box");
                RectTransform title = (RectTransform)prefab.transform.Find(screen + "/Header/Title");
                Assert.AreEqual(vehicleTitle.anchoredPosition.y, title.anchoredPosition.y, 0.01f, screen + ": the title does not jump between screens");

                Transform options = prefab.transform.Find(screen + "/Options");
                RectTransform left = (RectTransform)options.GetChild(0), right = (RectTransform)options.GetChild(1);
                Assert.Less(VisibleRight(left), 370f, screen + ": the left option clears the door's mark");
                Assert.Greater(VisibleLeft(right), 666f, screen + ": the right option clears the door's mark");
                Assert.AreEqual(183f, VisibleTop(left), 0.5f, screen + ": at the vehicle cards' height");
                Assert.AreEqual(183f, VisibleTop(right), 0.5f, screen);

                RectTransform back = (RectTransform)prefab.transform.Find(screen + "/BackButton");
                RectTransform next = (RectTransform)prefab.transform.Find(screen + "/ContinueButton");
                Assert.AreEqual(VisibleLeft(left), VisibleLeft(back), 0.5f, screen + ": Back under the left card");
                Assert.AreEqual(VisibleRight(right), VisibleRight(next), 0.5f, screen + ": Continue under the right card");
            }
        }

        [Test]
        public void Comfort_TitleInABlueBox_SettingsInTheirOwnUnderIt()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Module00);
            RectTransform header = TitleBox(prefab, "Comfort");
            RectTransform content = (RectTransform)prefab.transform.Find("Comfort/Frame");
            float pad = new SerializedObject(content.GetComponent<Enclose>()).FindProperty("_atlasPadding").floatValue;
            Assert.AreEqual(16f, -content.anchoredPosition.y + pad - VisibleBottom(header), 0.5f, "the two boxes 16 apart");
            Assert.AreEqual(header.anchoredPosition.x, content.anchoredPosition.x, 0.5f, "same left edge");
            Assert.AreEqual(header.sizeDelta.x, content.sizeDelta.x, 0.5f, "same width");
        }

        [Test]
        public void TheOldWordmark_IsGoneFromMiEleccion()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Module01);
            Assert.IsNull(prefab.transform.Find("Comparison/Header/TitleBar/Logo"),
                          "the whole logo at 54 px was unreadable: the mark replaces it");
        }

        [Test]
        public void InTheV5_TheSignCarriesTheWhiteMark_TheDecisionTheNavyOne_AndThePopupNone()
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere)
                scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath,
                    UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                Transform popupTitle = null, sign = null, decision = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == "ItemName" && t.parent.name == "Confirmation") popupTitle = t;
                        if (t.name == "Instruction" && t.parent.name == "StationInstruction") sign = t;
                        if (t.name == "Title" && t.parent.name == "ReadinessDecision") decision = t;
                    }
                Assert.IsNotNull(popupTitle);
                Assert.IsNotNull(sign);
                Assert.IsNotNull(decision);
                Assert.IsNull(popupTitle.parent.Find("BrandMark"), "the popup opens under the station's sign, which carries it");
                Assert.AreEqual(White, AssertMarked(sign).sprite.name, "the station sign is dark");
                Assert.AreEqual(Navy, AssertMarked(decision).sprite.name, "the decision box is light");
                // Centred like every panel title since 2026-10-29 (PO: «todo unificado»), its mark inline.
                Assert.AreEqual(HorizontalAlignmentOptions.Center, decision.GetComponent<TMP_Text>().horizontalAlignment);
            }
            finally
            {
                if (openedHere)
                    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
