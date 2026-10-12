using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RideSafe.Tutorial.Presentation;
using UnityEngine;

namespace RideSafe.Tutorial.Tests
{
    /// <summary>Sets serialized private fields the way the Inspector would.</summary>
    internal static class SerializedFields
    {
        public static void Set(object target, string field, object value)
        {
            FieldInfo info = null;
            for (System.Type type = target.GetType(); type != null && info == null; type = type.BaseType)
                info = type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(info, target.GetType().Name + " has no field " + field);
            info.SetValue(target, value);
        }
    }

    /// <summary>A two-controller rig built in code: each model has trigger, grip and primary button.</summary>
    internal sealed class TouchPlusRig
    {
        private readonly List<Object> _owned = new List<Object>();

        public readonly ControllerHint3DView View;
        public readonly ControllerModelVisual Left;
        public readonly ControllerModelVisual Right;
        public readonly Material Material;
        public static readonly Color BaseColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        public static readonly Vector3 TriggerEuler = new Vector3(60f, 0f, 0f);
        public static readonly Vector3 GripEuler = new Vector3(0f, 90f, 0f);

        public TouchPlusRig()
        {
            Material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Material.SetColor("_BaseColor", BaseColor);
            _owned.Add(Material);

            GameObject root = new GameObject("Hint3D");
            _owned.Add(root);
            View = root.AddComponent<ControllerHint3DView>();
            Left = CreateModel(root.transform, XRNodeRole.LeftHand);
            Right = CreateModel(root.transform, XRNodeRole.RightHand);
            SerializedFields.Set(View, "_left", Left);
            SerializedFields.Set(View, "_right", Right);
            root.SetActive(false);
        }

        public ControllerButtonVisual Button(ControllerModelVisual model, ControllerControl control)
        {
            ControllerButtonVisual button;
            Assert.IsTrue(model.TryGetButton(control, out button), "rig lacks " + control);
            return button;
        }

        public void Destroy()
        {
            foreach (Object owned in _owned)
                Object.DestroyImmediate(owned);
        }

        private ControllerModelVisual CreateModel(Transform parent, XRNodeRole hand)
        {
            GameObject model = new GameObject(hand + " model");
            model.transform.SetParent(parent, false);
            ControllerModelVisual visual = model.AddComponent<ControllerModelVisual>();
            SerializedFields.Set(visual, "_hand", hand);

            AddButton(model.transform, ControllerControl.Trigger, new Vector3(0f, 0f, -1f));
            AddButton(model.transform, ControllerControl.Grip, new Vector3(1f, 0f, 0f));
            AddButton(model.transform, ControllerControl.PrimaryButton, new Vector3(0f, -1f, 0f));
            SerializedFields.Set(visual, "_defaultPose", new ControllerModelVisual.ControlPose
            {
                Control = ControllerControl.None, Euler = new Vector3(0f, 10f, 0f)
            });
            SerializedFields.Set(visual, "_poses", new List<ControllerModelVisual.ControlPose>
            {
                new ControllerModelVisual.ControlPose { Control = ControllerControl.Trigger, Euler = TriggerEuler, Offset = new Vector3(0f, 0.01f, 0f) },
                new ControllerModelVisual.ControlPose { Control = ControllerControl.Grip, Euler = GripEuler }
            });
            return visual;
        }

        private void AddButton(Transform parent, ControllerControl control, Vector3 pressDirection)
        {
            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.name = control.ToString();
            button.transform.SetParent(parent, false);
            button.transform.localPosition = new Vector3(0.1f, 0.2f, 0.3f);
            button.GetComponent<MeshRenderer>().sharedMaterial = Material;
            ControllerButtonVisual visual = button.AddComponent<ControllerButtonVisual>();
            SerializedFields.Set(visual, "_control", control);
            SerializedFields.Set(visual, "_pressDirection", pressDirection);
            SerializedFields.Set(visual, "_pressDistance", 0.004f);
            SerializedFields.Set(visual, "_pressSeconds", 0.2f);
        }
    }

