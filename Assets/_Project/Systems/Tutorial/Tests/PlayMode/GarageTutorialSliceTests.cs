using System.Collections;
using System.Collections.Generic;
using Cachacos;
using NUnit.Framework;
using RideSafe.TaskSequence;
using RideSafe.Tutorial.AutoHand;
using RideSafe.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using EntityId = RideSafe.TaskSequence.EntityId;

namespace RideSafe.Tutorial.Tests
{
    /// <summary>
    /// The Sprint 1 onboarding inside the real RideSafe_Garaje scene:
    /// tracking (hidden) -> Interaction Gate -> Language -> Location -> Vehicle -> Comfort.
    /// <para>
    /// No headset here, so the only hardware step (hidden tracking wait) is completed
    /// through the runner. Every choice goes through real EventSystem events and real
    /// Toggle/Button changes, which is the path AutoHand's input module drives on device.
    /// </para>
    /// </summary>
    public class GarageTutorialSliceTests
    {
        private const string SceneName = "RideSafe_Garaje";
        private readonly List<string> _moduleProblems = new List<string>();

        private TaskSequenceService _service;
        private TaskSequenceRunner _runner;
        private TutorialSession _session;
        private TMP_Text _instruction;
        private GameObject _highlight;
        private ControllerHintView _hint;
        private ModuleUI _module;

        private static readonly ActionId PrimarySelect = new ActionId(ActionIds.PrimarySelect);
        private static readonly ActionId Confirm = new ActionId(ActionIds.Confirm);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // No XR runtime in batch mode: AutoHand/OpenXR may log errors that are not ours.
            // Our own warnings/errors are collected and asserted separately.
            LogAssert.ignoreFailingMessages = true;
            _moduleProblems.Clear();
            Application.logMessageReceived += CollectModuleProblems;

            SceneManager.LoadScene(SceneName);
            yield return null;

            // Without a headset AutoHandPlayer's body has no real head to follow and drifts
            // (it keeps stepping its tracking space up). It is AutoHand's locomotion, not what
            // these tests cover, so freeze it: the head then stays where the rig spawned it.
            foreach (Autohand.AutoHandPlayer player in Object.FindObjectsByType<Autohand.AutoHandPlayer>(FindObjectsInactive.Exclude))
            {
                player.enabled = false;
                player.GetComponent<Rigidbody>().isKinematic = true;
            }
            yield return null;

