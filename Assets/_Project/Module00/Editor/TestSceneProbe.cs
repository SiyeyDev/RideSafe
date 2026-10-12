using System.IO;
using System.Reflection;
using System.Text;
using RideSafe.TaskSequence;
using RideSafe.Tutorial;
using RideSafe.Tutorial.AutoHand;
using RideSafe.Tutorial.Presentation;
using RideSafe.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RideSafe.Module00.Editor
{
    /// <summary>
    /// Editor-only diagnostics for RideSafe_Garaje_Test while playing: dumps what the tutorial
    /// is doing and what is visible to <see cref="OutputPath"/>. Read-only except the explicit
    /// "act" helpers, which perform the same EventSystem / Toggle actions the mouse would.
    /// </summary>
    public static class TestSceneProbe
    {
        public const string OutputPath = "Temp/ridesafe-probe.txt";

        public static void Dump()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("playing=" + Application.isPlaying + " paused=" + UnityEditor.EditorApplication.isPaused +
                          " frame=" + Time.frameCount + " t=" + Time.time.ToString("F1"));

            TaskSequenceService service = Object.FindAnyObjectByType<TaskSequenceService>();
            TaskSequenceRunner runner = service != null ? service.Runner : null;
            sb.AppendLine(runner == null ? "runner=NULL"
                : "runner seq=" + runner.SequenceId + " step=" + runner.StepId + " phase=" + runner.Phase + " status=" + runner.Status);

            TutorialPresenter presenter = Object.FindAnyObjectByType<TutorialPresenter>(FindObjectsInactive.Include);
            if (presenter != null)
            {
                sb.AppendLine("presenter enabled=" + presenter.isActiveAndEnabled +
                              " input=" + Field(presenter, "_input") + " hint=" + Field(presenter, "_controllerHint") +
                              " fallback=" + Field(presenter, "_fallbackControllerHint") + " bindings=" + Field(presenter, "_bindings") +
                              " views=" + CountViews(presenter) + " activeHint=" + Field(presenter, "_activeHint") +
                              " level=" + Field(presenter, "_level") + " firstUse=" + Field(presenter, "_firstUse") +
                              " viewer=" + Field(presenter, "_viewer"));
                MonoBehaviour ring = Field(presenter, "_targetHighlight") as MonoBehaviour;
                sb.AppendLine("targetHighlight highlighted=" + Field(presenter, "_highlighted") +
                              " ringActive=" + (ring != null && ring.gameObject.activeInHierarchy));
                TMP_Text label = Field(presenter, "_instructionLabel") as TMP_Text;
                sb.AppendLine("instruction " + (label == null ? "NULL" : "active=" + label.gameObject.activeInHierarchy +
                              " text='" + label.text + "' pos=" + label.transform.position));
            }

            foreach (ControllerHint3DView view in Object.FindObjectsByType<ControllerHint3DView>(FindObjectsInactive.Include))
                sb.AppendLine("hint3D visible=" + view.IsVisible + " active=" + view.gameObject.activeInHierarchy + " pos=" + view.transform.position);
            foreach (ControllerHintView view in Object.FindObjectsByType<ControllerHintView>(FindObjectsInactive.Include))
                sb.AppendLine("hint2D visible=" + view.IsVisible + " pos=" + view.transform.position);

            Transform tutorial = GameObject.Find("Tutorial") != null ? GameObject.Find("Tutorial").transform : null;
            if (tutorial != null)
            {
                foreach (Transform child in tutorial)
                    sb.AppendLine("  Tutorial/" + child.name + " active=" + child.gameObject.activeInHierarchy + " pos=" + child.position);
                ModuleUI module = tutorial.GetComponentInChildren<ModuleUI>(true);
                if (module != null)
                    sb.AppendLine("onboarding module current=" + module.Current + " active=" + module.gameObject.activeInHierarchy);
            }

            foreach (ModuleUI module in Object.FindObjectsByType<ModuleUI>(FindObjectsInactive.Include))
                sb.AppendLine("ModuleUI " + Path(module.transform) + " active=" + module.gameObject.activeInHierarchy + " current=" + module.Current);

            foreach (Camera camera in Camera.allCameras)
                sb.AppendLine("camera " + Path(camera.transform) + " depth=" + camera.depth + " pos=" + camera.transform.position +
                              " fwd=" + camera.transform.forward + " main=" + (camera == Camera.main));
            sb.AppendLine("EventSystem current=" + (EventSystem.current != null ? Path(EventSystem.current.transform) +
                          " module=" + EventSystem.current.currentInputModule : "NULL"));

            if (service != null && service.Entities != null)
                sb.AppendLine("entities: " + service.Entities.DumpIds());

            VehiclePreviewController preview = Object.FindAnyObjectByType<VehiclePreviewController>();
            if (preview != null)
                sb.AppendLine("preview selected=" + preview.SelectedId + " ebikeRot=" + preview.IsRotating("ebike") +
                              " scooterRot=" + preview.IsRotating("escooter"));

            File.WriteAllText(OutputPath, sb.ToString());
        }

        public const string ShotPath = "Temp/ridesafe-shot.png";

        /// <summary>What the main camera sees right now, as a PNG at <see cref="ShotPath"/>.</summary>
        public static void Shot()
        {
            Camera camera = Camera.main;
            RenderTexture target = RenderTexture.GetTemporary(1280, 720, 24);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            Texture2D image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(target);
            File.WriteAllBytes(ShotPath, image.EncodeToPNG());
            Object.DestroyImmediate(image);
        }

        /// <summary>Hover the gate target, as the mouse would.</summary>
        public static void PointGate() => Act("gate.target", target =>
            ExecuteEvents.Execute(target, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler));

        /// <summary>Click the gate target's toggle, as the mouse would.</summary>
        public static void SelectGate() => Act("gate.target", target => target.GetComponent<Toggle>().isOn = true);

        public static void PointLanguage() => Act("onboarding.language.options", target =>
            ExecuteEvents.Execute(target, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler));
        public static void ChooseEnglish() => Choose("Language", "en");
        public static void ChooseSpanish() => Choose("Language", "es");
        public static void ChooseBogota() => Choose("Jurisdiction", "bogota");
        public static void ChooseEbike() => Choose("Vehicle", "ebike");
        public static void ChooseScooter() => Choose("Vehicle", "escooter");
        public static void ContinueLanguage() => Click("onboarding.language.continue");
        public static void ContinueLocation() => Click("onboarding.location.continue");

        public const string CardsPath = "Temp/ridesafe-cards.txt";

        // Card/button state checks: the same EventSystem events AutoHand's AutoInputModule
        // sends from its ray (enter/exit from HandlePointerExitAndEnter, down/up/click from
        // ProcessPress/ProcessRelease), minus the physical raycast.
        public static void HoverEnglish() => Enter("Option_en");
        public static void ExitEnglish() => Exit("Option_en");
        public static void PressEnglish()
        {
            GameObject target = Find("Language", "Option_en");
            Data().pointerPress = target;
            ExecuteEvents.Execute(target, Data(), ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, Data(), ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, Data(), ExecuteEvents.pointerClickHandler);
            Data().pointerPress = null;
        }
        public static void HoverContinue() => Enter("ContinueButton");
        public static void ExitContinue() => Exit("ContinueButton");

        private static PointerEventData _pointer;

        private static PointerEventData Data()
        {
            if (_pointer == null)
                _pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            return _pointer;
        }

        private static void Enter(string child)
        {
            GameObject target = Find("Language", child);
            if (!Data().hovered.Contains(target))
                Data().hovered.Add(target);
            ExecuteEvents.Execute(target, Data(), ExecuteEvents.pointerEnterHandler);
        }

        private static void Exit(string child)
        {
            GameObject target = Find("Language", child);
            Data().hovered.Remove(target);
            ExecuteEvents.Execute(target, Data(), ExecuteEvents.pointerExitHandler);
        }

        private static GameObject Find(string panel, string child)
        {
            ModuleUI module = GameObject.Find("Tutorial").GetComponentInChildren<ModuleUI>(true);
            foreach (Transform t in module.Get<RectTransform>(panel).GetComponentsInChildren<Transform>(true))
            {
                if (t.name == child)
                    return t.gameObject;
            }
            return null;
        }

        public static void PointLocation() => Act("onboarding.location.options", target =>
            ExecuteEvents.Execute(target, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler));

        /// <summary>Every visible text of the open onboarding panel: font, size, text. To CardsPath (appends).</summary>
        public static void Texts()
        {
            StringBuilder sb = new StringBuilder();
            ModuleUI module = GameObject.Find("Tutorial").GetComponentInChildren<ModuleUI>(true);
            foreach (string panel in new[] { "Language", "Jurisdiction", "Vehicle" })
            {
                RectTransform root = module.Get<RectTransform>(panel);
                if (!root.gameObject.activeInHierarchy)
                    continue;
                sb.AppendLine("--- texts " + panel + " frame " + Time.frameCount);
                foreach (TMPro.TMP_Text label in root.GetComponentsInChildren<TMPro.TMP_Text>(false))
                {
                    sb.AppendLine("  " + label.transform.parent.name + "/" + label.name + " font=" + Name(label.font) +
                                  " size=" + label.fontSize + " text='" + label.text.Replace("\n", " | ") + "'");
                }
            }
            File.AppendAllText(CardsPath, sb.ToString());
        }

        /// <summary>Visual state of the Language cards, Continue and the headers, to CardsPath (appends).</summary>
        public static void CardStates()
        {
            StringBuilder sb = new StringBuilder("--- frame " + Time.frameCount + System.Environment.NewLine);
            foreach (string child in new[] { "Option_en", "Option_es", "ContinueButton" })
            {
                GameObject go = Find("Language", child);
                Selectable selectable = go.GetComponent<Selectable>();
                Image image = (Image)selectable.targetGraphic;
                PropertyInfo state = typeof(Selectable).GetProperty("currentSelectionState", BindingFlags.Instance | BindingFlags.NonPublic);
                sb.AppendLine(child + " state=" + state.GetValue(selectable) +
                              " transition=" + selectable.transition +
                              " hoverSprite=" + Name(selectable.spriteState.highlightedSprite) +
                              " pressedSprite=" + Name(selectable.spriteState.pressedSprite) +
                              " sprite=" + Name(image.sprite) + " override=" + Name(image.overrideSprite) +
                              " isOn=" + (selectable is Toggle toggle ? toggle.isOn.ToString() : "-") +
                              " interactable=" + selectable.IsInteractable());
            }
            ModuleUI module = GameObject.Find("Tutorial").GetComponentInChildren<ModuleUI>(true);
            foreach (string panel in new[] { "Language", "Jurisdiction", "Vehicle", "Comfort" })
            {
                Transform header = module.Get<RectTransform>(panel).Find("Header");
                sb.AppendLine(panel + "/Header active=" + (header != null && header.gameObject.activeSelf));
            }
            File.AppendAllText(CardsPath, sb.ToString());
        }

        /// <summary>Vehicle option id -> toggle, its title and world position; preview id -> model. To CardsPath.</summary>
        public static void VehicleMapping()
        {
            StringBuilder sb = new StringBuilder("--- vehicle mapping" + System.Environment.NewLine);
            ModuleUI module = Object.FindObjectsByType<ModuleUI>(FindObjectsInactive.Include)[0];
            foreach (ModuleUI candidate in Object.FindObjectsByType<ModuleUI>(FindObjectsInactive.Include))
            {
                if (candidate.transform.root.name == "Tutorial")
                    module = candidate;
            }
            ChoicePanel panel = module.Get<ChoicePanel>("Vehicle");
            foreach (object option in (System.Collections.IList)typeof(ChoicePanel)
                         .GetField("_options", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(panel))
            {
                ChoicePanel.Option o = (ChoicePanel.Option)option;
                TMP_Text title = o.Toggle.transform.Find("Title").GetComponent<TMP_Text>();
                sb.AppendLine("option id=" + o.Id + " toggle=" + o.Toggle.name + " title='" + title.text +
                              "' worldX=" + o.Toggle.transform.position.x.ToString("F2"));
            }
            VehiclePreviewController preview = Object.FindAnyObjectByType<VehiclePreviewController>(FindObjectsInactive.Include);
            foreach (object vehicle in (System.Collections.IList)typeof(VehiclePreviewController)
                         .GetField("_vehicles", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(preview))
            {
                VehiclePreviewController.Vehicle v = (VehiclePreviewController.Vehicle)vehicle;
                Renderer r = v.Model.GetComponentInChildren<Renderer>();
                sb.AppendLine("preview id=" + v.Id + " model=" + v.Model.name + " modelX=" + v.Model.position.x.ToString("F2") +
                              " mesh=" + (r is MeshRenderer ? r.GetComponent<MeshFilter>().sharedMesh.name : r != null ? r.name : "none"));
            }
            File.AppendAllText(CardsPath, sb.ToString());
        }

        public static void CardGeometry()
        {
            StringBuilder sb = new StringBuilder("--- card geometry" + System.Environment.NewLine);
            Transform card = Find("Language", "Option_en").transform;
            foreach (RectTransform rt in card.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.parent != card && rt != card)
                    continue;
                sb.AppendLine(rt.name + " aMin=" + rt.anchorMin + " aMax=" + rt.anchorMax + " pivot=" + rt.pivot +
                              " pos=" + rt.anchoredPosition + " size=" + rt.sizeDelta + " rect=" + rt.rect +
                              (rt.GetComponent<TMP_Text>() is TMP_Text t ? " align=" + t.alignment + " fs=" + t.fontSize : ""));
            }
            UnityEngine.UI.Image image = card.GetComponent<UnityEngine.UI.Image>();
            sb.AppendLine("sprite border=" + image.sprite.border + " ppu=" + image.pixelsPerUnitMultiplier + " type=" + image.type);
            File.AppendAllText(CardsPath, sb.ToString());
        }

        private static string Name(Object o) => o == null ? "none" : o.name;

        private static void Choose(string panel, string option)
        {
            ModuleUI module = GameObject.Find("Tutorial").GetComponentInChildren<ModuleUI>(true);
            module.Get<ChoicePanel>(panel).Select(option);
            Dump();
        }

        private static void Click(string entityId) => Act(entityId, target =>
            ExecuteEvents.Execute(target, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler));

        private static void Act(string entityId, System.Action<GameObject> action)
        {
            ITaskEntity entity;
            TaskSequenceService service = Object.FindAnyObjectByType<TaskSequenceService>();
            if (service.Entities.TryGetEntity(new RideSafe.TaskSequence.EntityId(entityId), out entity))
                action(entity.Transform.gameObject);
            Dump();
        }

        private static int CountViews(TutorialPresenter presenter)
        {
            System.Collections.ICollection views = Field(presenter, "_hintViews") as System.Collections.ICollection;
            return views != null ? views.Count : -1;
        }

        private static object Field(object target, string name)
        {
            FieldInfo info = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            object value = info != null ? info.GetValue(target) : "<no field>";
            return value is Object unityObject && unityObject == null ? null : value;
        }

        private static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    }
}