    public class ControllerModelVisualTests
    {
        private TouchPlusRig _rig;

        [SetUp] public void SetUp() => _rig = new TouchPlusRig();
        [TearDown] public void TearDown() => _rig.Destroy();

        [Test]
        public void TryGetButton_FindsTheVisualConfiguredForThatControl()
        {
            ControllerButtonVisual trigger;
            Assert.IsTrue(_rig.Right.TryGetButton(ControllerControl.Trigger, out trigger));
            Assert.AreEqual(ControllerControl.Trigger, trigger.Control);
            Assert.AreEqual("Trigger", trigger.name);
        }

        [Test]
        public void TryGetButton_ControlTheModelDoesNotHave_ReturnsFalse()
        {
            ControllerButtonVisual missing;
            Assert.IsFalse(_rig.Right.TryGetButton(ControllerControl.Thumbstick, out missing));
            Assert.IsNull(missing);
        }

        [Test]
        public void Hand_IsTheConfiguredSide()
        {
            Assert.AreEqual(XRNodeRole.LeftHand, _rig.Left.Hand);
            Assert.AreEqual(XRNodeRole.RightHand, _rig.Right.Hand);
        }
    }

    public class ControllerHint3DViewTests
    {
        private TouchPlusRig _rig;
        private static readonly PhysicalBinding RightTrigger = new PhysicalBinding(XRNodeRole.RightHand, ControllerControl.Trigger);

        [SetUp] public void SetUp() => _rig = new TouchPlusRig();
        [TearDown] public void TearDown() => _rig.Destroy();

        [Test]
        public void Show_RightTrigger_ShowsOnlyTheRightController()
        {
            Assert.IsTrue(_rig.View.Show(RightTrigger));

            Assert.IsTrue(_rig.View.IsVisible);
            Assert.IsTrue(_rig.Right.gameObject.activeSelf);
            Assert.IsFalse(_rig.Left.gameObject.activeSelf);
        }

        [Test]
        public void Show_LeftBinding_ShowsOnlyTheLeftController()
        {
            Assert.IsTrue(_rig.View.Show(new PhysicalBinding(XRNodeRole.LeftHand, ControllerControl.PrimaryButton)));

            Assert.IsTrue(_rig.Left.gameObject.activeSelf);
            Assert.IsFalse(_rig.Right.gameObject.activeSelf);
        }

        [Test]
        public void Show_ControlWithoutVisual_ReturnsFalseAndStaysHidden()
        {
            Assert.IsFalse(_rig.View.Show(new PhysicalBinding(XRNodeRole.RightHand, ControllerControl.Thumbstick)));
            Assert.IsFalse(_rig.View.IsVisible);
            Assert.IsFalse(_rig.View.gameObject.activeSelf);
        }

        [Test]
        public void Show_InvalidBinding_ReturnsFalse()
        {
            Assert.IsFalse(_rig.View.Show(default(PhysicalBinding)));
            Assert.IsFalse(_rig.View.IsVisible);
        }

        [Test]
        public void Tick_HighlightsOnlyTheRequiredButton()
        {
            _rig.View.Show(RightTrigger);
            _rig.View.Tick(0.1f);

            Assert.IsTrue(HasHighlight(_rig.Button(_rig.Right, ControllerControl.Trigger)));
            Assert.IsFalse(HasHighlight(_rig.Button(_rig.Right, ControllerControl.Grip)));
            Assert.IsFalse(HasHighlight(_rig.Button(_rig.Left, ControllerControl.Trigger)));
        }

        [Test]
        public void Highlight_UsesPropertyBlock_NotANewMaterial()
        {
            _rig.View.Show(RightTrigger);
            _rig.View.Tick(0.1f);

            Renderer renderer = _rig.Button(_rig.Right, ControllerControl.Trigger).GetComponent<Renderer>();
            Assert.AreSame(_rig.Material, renderer.sharedMaterial);
        }