            _service = Object.FindAnyObjectByType<TaskSequenceService>();
            Assert.IsNotNull(_service, "TaskSequenceService missing from the garage");
            _runner = _service.Runner;
            _session = Object.FindAnyObjectByType<TutorialSession>();
            Transform root = GameObject.Find("Tutorial").transform;
            _instruction = root.GetComponentInChildren<TextMeshPro>(true);
            _highlight = root.Find("Tutorial Highlight").gameObject;
            _hint = root.Find("Tutorial Controller Hint").GetComponent<ControllerHintView>();
            _module = root.GetComponentInChildren<ModuleUI>(true);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Application.logMessageReceived -= CollectModuleProblems;
            LogAssert.ignoreFailingMessages = false;
            // I2 remembers the language in PlayerPrefs; leave the editor as we found it.
            ServiceLocator.Instance.RequestService<ILocalizationProvider>()?.SetLanguage("en");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Onboarding_TeachesJustInTime_FirstUseThenReminders()
        {
            TutorialProgress progress = ServiceLocator.Instance.RequestService<TutorialProgress>();
            GameObject gate = GameObject.Find("Tutorial").transform.Find("Interaction Gate").gameObject;
            Transform head = Camera.main.transform;

            // Tracking is waited for internally: nothing is shown or placed yet.
            Assert.AreEqual("Initial.TrackingReady", _runner.SequenceId);
            Assert.IsFalse(gate.activeSelf, "the gate waits for tracked controllers");
            Assert.IsFalse(_instruction.gameObject.activeSelf);
            _runner.CompleteCurrentStep();

            // Gate appears in front of the HEAD, whatever the floor or eye height.
            yield return ExpectStep("gate.point");
            Assert.IsTrue(gate.activeSelf);
            AssertInFrontOfHead(gate.transform, head, 1.1f, -0.12f);
            PointerRayAffordance affordance = gate.GetComponentInChildren<PointerRayAffordance>();
            yield return null;
            Assert.IsTrue(affordance.IsShowing, "the first Point is visible before AutoHand's own ray exists");

            GameObject target = Entity("gate.target").gameObject;
            ExecuteEvents.Execute(target, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);

            // FIRST USE of PrimarySelect: controller from the very start, no text.
            yield return ExpectStep("gate.select");
            Assert.IsFalse(progress.IsActionLearned(PrimarySelect));
            Assert.IsTrue(_hint.IsVisible, "first use shows the controller at once");
            Assert.IsFalse(_instruction.gameObject.activeSelf, "the gate does not depend on text");

            target.GetComponent<Toggle>().isOn = true;
            yield return null;
            Assert.IsTrue(_hint.IsReleasing, "the hint shows the press, then leaves");
            yield return new WaitForSeconds(0.8f);
            Assert.IsFalse(_hint.IsVisible, "help gone ~0.5 s after the action");
            Assert.IsTrue(progress.IsActionLearned(PrimarySelect));
            Assert.IsFalse(gate.activeInHierarchy, "the gate and its pointing affordance leave the world");
            Assert.AreEqual("Language", _module.Current);
            AssertInFrontOfHead(_module.transform.parent, head, 2.1f, -0.1f);

            // Language: PrimarySelect is known -> REMINDER ladder, nothing up front.
            yield return ExpectStep("language.choose");
            Assert.IsFalse(_hint.IsVisible, "no re-teaching of a learned control");
            Assert.IsFalse(_highlight.activeSelf);
            yield return new WaitForSeconds(3.1f);
            Assert.IsTrue(_highlight.activeSelf, "reminder highlight after ~2.75 s");
            Assert.IsFalse(_hint.IsVisible);
            yield return new WaitForSeconds(2.2f);
            Assert.IsTrue(_hint.IsVisible, "controller reminder after ~5 s");

            _module.Get<ChoicePanel>("Language").Select("es");
            yield return null;
            Assert.AreEqual("es", _session.Context.Language);
            yield return ExpectStep("language.continue");
            yield return Click("onboarding.language.continue");

            // Location: the instruction AND the panel itself are now Spanish.
            yield return ExpectStep("location.choose");
            Assert.AreEqual("Jurisdiction", _module.Current);
            Assert.AreEqual(Translate("Tutorial/ChooseLocation"), _instruction.text);
            AssertPanelTitle("Jurisdiction", "M0/Location_Title", "Which rules apply to your riding?");
            _module.Get<ChoicePanel>("Jurisdiction").Select("bogota");
            yield return ExpectStep("location.continue");
            yield return Click("onboarding.location.continue");
            Assert.AreEqual("bogota", _session.Context.Jurisdiction);

            // Vehicle: choose, then the visible Confirm button with the SAME PrimarySelect.
            yield return ExpectStep("vehicle.choose");
            AssertPanelTitle("Vehicle", "M0/Vehicle_Title", "Choose your vehicle");
            _module.Get<ChoicePanel>("Vehicle").Select("ebike");
            yield return ExpectStep("vehicle.confirm");
            TMP_Text sentence = FindText("Vehicle", "SelectedText");
            Assert.AreEqual(string.Format(Translate("M0/Vehicle_SelectedFormat"), Translate("M0/Vehicle_EBike")), sentence.text);
            Assert.AreEqual(Translate("Tutorial/ConfirmVehicle"), _instruction.text);
            // Let the Language reminder finish its short "pressed" exit, still well before 2.75 s.
            yield return new WaitForSeconds(0.6f);
            Assert.IsFalse(_hint.IsVisible, "Confirm uses PrimarySelect: no new control is introduced");

            yield return Click("onboarding.vehicle.confirm");
            yield return null;
            Assert.AreEqual("ebike", _session.Context.Vehicle);
            Assert.AreEqual("Comfort", _module.Current);
            AssertPanelTitle("Comfort", "M0/Comfort_Title", "Set up your comfort");
            Assert.AreEqual(TaskSequenceStatus.Completed, _runner.Status);
            Assert.IsFalse(progress.IsActionLearned(Confirm), "no separate Confirm input was taught in M0");

            CollectionAssert.IsEmpty(_moduleProblems, string.Join("\n", _moduleProblems));
        }

