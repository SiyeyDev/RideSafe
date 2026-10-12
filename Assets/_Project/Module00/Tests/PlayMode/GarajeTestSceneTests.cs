using System.Collections;
using Cachacos;
using NUnit.Framework;
using RideSafe.TaskSequence;
using RideSafe.Tutorial;
using RideSafe.Tutorial.AutoHand;
using RideSafe.Tutorial.Presentation;
using RideSafe.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RideSafe.Module00.Tests
{
    /// <summary>
    /// RideSafe_Garaje_Test as wired by Module00TestSceneBuilder: Point -> Select with the 3D
    /// Touch Plus hint, and the vehicle preview. No headset, so the hidden tracking step is
    /// completed through the runner and the learner's actions are real EventSystem / Toggle
    /// changes, the path AutoHand's input module drives on device.
    /// </summary>
    public class GarajeTestSceneTests
    {
        private const string ScenePath = "Assets/_Project/Scene/RideSafe_Garaje_Test.unity";

        private TaskSequenceRunner _runner;
        private Transform _tutorial;
        private TMP_Text _instruction;
        private ControllerHint3DView _hint3D;
        private ControllerHintView _hint2D;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LogAssert.ignoreFailingMessages = true; // No XR runtime here: AutoHand/OpenXR logs are not ours.
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#endif
            yield return null;
            foreach (Autohand.AutoHandPlayer player in Object.FindObjectsByType<Autohand.AutoHandPlayer>(FindObjectsInactive.Exclude))
            {
                player.enabled = false;
                player.GetComponent<Rigidbody>().isKinematic = true;
            }
            yield return null;

            _runner = Object.FindAnyObjectByType<TaskSequenceService>().Runner;
            _tutorial = GameObject.Find("Tutorial").transform;
            _instruction = _tutorial.GetComponentInChildren<TextMeshPro>(true);
            _hint3D = _tutorial.GetComponentInChildren<ControllerHint3DView>(true);
            _hint2D = _tutorial.GetComponentInChildren<ControllerHintView>(true);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            yield return null;
        }

        [UnityTest]
        public IEnumerator PointThenSelect_Shows3DTrigger_AndOnlyTheRealActionCompletes()
        {
            Assert.IsNotNull(_hint3D, "the test scene has no 3D hint; run the scene builder");
            // The scene is saved in PC mode: DesktopTutorialInputSimulator reports tracked controllers,
            // so the hidden tracking wait passes by itself. Complete it only if it has not yet.
            if (_runner.StepId == "tracking.controllers")
                _runner.CompleteCurrentStep();

            // POINT: instruction only, no controller (pointing is not a button).
            yield return ExpectStep("gate.point");
            Assert.AreEqual("Module00.PointSelect", _runner.SequenceId);
            Assert.IsTrue(_instruction.gameObject.activeInHierarchy, "the learner must actually see the instruction");
            Assert.AreEqual(Translate("Tutorial/Point"), _instruction.text);
            GameObject gate = _tutorial.Find("Interaction Gate").gameObject;
            Assert.Less(Vector3.Distance(gate.transform.position, _instruction.transform.position), 0.6f,
                "the instruction sits with the target, not wherever its old parent was");
            Assert.IsFalse(_hint3D.IsVisible);

            GameObject target = Entity("gate.target").gameObject;
            ExecuteEvents.Execute(target, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);

            // SELECT, first use: right Touch Plus with its trigger lit, the 2D drawing stays hidden.
            yield return ExpectStep("gate.select");
            Assert.AreEqual(Translate("Tutorial/Select"), _instruction.text);
            Assert.IsTrue(_instruction.gameObject.activeInHierarchy);
            Assert.IsTrue(_hint3D.IsVisible, "first use shows the 3D controller at once");
            Assert.IsFalse(_hint2D.IsVisible, "the 2D drawing is only the fallback");
            ControllerModelVisual right = Model(XRNodeRole.RightHand);
            Assert.IsTrue(right.gameObject.activeSelf);
            Assert.IsFalse(Model(XRNodeRole.LeftHand).gameObject.activeSelf);
            ControllerButtonVisual trigger;
            Assert.IsTrue(right.TryGetButton(ControllerControl.Trigger, out trigger));
            yield return null;
            Assert.IsTrue(trigger.GetComponent<Renderer>().HasPropertyBlock(), "the trigger glows");

            // The demonstration loops and never completes the step on its own.
            yield return new WaitForSeconds(2.5f);
            Assert.AreEqual("gate.select", _runner.StepId);
            Assert.AreEqual(TaskStepPhase.WaitForInteraction, _runner.Phase);
            Assert.IsTrue(_hint3D.IsVisible);

            // The learner's real selection completes it; the hint shows the press and leaves.
            target.GetComponent<Toggle>().isOn = true;
            yield return null;
            Assert.IsTrue(_hint3D.IsReleasing);
            yield return new WaitForSeconds(0.8f);
            Assert.IsFalse(_hint3D.IsVisible);
            Assert.IsFalse(trigger.GetComponent<Renderer>().HasPropertyBlock(), "highlight cleared");
            Assert.IsTrue(ServiceLocator.Instance.RequestService<TutorialProgress>()
                .IsActionLearned(new ActionId(ActionIds.PrimarySelect)));
        }

        [UnityTest]
        public IEnumerator OnlyTheTutorialModule00UI_IsActive()
        {
            yield return null;
            int active = 0;
            foreach (ModuleUI module in Object.FindObjectsByType<ModuleUI>(FindObjectsInactive.Exclude))
            {
                if (module.GetComponent<RectTransform>() != null && module.GetComponentInChildren<ChoicePanel>(true) != null &&
                    !module.transform.IsChildOf(_tutorial))
                    active++;
            }
            Assert.AreEqual(0, active, "a second Module 0 UI competes with the tutorial's onboarding");
        }

        [UnityTest]
        public IEnumerator VehicleChoice_PresentsOneVehicleAtATime_AndRestoresTheOther()
        {
            VehiclePreviewController preview = Object.FindAnyObjectByType<VehiclePreviewController>();
            Assert.IsNotNull(preview, "the test scene has no vehicle preview; run the scene builder");
            Transform ebike = GameObject.Find("ebike").transform;
            Vector3 ebikeRest = ebike.localPosition;
            Quaternion ebikeRestRotation = ebike.localRotation;
            ChoicePanel vehiclePanel = _tutorial.GetComponentInChildren<ModuleUI>(true).Get<ChoicePanel>("Vehicle");

            Assert.IsFalse(preview.IsPlatformShown("ebike"));

            vehiclePanel.onSelectionChanged.Invoke("ebike");
            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(preview.IsRotating("ebike"));
            Assert.IsTrue(preview.IsPlatformShown("ebike"));
            Assert.Greater(Quaternion.Angle(ebikeRestRotation, ebike.localRotation), 1f);

            vehiclePanel.onSelectionChanged.Invoke("escooter");
            yield return null;
            Assert.IsFalse(preview.IsRotating("ebike"));
            Assert.IsTrue(preview.IsRotating("escooter"));
            yield return new WaitForSeconds(1.2f);
            Assert.Less(Vector3.Distance(ebikeRest, ebike.localPosition), 1e-4f);
            Assert.Less(Quaternion.Angle(ebikeRestRotation, ebike.localRotation), 0.05f);
            Assert.IsTrue(preview.IsPlatformShown("escooter"));
            Assert.IsFalse(preview.IsPlatformShown("ebike"));
        }

        private ControllerModelVisual Model(XRNodeRole hand)
        {
            foreach (ControllerModelVisual model in _hint3D.GetComponentsInChildren<ControllerModelVisual>(true))
            {
                if (model.Hand == hand)
                    return model;
            }
            Assert.Fail("no " + hand + " model");
            return null;
        }

        private IEnumerator ExpectStep(string stepId)
        {
            for (int frame = 0; frame < 120 && !(_runner.StepId == stepId && _runner.Phase == TaskStepPhase.WaitForInteraction); frame++)
                yield return null;
            Assert.AreEqual(stepId, _runner.StepId);
            Assert.AreEqual(TaskStepPhase.WaitForInteraction, _runner.Phase);
        }

        private static Transform Entity(string id)
        {
            ITaskEntity entity;
            Assert.IsTrue(ServiceLocator.Instance.RequestService<ITaskSequenceService>().Entities
                .TryGetEntity(new RideSafe.TaskSequence.EntityId(id), out entity), id);
            return entity.Transform;
        }

        private static string Translate(string key)
        {
            string text = ServiceLocator.Instance.RequestService<ILocalizationProvider>()?.GetTranslation(key);
            return string.IsNullOrEmpty(text) ? key : text;
        }
    }
}