        [Test]
        public void Demonstration_PressesAlongTheConfiguredDirection_AndComesBack()
        {
            Transform trigger = _rig.Button(_rig.Right, ControllerControl.Trigger).transform;
            Vector3 rest = trigger.localPosition;
            _rig.View.Show(RightTrigger);

            float deepest = 0f;
            for (int frame = 0; frame < 300; frame++)
            {
                _rig.View.Tick(1f / 60f);
                deepest = Mathf.Max(deepest, rest.z - trigger.localPosition.z);
                Assert.AreEqual(rest.x, trigger.localPosition.x, 1e-5f, "pressed off its axis");
            }

            Assert.AreEqual(0.004f, deepest, 0.0005f, "never reached full press depth");
            Assert.IsTrue(_rig.View.IsVisible, "demonstration must loop while help is needed");
        }

        [Test]
        public void Demonstration_Repeats()
        {
            Transform trigger = _rig.Button(_rig.Right, ControllerControl.Trigger).transform;
            Vector3 rest = trigger.localPosition;
            _rig.View.Show(RightTrigger);

            int presses = 0;
            bool down = false;
            for (int frame = 0; frame < 60 * 6; frame++)
            {
                _rig.View.Tick(1f / 60f);
                bool nowDown = rest.z - trigger.localPosition.z > 0.003f;
                if (nowDown && !down)
                    presses++;
                down = nowDown;
            }

            Assert.GreaterOrEqual(presses, 3);
        }

        [Test]
        public void Hide_RestoresPoseAndClearsHighlight()
        {
            ControllerButtonVisual trigger = _rig.Button(_rig.Right, ControllerControl.Trigger);
            Vector3 rest = trigger.transform.localPosition;
            _rig.View.Show(RightTrigger);
            for (int frame = 0; frame < 50; frame++)
                _rig.View.Tick(1f / 60f);

            _rig.View.Hide();

            Assert.AreEqual(rest, trigger.transform.localPosition);
            Assert.IsFalse(HasHighlight(trigger));
            Assert.IsFalse(_rig.View.IsVisible);
            Assert.IsFalse(_rig.View.gameObject.activeSelf);
        }

        [Test]
        public void Release_PlaysPressedPoseThenHidesAndRestores()
        {
            ControllerButtonVisual trigger = _rig.Button(_rig.Right, ControllerControl.Trigger);
            Vector3 rest = trigger.transform.localPosition;
            _rig.View.Show(RightTrigger);
            _rig.View.Tick(0.05f);

            _rig.View.Release();
            Assert.IsTrue(_rig.View.IsReleasing);
            _rig.View.Tick(0.05f);
            Assert.Less(trigger.transform.localPosition.z, rest.z, "release should show the press");

            for (int frame = 0; frame < 120 && _rig.View.IsVisible; frame++)
                _rig.View.Tick(1f / 60f);

            Assert.IsFalse(_rig.View.IsVisible);
            Assert.IsFalse(_rig.View.IsReleasing);
            Assert.AreEqual(rest, trigger.transform.localPosition);
            Assert.IsFalse(HasHighlight(trigger));
        }

        [Test]
        public void Show_AnotherControl_RestoresThePreviousOne()
        {
            ControllerButtonVisual trigger = _rig.Button(_rig.Right, ControllerControl.Trigger);
            Vector3 rest = trigger.transform.localPosition;
            _rig.View.Show(RightTrigger);
            for (int frame = 0; frame < 50; frame++)
                _rig.View.Tick(1f / 60f);

            _rig.View.Show(new PhysicalBinding(XRNodeRole.RightHand, ControllerControl.Grip));
            _rig.View.Tick(0.1f);

            Assert.AreEqual(rest, trigger.transform.localPosition);
            Assert.IsFalse(HasHighlight(trigger));
            Assert.IsTrue(HasHighlight(_rig.Button(_rig.Right, ControllerControl.Grip)));
        }