        [UnityTest]
        public IEnumerator Abort_ClearsHelp_AndRestartUsesReminderForLearnedActions()
        {
            _runner.CompleteCurrentStep(); // hidden tracking
            yield return ExpectStep("gate.point");
            _runner.CompleteCurrentStep();
            yield return ExpectStep("gate.select");
            Assert.IsTrue(_hint.IsVisible);

            _session.Abort();
            yield return null;
            Assert.AreEqual(TaskSequenceStatus.Aborted, _runner.Status);
            Assert.AreEqual(TaskStepPhase.None, _runner.Phase);
            Assert.IsFalse(_hint.IsVisible, "abort clears every piece of help");
            Assert.IsFalse(_highlight.activeSelf);

            // A late action after abort reaches no stale validator.
            Entity("gate.target").GetComponent<Toggle>().isOn = true;
            yield return null;
            Assert.AreEqual(TaskSequenceStatus.Aborted, _runner.Status);
            Entity("gate.target").GetComponent<Toggle>().isOn = false;

            // Learned in this session -> the restarted gate only reminds.
            ServiceLocator.Instance.RequestService<TutorialProgress>().MarkActionLearned(PrimarySelect);
            _session.Restart();
            yield return ExpectStep("tracking.controllers");
            _runner.CompleteCurrentStep();
            yield return ExpectStep("gate.point");
            _runner.CompleteCurrentStep();
            yield return ExpectStep("gate.select");
            Assert.IsFalse(_hint.IsVisible, "a learned action is not re-taught up front");

            CollectionAssert.IsEmpty(_moduleProblems, string.Join("\n", _moduleProblems));
        }

        private void AssertPanelTitle(string panel, string term, string english)
        {
            TMP_Text title = FindText(panel, "Title");
            Assert.AreEqual(Translate(term), title.text, panel + " title is localized");
            Assert.AreNotEqual(english, title.text, panel + " title is no longer English");
        }

        private TMP_Text FindText(string panel, string objectName)
        {
            foreach (TMP_Text text in _module.Get<RectTransform>(panel).GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name == objectName)
                    return text;
            }
            Assert.Fail(panel + " has no text '" + objectName + "'");
            return null;
        }

        private static void AssertInFrontOfHead(Transform placed, Transform head, float distance, float verticalOffset)
        {
            Vector3 flat = Vector3.ProjectOnPlane(placed.position - head.position, Vector3.up);
            Assert.That(flat.magnitude, Is.EqualTo(distance).Within(0.02f), placed.name + " distance from head");
            Assert.That(placed.position.y - head.position.y, Is.EqualTo(verticalOffset).Within(0.02f), placed.name + " height from eyes");
        }

        private IEnumerator ExpectStep(string stepId)
        {
            float waited = 0f;
            while (_runner.StepId != stepId && waited < 3f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual(stepId, _runner.StepId, "expected step");
            yield return null; // Present -> WaitForInteraction
        }

        private IEnumerator Click(string entityId)
        {
            ExecuteEvents.Execute(Entity(entityId).gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            yield return null;
        }

        private AutoHandUITaskEntity Entity(string id)
        {
            Assert.IsTrue(_service.Entities.TryGetEntity(new EntityId(id), out AutoHandUITaskEntity entity), id);
            return entity;
        }

        private static string Translate(string key) =>
            ServiceLocator.Instance.RequestService<ILocalizationProvider>().GetTranslation(key);

        private void CollectModuleProblems(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Log)
                return;
            if (message.Contains("[Tutorial]") || message.Contains("[TaskSequence]") || message.Contains("[Services]") ||
                message.Contains("[RideSafe.UI]") ||
                stackTrace.Contains("RideSafe.Tutorial") || stackTrace.Contains("RideSafe.TaskSequence") ||
                stackTrace.Contains("RideSafe.UI"))
                _moduleProblems.Add(type + ": " + message);
        }
    }
}
