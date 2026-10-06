#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace RideSafe.UI.Tests
{
    /// <summary>
    /// Structural contract and runtime behaviour of the three module UI prefabs, driven
    /// through their real components the way gameplay or the XRPlayer ray would.
    /// </summary>
    public class ModuleUITests
    {
        private const string k_Folder = "Assets/DriveSafe/UI/Prefabs/";
        private static readonly string[] s_Modules = { "PF_Module00_UI", "PF_Module01_UI", "PF_Module02_UI" };

        private static readonly HashSet<string> s_AllowedTextures = new HashSet<string>
        {
            "Assets/DriveSafe/UI/RideSafe_UI_Atlas_A_4096.png",
            "Assets/DriveSafe/UI/RideSafe_UI_Atlas_B_4096.png",
            "Assets/DriveSafe/UI/ridesafe-logo.png"
        };

        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                    Object.Destroy(go);
            }
            _spawned.Clear();
        }

        private static GameObject Load(string module)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_Folder + module + ".prefab");
            Assert.IsNotNull(prefab, "Missing prefab " + module);
            return prefab;
        }

        /// <summary>Spawns a module and shows one of its panels.</summary>
        private GameObject Open(string module, string panel)
        {
            GameObject instance = Object.Instantiate(Load(module));
            _spawned.Add(instance);
            instance.GetComponent<ModuleUI>().Show(panel);
            return instance.transform.Find(panel).gameObject;
        }

        private static T Child<T>(GameObject root, string path) where T : Component
        {
            Transform child = root.transform.Find(path);
            Assert.IsNotNull(child, "Missing '" + path + "' under " + root.name);
            T component = child.GetComponent<T>();
            Assert.IsNotNull(component, "'" + path + "' has no " + typeof(T).Name);
            return component;
        }

        private static int ActiveChildren(Transform parent)
        {
            int count = 0;
            foreach (Transform child in parent)
            {
                if (child.gameObject.activeSelf)
                    count++;
            }
            return count;
        }

        #region Contract

        [Test]
        public void ModulesAreClickableWorldCanvasesWithOnePanelActive()
        {
            foreach (string name in s_Modules)
            {
                GameObject prefab = Load(name);
                Assert.AreEqual(RenderMode.WorldSpace, prefab.GetComponent<Canvas>().renderMode, name);
                Assert.IsNotNull(prefab.GetComponent<UnityEngine.UI.GraphicRaycaster>(), name + " needs a GraphicRaycaster.");
                Assert.IsNotNull(prefab.GetComponent<ModuleUI>(), name + " needs ModuleUI.");
                Assert.IsNull(prefab.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true),
                              name + " must not ship an EventSystem (AutoHand's AutoInputModule owns it).");
                Assert.AreEqual(1, ActiveChildren(prefab.transform), name + " must be saved with only its first panel active.");
            }
        }

        [Test]
        public void ControlsReceiveRaysTextDoesNotAndImagesUseTheAtlases()
        {
            foreach (string name in s_Modules)
            {
                GameObject prefab = Load(name);
                foreach (UnityEngine.UI.Selectable selectable in prefab.GetComponentsInChildren<UnityEngine.UI.Selectable>(true))
                    Assert.IsTrue(selectable.targetGraphic != null && selectable.targetGraphic.raycastTarget,
                                  name + "/" + selectable.name + " would ignore the XRPlayer ray.");

                foreach (TMP_Text text in prefab.GetComponentsInChildren<TMP_Text>(true))
                    Assert.IsFalse(text.raycastTarget, name + "/" + text.name + " would block rays meant for a control.");

                foreach (UnityEngine.UI.Image image in prefab.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                {
                    if (image.sprite == null)
                    {
                        Assert.AreEqual(0f, image.color.a, name + "/" + image.name + " has no sprite.");
                        continue;
                    }
                    Assert.IsTrue(s_AllowedTextures.Contains(AssetDatabase.GetAssetPath(image.sprite.texture)),
                                  name + "/" + image.name + " uses a sprite outside the RideSafe atlases.");
                }

                foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                    Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject),
                                    name + "/" + child.name + " has a missing script.");
            }
        }

        [UnityTest]
        public IEnumerator ShowActivatesOnlyTheRequestedPanel()
        {
            GameObject instance = Object.Instantiate(Load("PF_Module00_UI"));
            _spawned.Add(instance);
            ModuleUI module = instance.GetComponent<ModuleUI>();
            yield return null;

            Assert.AreEqual("Welcome", module.Current, "Start panel must show on enable.");
            module.Show("Comfort");
            Assert.AreEqual(1, ActiveChildren(instance.transform));
            Assert.IsTrue(instance.transform.Find("Comfort").gameObject.activeSelf);
            Assert.IsNotNull(module.Get<ComfortSettingsPanel>("Comfort"));

            LogAssert.Expect(LogType.Error, new Regex("has no panel 'Nope'"));
            module.Show("Nope");
        }

        #endregion

        #region Module 0

        [UnityTest]
        public IEnumerator Language_ContinueIsGatedUntilAChoiceThenReportsIt()
        {
            GameObject panel = Open("PF_Module00_UI", "Language");
            yield return null;

            ChoicePanel choice = panel.GetComponent<ChoicePanel>();
            UnityEngine.UI.Button next = Child<UnityEngine.UI.Button>(panel, "ContinueButton");
            Assert.IsFalse(next.interactable, "Continue must start disabled.");

            string reported = null;
            choice.onContinue.AddListener(id => reported = id);

            UnityEngine.UI.Toggle english = Child<UnityEngine.UI.Toggle>(panel, "Options/Option_en");
            Child<UnityEngine.UI.Toggle>(panel, "Options/Option_es").isOn = true;
            english.isOn = true;
            yield return null;

            Assert.IsTrue(next.interactable);
            Assert.IsFalse(Child<UnityEngine.UI.Toggle>(panel, "Options/Option_es").isOn, "Only one language at a time.");
            Assert.AreEqual("Card_Selected", english.GetComponent<UnityEngine.UI.Image>().sprite.name);
            Assert.IsTrue(english.transform.Find("StateCaption").gameObject.activeSelf);

            next.onClick.Invoke();
            Assert.AreEqual("en", reported);
        }

        [UnityTest]
        public IEnumerator Vehicle_ConfirmationBarAppearsOnChoiceAndChangeClearsIt()
        {
            GameObject panel = Open("PF_Module00_UI", "Vehicle");
            yield return null;

            GameObject bar = panel.transform.Find("ConfirmationBar").gameObject;
            Assert.IsFalse(bar.activeSelf);

            Child<UnityEngine.UI.Toggle>(panel, "VehicleLabels/Option_ebike").isOn = true;
            yield return null;
            Assert.IsTrue(bar.activeSelf);
            Assert.AreEqual("E-bike selected —", Child<TMP_Text>(bar, "SelectedText").text);

            string confirmed = null;
            panel.GetComponent<ChoicePanel>().onContinue.AddListener(id => confirmed = id);
            Child<UnityEngine.UI.Button>(bar, "ConfirmButton").onClick.Invoke();
            Assert.AreEqual("ebike", confirmed);

            Child<UnityEngine.UI.Button>(bar, "ChangeButton").onClick.Invoke();
            yield return null;
            Assert.IsFalse(bar.activeSelf);
            Assert.IsNull(panel.GetComponent<ChoicePanel>().SelectedId);
        }

        [UnityTest]
        public IEnumerator Comfort_ControlsChangeSettingsAndStartReportsThem()
        {
            GameObject panel = Open("PF_Module00_UI", "Comfort");
            yield return null;

            ComfortSettingsPanel comfort = panel.GetComponent<ComfortSettingsPanel>();
            Assert.AreEqual(RidingPosture.Seated, comfort.Current.Posture);
            Assert.AreEqual(110f, comfort.Current.TextScalePercent);
            Assert.AreEqual(DominantHand.Right, comfort.Current.Hand);

            UnityEngine.UI.Toggle subtitles = Child<UnityEngine.UI.Toggle>(panel, "SettingsPanel/SubtitlesSwitch");
            RectTransform knob = Child<RectTransform>(subtitles.gameObject, "Knob");
            float knobOnX = knob.anchoredPosition.x;

            Child<UnityEngine.UI.Toggle>(panel, "SettingsPanel/PostureGroup/StandingOption").isOn = true;
            subtitles.isOn = false;
            Child<UnityEngine.UI.Slider>(panel, "SettingsPanel/TextSizeSlider").value = 130f;
            Child<UnityEngine.UI.Toggle>(panel, "SettingsPanel/HandGroup/LeftOption").isOn = true;
            yield return null;

            ComfortSettings reported = default;
            comfort.onStart.AddListener(settings => reported = settings);
            Child<UnityEngine.UI.Button>(panel, "StartButton").onClick.Invoke();

            Assert.AreEqual(RidingPosture.Standing, reported.Posture);
            Assert.IsFalse(reported.Subtitles);
            Assert.AreEqual(130f, reported.TextScalePercent);
            Assert.AreEqual(DominantHand.Left, reported.Hand);
            Assert.Less(knob.anchoredPosition.x, knobOnX, "Switch knob must slide left when off.");
            Assert.AreEqual("Off", Child<TMP_Text>(panel, "SettingsPanel/SubtitlesState").text);
            Assert.AreEqual("130%", Child<TMP_Text>(panel, "SettingsPanel/TextSizeValue").text);
            Assert.AreEqual(Color.white, Child<TMP_Text>(panel, "SettingsPanel/HandGroup/LeftOption/Label").color);
        }

        #endregion

        #region Module 1

        [UnityTest]
        public IEnumerator Preparation_EmptyStateThenGrowingListThenRemoveByPointing()
        {
            GameObject panel = Open("PF_Module01_UI", "Preparation");
            yield return null;

            ChecklistView list = panel.GetComponent<ChecklistView>();
            GameObject empty = panel.transform.Find("ChecklistPanel/EmptyState").gameObject;
            Transform rows = panel.transform.Find("ChecklistPanel/Rows");
            GameObject counter = panel.transform.Find("ZoneCounter").gameObject;
            Assert.IsTrue(empty.activeSelf);
            Assert.IsFalse(counter.activeSelf);

            list.AddItem("helmet", "Helmet · undamaged");
            list.AddItem("shoes", "Closed-toe shoes");
            list.AddItem("jacket", "Fitted jacket");
            list.AddItem("jacket", "Fitted jacket");
            yield return null;

            Assert.AreEqual(3, list.Count, "Duplicate ids must be ignored.");
            Assert.AreEqual(3, ActiveChildren(rows));
            Assert.IsFalse(empty.activeSelf);
            Assert.IsTrue(panel.transform.Find("ChecklistPanel/Footnote").gameObject.activeSelf);
            Assert.AreEqual("Preparation zone · 3 items", counter.GetComponent<TMP_Text>().text);

            string removed = null;
            list.onItemRemoved.AddListener(id => removed = id);
            rows.Find("Row · Closed-toe shoes").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;

            Assert.AreEqual("shoes", removed, "Pointing at a row and pulling the trigger removes it.");
            Assert.AreEqual(2, list.Count);
        }

        [UnityTest]
        public IEnumerator Diagnostic_SubtitleSwitchesOnceItemsExist()
        {
            GameObject panel = Open("PF_Module01_UI", "Diagnostic");
            yield return null;

            TMP_Text subtitle = Child<TMP_Text>(panel, "ChecklistPanel/Subtitle");
            Assert.AreEqual("Components you check will appear here.", subtitle.text);
            panel.GetComponent<ChecklistView>().AddItem("front-brake", "Front brake lever");
            yield return null;
            Assert.AreEqual("Point a row to remove it.", subtitle.text);
        }

        [UnityTest]
        public IEnumerator ReviewChoices_ShowsOnlyGivenItemsReadOnly()
        {
            GameObject panel = Open("PF_Module01_UI", "ReviewChoices");
            yield return null;

            Transform rows = panel.transform.Find("Modal/ListBox/Rows");
            Assert.AreEqual(0, ActiveChildren(rows), "Preview sample rows must not survive into runtime.");

            panel.GetComponent<ChecklistView>().SetItems(new[]
            {
                new KeyValuePair<string, string>("helmet", "Helmet · undamaged"),
                new KeyValuePair<string, string>("jacket", "Fitted jacket")
            });
            yield return null;

            Assert.AreEqual(2, ActiveChildren(rows));
            Assert.IsFalse(rows.Find("Row · Fitted jacket").GetComponent<UnityEngine.UI.Button>().interactable,
                           "The recap is read-only.");
        }

        [UnityTest]
        public IEnumerator Comparison_ServesSafetyAndDiagnosticReviews()
        {
            GameObject panel = Open("PF_Module01_UI", "Comparison");
            yield return null;

            Assert.AreEqual(4, ActiveChildren(panel.transform.Find("LeftColumn/Rows")));
            Assert.AreEqual(4, ActiveChildren(panel.transform.Find("RightColumn/Rows")));

            panel.GetComponent<ComparisonView>().SetContent("What you checked", "Your pre-ride check.",
                "Your check", new[] { new ComparisonEntry(ItemStatus.Selected, "Front brake lever") },
                "Reference · e-bike", new[]
                {
                    new ComparisonEntry(ItemStatus.CoreOmitted, "Both brake levers", "Front identified, rear not"),
                    new ComparisonEntry(ItemStatus.NotSuited, "Bag on handlebar", "Unsafe load")
                });
            yield return null;

            Transform right = panel.transform.Find("RightColumn/Rows");
            Assert.AreEqual("What you checked", Child<TMP_Text>(panel, "Header/Title").text);
            Assert.AreEqual(1, ActiveChildren(panel.transform.Find("LeftColumn/Rows")));
            Assert.AreEqual(2, ActiveChildren(right));
            Assert.AreEqual("Status_Retry", right.Find("Row · Bag on handlebar/Icon").GetComponent<UnityEngine.UI.Image>().sprite.name,
                            "NotSuited must use its own glyph, not colour alone.");
        }

        [UnityTest]
        public IEnumerator Explanation_ModeAndItemUpdateEveryField()
        {
            GameObject panel = Open("PF_Module01_UI", "Explanation");
            yield return null;

            ExplanationView view = panel.GetComponent<ExplanationView>();
            view.SetMode("Explaining · control {0} of {1}", "The control being explained");
            view.SetItem(1, 3, new ComparisonEntry(ItemStatus.CoreOmitted, "Rear brake lever", "Core control — not identified"),
                         "Both levers are separate controls.", "Testing both levers before you set off.");
            yield return null;

            Assert.AreEqual("Explaining · control 1 of 3", Child<TMP_Text>(panel, "Badge/Text").text);
            Assert.AreEqual("The control being explained", Child<TMP_Text>(panel, "ItemCard/Heading").text);
            Assert.AreEqual("Item 1 of 3", Child<TMP_Text>(panel, "ItemCard/Counter").text);
            Assert.AreEqual("Rear brake lever", Child<TMP_Text>(panel, "ItemCard/Item/Text/Title").text);
            Assert.AreEqual("Both levers are separate controls.", Child<TMP_Text>(panel, "Explanation/Text").text);
        }

        #endregion

        #region Module 2

        [UnityTest]
        public IEnumerator Mounting_StepCardAdvancesAndHasNoFreeHandButton()
        {
            GameObject panel = Open("PF_Module02_UI", "Mounting");
            yield return null;

            Child<CardView>(panel, "StepCard").SetCounter(3, 4);
            yield return null;

            Assert.AreEqual("Step 3 of 4", Child<TMP_Text>(panel, "StepCard/Counter").text);
            Assert.AreEqual("Button_Primary_Default", Child<UnityEngine.UI.Image>(panel, "StepCard/Segments/Segment 3").sprite.name);
            Assert.AreEqual("Status_Pill_Default", Child<UnityEngine.UI.Image>(panel, "StepCard/Segments/Segment 4").sprite.name);
            Assert.IsNull(panel.GetComponentInChildren<UnityEngine.UI.Button>(true),
                          "Mounting is mounted-input only: no free-hand button.");
        }

        [UnityTest]
        public IEnumerator Debrief_RepairStripHighlightsOnlyTheCurrentStage()
        {
            GameObject panel = Open("PF_Module02_UI", "Debrief");
            yield return null;

            Child<ProgressView>(panel, "RepairSequence").SetStep(3);
            Child<CardView>(panel, "BeatCard").SetCounter(2, 4);
            yield return null;

            Assert.AreEqual(FontStyles.Bold, Child<TMP_Text>(panel, "RepairSequence/Stage · Fades").fontStyle);
            Assert.AreEqual(FontStyles.Normal, Child<TMP_Text>(panel, "RepairSequence/Stage · Aid").fontStyle);
            Assert.AreEqual("Beat 2 of 4", Child<TMP_Text>(panel, "BeatCard/Counter").text);
        }

        #endregion
    }
}
#endif