        [Test]
        public void View_IsUsableThroughTheInterface()
        {
            IControllerHintView view = _rig.View;
            Assert.IsTrue(view.Show(RightTrigger));
            view.Hide();
            Assert.IsFalse(view.IsVisible);
        }

        [Test]
        public void Demonstration_StopsAfterThreePresses_ButHelpStays()
        {
            ControllerButtonVisual trigger = _rig.Button(_rig.Right, ControllerControl.Trigger);
            Vector3 rest = trigger.transform.localPosition;
            _rig.View.Show(RightTrigger);

            int presses = 0;
            bool down = false;
            for (int frame = 0; frame < 60 * 20; frame++)
            {
                _rig.View.Tick(1f / 60f);
                bool nowDown = rest.z - trigger.transform.localPosition.z > 0.003f;
                if (nowDown && !down)
                    presses++;
                down = nowDown;
            }

            Assert.AreEqual(3, presses, "at most three automatic demonstrations");
            Assert.AreEqual(3, _rig.View.DemonstrationsPlayed);
            Assert.IsTrue(_rig.View.IsVisible, "after the demonstrations the controller stays");
            Assert.IsTrue(HasHighlight(trigger), "and the button stays highlighted");
            Assert.AreEqual(rest, trigger.transform.localPosition, "resting, not pressed");
        }

        [Test]
        public void Release_DuringADemonstration_StopsTheLoopAtOnce()
        {
            _rig.View.Show(RightTrigger);
            _rig.View.Tick(0.5f);

            _rig.View.Release();
            int before = _rig.View.DemonstrationsPlayed;
            for (int frame = 0; frame < 120 && _rig.View.IsVisible; frame++)
                _rig.View.Tick(1f / 60f);

            Assert.IsFalse(_rig.View.IsVisible);
            Assert.AreEqual(before, _rig.View.DemonstrationsPlayed, "no new demonstration after the real action");
        }

        [Test]
        public void Show_TurnsTheModelToThePoseOfThatControl()
        {
            _rig.View.Show(RightTrigger);
            _rig.View.Tick(1f);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(TouchPlusRig.TriggerEuler), _rig.Right.transform.localRotation), 0.5f);
            Assert.Less(Vector3.Distance(new Vector3(0f, 0.01f, 0f), _rig.Right.transform.localPosition), 1e-4f);

