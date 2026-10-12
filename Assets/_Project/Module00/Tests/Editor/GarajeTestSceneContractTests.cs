using System.Reflection;
using NUnit.Framework;
using RideSafe.Demo;
using RideSafe.TaskSequence;
using RideSafe.Tutorial;
using RideSafe.Tutorial.AutoHand;
using RideSafe.Tutorial.Presentation;
using RideSafe.UI;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RideSafe.Module00.Tests
{
    /// <summary>
    /// What Module00TestSceneBuilder must leave in RideSafe_Garaje_Test, checked on the saved
    /// scene without playing it. Visual quality is judged in Play; this guards the wiring.
    /// </summary>
    public class GarajeTestSceneContractTests
    {
        private const string ScenePath = "Assets/_Project/Scene/RideSafe_Garaje_Test.unity";
        private Scene _scene;
        private bool _openedHere;

        [OneTimeSetUp]
        public void Open()
        {
            _scene = SceneManager.GetSceneByPath(ScenePath);
            _openedHere = !_scene.isLoaded;
            if (_openedHere)
                _scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        [OneTimeTearDown]
        public void Close()
        {
            if (_openedHere)
                EditorSceneManager.CloseScene(_scene, true);
        }

        [Test]
        public void InstructionLabel_IsNotInsideAParentHiddenDuringTheTutorial()
        {
            TutorialPresenter presenter = Find<TutorialPresenter>();
            TMP_Text label = Field<TMP_Text>(presenter, "_instructionLabel");
            Transform tutorial = Root("Tutorial").transform;

            Assert.AreEqual(tutorial, label.transform.parent, "the label hangs from Tutorial, which is active");
            Assert.IsTrue(tutorial.gameObject.activeSelf);
            Assert.IsTrue(Field<bool>(presenter, "_instructionAboveTarget"));
        }

        [Test]
        public void OnlyTheTutorialsModule00UI_IsActive()
        {
            GameObject legacy = Root("PF_Module00_UI");
            Assert.IsTrue(legacy == null || !legacy.activeSelf, "the standalone PF_Module00_UI competes with the tutorial");
        }

        [Test]
        public void DesktopMode_IsTheExistingPcMode_PlusOnlyTheInputSimulator()
        {
            GameObject desktop = Root("DesktopMode");
            Assert.IsTrue(desktop.activeSelf, "saved in PC mode");
            DesktopTutorialInputSimulator simulator = desktop.GetComponent<DesktopTutorialInputSimulator>();
            Assert.IsNotNull(simulator);
            Assert.AreEqual(desktop.GetComponentInChildren<Camera>(true), Field<Camera>(simulator, "_desktopCamera"));
            Assert.AreEqual(1, Object.FindObjectsByType<DesktopTutorialInputSimulator>(FindObjectsInactive.Include).Length);
        }

        [Test]
        public void DemoModeMenu_FindsExactlyOneXRPlayer()
        {
            int count = 0;
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                if (root.name == "XRPlayer")
                    count++;
            }
            Assert.AreEqual(1, count, "RideSafe/Demo/Modo VR finds the rig by this name");
        }

        [Test]
        public void Session_RunsTheTestSequences_NotTheProductionOnes()
        {
            TutorialSession session = Find<TutorialSession>();
            System.Collections.IList stages = Field<System.Collections.IList>(session, "_stages");
            string ids = "";
            foreach (TutorialSession.Stage stage in stages)
                ids += stage.Sequence.SequenceId + ";";

            StringAssert.Contains("Module00.PointSelect;", ids);
            StringAssert.Contains("Module00.Onboarding.Test;", ids);
            StringAssert.DoesNotContain("Module00.Onboarding;", ids);
        }

        [Test]
        public void TestOnboarding_StepsDriveTheFlow_WithoutRepeatingTheScreenAsText()
        {
            TutorialSession session = Find<TutorialSession>();
            System.Collections.IList stages = Field<System.Collections.IList>(session, "_stages");
            TaskSequenceSO onboarding = ((TutorialSession.Stage)stages[stages.Count - 1]).Sequence;

            string[] expected =
            {
                "location.point", "location.choose", "location.continue",
                "language.choose", "language.continue", "vehicle.choose", "vehicle.confirm"
            };
            Assert.AreEqual(expected.Length, onboarding.Steps.Count);
            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], onboarding.Steps[i].StepId, "approved order: 01A location, 01B language, 01C vehicle");
            Assert.AreEqual("onboarding.location.options", onboarding.Steps[0].EntityId.Value, "point at the first screen");
            Assert.AreEqual(ActionIds.PrimarySelect, onboarding.Steps[1].HintActionId, "the 3D trigger is the reminder");
            foreach (TaskStepData step in onboarding.Steps)
            {
                Assert.IsTrue(string.IsNullOrEmpty(step.InstructionKey), step.StepId + " repeats the screen as text");
                Assert.IsNotNull(step.Validator, step.StepId + " still waits for the learner");
            }
        }

        [Test]
        public void Binding_OpensLocationFirst()
        {
            Assert.IsTrue(Field<bool>(Find<Module00TutorialBinding>(), "_locationFirst"));
        }

        [Test]
        public void GuidedPanels_FollowTheApprovedTypeScale()
        {
            ModuleUI module = Root("Tutorial").transform.Find("Onboarding").GetComponentInChildren<ModuleUI>(true);
            foreach (string panel in new[] { Module00TutorialBinding.LanguagePanel, Module00TutorialBinding.LocationPanel, Module00TutorialBinding.VehiclePanel })
            {
                Transform root = module.Get<RectTransform>(panel);
                TMP_Text screenTitle = root.Find("Header/Title").GetComponent<TMP_Text>();
                Assert.AreEqual("Manrope-Bold SDF", screenTitle.font.name, panel + " H1");
                Assert.That(screenTitle.fontSize, Is.InRange(34f, 40f), panel + " H1 size");
                foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    string where = panel + "/" + label.transform.parent.name + "/" + label.name;
                    if (label == screenTitle)
                        continue;
                    if (label.name == "Title")
                        Assert.AreEqual("Manrope-SemiBold SDF", label.font.name, where + " H2");
                    else if (label.name == "Body" || label.name == "Description" || label.name == "SelectedText")
                        Assert.AreEqual("Inter-Regular SDF", label.font.name, where);
                    else if (label.name == "Tag" || label.name == "StateCaption" || label.name == "Label" || label.name == "Prompt")
                        Assert.AreEqual("Inter-SemiBold SDF", label.font.name, where);
                    Assert.Less(label.fontSize, screenTitle.fontSize, where + " competes with the screen title");
                }
            }
        }

        [Test]
        public void Hint3D_HasBothTouchPlusModels_WithPoses()
        {
            ControllerHint3DView view = Find<ControllerHint3DView>();
            ControllerModelVisual right = Field<ControllerModelVisual>(view, "_right");
            ControllerModelVisual left = Field<ControllerModelVisual>(view, "_left");
            Assert.AreEqual(XRNodeRole.RightHand, right.Hand);
            Assert.AreEqual(XRNodeRole.LeftHand, left.Hand);
            Assert.AreNotEqual(right.PoseFor(ControllerControl.Trigger).rotation, right.PoseFor(ControllerControl.PrimaryButton).rotation,
                "trigger and face buttons are shown from different sides");
        }

        [Test]
        public void Visuals_UseRideSafeAtlasSprites_DrawnSliced()
        {
            TutorialPresenter presenter = Find<TutorialPresenter>();
            TMP_Text label = Field<TMP_Text>(presenter, "_instructionLabel");
            SpriteRenderer plate = label.transform.Find("Backplate").GetComponent<SpriteRenderer>();
            Assert.AreEqual("Panel_Instruction_Wide", plate.sprite.name);
            Assert.AreEqual(SpriteDrawMode.Sliced, plate.drawMode);
            Assert.AreEqual(new Color32(0x00, 0x25, 0x54, 0xFF), (Color32)label.color, "H2 BeamNavy text");

            ControllerHint3DView view = Find<ControllerHint3DView>();
            SpriteRenderer backdrop = view.transform.Find("Backdrop").GetComponent<SpriteRenderer>();
            Assert.AreEqual("Panel_Dark_Base", backdrop.sprite.name);
            Assert.AreEqual(SpriteDrawMode.Sliced, backdrop.drawMode);
            Assert.AreEqual(backdrop.transform.localScale.x, backdrop.transform.localScale.y, 1e-5f);
            Assert.AreEqual(new Color32(0xA9, 0xE2, 0xD4, 0xFF), (Color32)Field<Color>(view, "_highlightColor"), "mint, not coral");

            MonoBehaviour ring = Field<MonoBehaviour>(presenter, "_targetHighlight");
            Assert.IsTrue(ring is ITargetHighlightView);
            Assert.AreEqual("Focus_Ring_Mint", ring.GetComponent<SpriteRenderer>().sprite.name);
            Assert.AreEqual(SpriteDrawMode.Sliced, ring.GetComponent<SpriteRenderer>().drawMode);
        }

        [Test]
        public void GuidedPanels_KeepTheirHeader_AndLanguageUsesTheApprovedComposition()
        {
            ModuleUI module = Root("Tutorial").transform.Find("Onboarding").GetComponentInChildren<ModuleUI>(true);
            foreach (string panel in new[] { Module00TutorialBinding.LanguagePanel, Module00TutorialBinding.LocationPanel, Module00TutorialBinding.VehiclePanel })
                Assert.IsTrue(module.Get<RectTransform>(panel).Find("Header").gameObject.activeSelf, panel + " lost its own title");

            Transform language = module.Get<RectTransform>(Module00TutorialBinding.LanguagePanel);
            TMP_Text title = language.Find("Header/Title").GetComponent<TMP_Text>();
            Assert.AreEqual("Choose your language / Elige tu idioma", title.text);
            foreach (Component c in title.GetComponents<Component>())
                Assert.AreNotEqual("Localize", c.GetType().Name, "I2 would overwrite the bilingual title");
            Assert.IsFalse(language.Find("Header/Body").gameObject.activeSelf, "no English-only subtitle before a language exists");

            AssertCard(language.Find("Options/Option_en"), "EN", "English", "Captions and narration in English.");
            AssertCard(language.Find("Options/Option_es"), "ES", "Español", "Subtítulos y narración en español.");
        }

        private static void AssertCard(Transform card, string tag, string name, string description)
        {
            Assert.AreEqual(tag, card.Find("Tag").GetComponent<TMP_Text>().text);
            Assert.AreEqual(name, card.Find("Title").GetComponent<TMP_Text>().text);
            Assert.AreEqual(description, card.Find("Description").GetComponent<TMP_Text>().text);
        }

        [Test]
        public void PointSelect_KeepsTheTargetHighlight()
        {
            TutorialSession session = Find<TutorialSession>();
            System.Collections.IList stages = Field<System.Collections.IList>(session, "_stages");
            foreach (TutorialSession.Stage stage in stages)
            {
                if (stage.Sequence.SequenceId != "Module00.PointSelect")
                    continue;
                foreach (TaskStepData step in stage.Sequence.Steps)
                    Assert.AreEqual(TargetHighlightMode.Show, step.TargetHighlight, step.StepId);
                return;
            }
            Assert.Fail("no Point/Select stage");
        }

        [Test]
        public void Presenter_DoesNotRingUIControls_ByDefault()
        {
            Assert.IsFalse(Field<bool>(Find<TutorialPresenter>(), "_highlightSelfStyledUI"));
        }

        private T Find<T>() where T : Component
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }
            Assert.Fail(typeof(T).Name + " not found in the test scene");
            return null;
        }

        private GameObject Root(string name)
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                if (root.name == name)
                    return root;
            }
            return null;
        }

        private static T Field<T>(object target, string name)
        {
            FieldInfo info = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, name);
            return (T)info.GetValue(target);
        }
    }
}