            _rig.View.Show(new PhysicalBinding(XRNodeRole.RightHand, ControllerControl.Grip));
            _rig.View.Tick(1f);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(TouchPlusRig.GripEuler), _rig.Right.transform.localRotation), 0.5f);
        }

        [Test]
        public void Show_ControlWithoutPose_FallsBackToTheDefaultPose()
        {
            _rig.View.Show(new PhysicalBinding(XRNodeRole.RightHand, ControllerControl.PrimaryButton));
            _rig.View.Tick(1f);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 10f, 0f), _rig.Right.transform.localRotation), 0.5f);
        }

        [Test]
        public void PoseChange_IsEased_NotSnapped()
        {
            _rig.View.Show(RightTrigger);
            _rig.View.Tick(1f / 60f);
            float angle = Quaternion.Angle(Quaternion.identity, _rig.Right.transform.localRotation);
            Assert.Greater(angle, 0f);
            Assert.Less(angle, 30f, "the first frame must not jump to the final pose");
        }

        [Test]
        public void Place_KeepsTheHintNearTheGaze_EvenForATargetFarToTheSide()
        {
            Pose viewer = new Pose(new Vector3(0f, 1.6f, 0f), Quaternion.identity);
            Vector3 farRight = new Vector3(3f, 1.2f, 1f);
            ((IHeadRelativeHintView)_rig.View).Place(viewer, farRight);

            Vector3 toHint = _rig.View.transform.position - viewer.position;
            float distance = Vector3.ProjectOnPlane(toHint, Vector3.up).magnitude;
            float yaw = Vector3.SignedAngle(Vector3.forward, Vector3.ProjectOnPlane(toHint, Vector3.up), Vector3.up);
            float targetYaw = Vector3.SignedAngle(Vector3.forward, Vector3.ProjectOnPlane(farRight - viewer.position, Vector3.up), Vector3.up);
            Assert.Less(Mathf.Abs(yaw), 35f, "stays within a comfortable field of view");
            Assert.Less(yaw, targetYaw, "never further out than the target itself");
            Assert.Less(distance, 1f, "close to the camera, not at the target");
        }

        [Test]
        public void Place_FacesTheViewer_AndUsesTheConfiguredScale()
        {
            SerializedFields.Set(_rig.View, "_scale", 2.5f);
            Pose viewer = new Pose(Vector3.zero, Quaternion.identity);
            ((IHeadRelativeHintView)_rig.View).Place(viewer, new Vector3(0f, 0f, 2f));

            Vector3 toViewer = (viewer.position - _rig.View.transform.position).normalized;
            Assert.Greater(Vector3.Dot(_rig.View.transform.forward, toViewer), 0.95f);
            Assert.AreEqual(Vector3.one * 2.5f, _rig.View.transform.localScale);
        }

        [Test]
        public void SmallHeadMovements_DoNotMoveTheHint()
        {
            _rig.View.Show(RightTrigger);
            IHeadRelativeHintView view = _rig.View;
            Vector3 focus = new Vector3(0f, 1.5f, 2f);
            view.Place(new Pose(new Vector3(0f, 1.6f, 0f), Quaternion.identity), focus);
            Vector3 placed = _rig.View.transform.position;

            for (int frame = 0; frame < 60; frame++)
            {
                // Jitter and small glances: a few degrees and centimetres.
                Quaternion glance = Quaternion.Euler(0f, Mathf.Sin(frame * 0.3f) * 8f, 0f);
                Vector3 sway = new Vector3(Mathf.Sin(frame * 0.5f) * 0.03f, 1.6f, 0f);
                view.Place(new Pose(sway, glance), focus);
                _rig.View.Tick(1f / 60f);
            }

            Assert.Less(Vector3.Distance(placed, _rig.View.transform.position), 1e-4f, "world-locked, not head-locked");
        }

        [Test]
        public void ALargeTurn_MovesTheHintSmoothly()
        {
            _rig.View.Show(RightTrigger);
            IHeadRelativeHintView view = _rig.View;
            view.Place(new Pose(new Vector3(0f, 1.6f, 0f), Quaternion.identity), new Vector3(0f, 1.5f, 2f));
            Vector3 before = _rig.View.transform.position;

            Pose turned = new Pose(new Vector3(0f, 1.6f, 0f), Quaternion.Euler(0f, 60f, 0f));
            Vector3 turnedFocus = turned.position + turned.rotation * new Vector3(0f, -0.1f, 2f);
            view.Place(turned, turnedFocus);
            _rig.View.Tick(1f / 60f);
            float firstStep = Vector3.Distance(before, _rig.View.transform.position);
            for (int frame = 0; frame < 60; frame++)
            {
                view.Place(turned, turnedFocus);
                _rig.View.Tick(1f / 60f);
            }

            Assert.Less(firstStep, 0.1f, "eases instead of jumping");
            Vector3 toHint = Vector3.ProjectOnPlane(_rig.View.transform.position - turned.position, Vector3.up);
            Assert.Less(Vector3.Angle(turned.rotation * Vector3.forward, toHint), 35f, "ends up in front of the new gaze");
        }

        private static bool HasHighlight(ControllerButtonVisual button)
        {
            Renderer renderer = button.GetComponent<Renderer>();
            if (!renderer.HasPropertyBlock())
                return false;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            return !block.isEmpty;
        }
    }
}
