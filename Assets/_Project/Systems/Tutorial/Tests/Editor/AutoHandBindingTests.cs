using System.Collections.Generic;
using Autohand.Demo;
using NUnit.Framework;
using RideSafe.Tutorial.AutoHand;
using UnityEngine;

namespace RideSafe.Tutorial.Tests
{
    public class AutoHandControlMapTests
    {
        [TestCase(CommonButton.triggerButton, ControllerControl.Trigger)]
        [TestCase(CommonButton.gripButton, ControllerControl.Grip)]
        [TestCase(CommonButton.primaryButton, ControllerControl.PrimaryButton)]
        [TestCase(CommonButton.secondaryButton, ControllerControl.SecondaryButton)]
        [TestCase(CommonButton.primary2DAxisClick, ControllerControl.Thumbstick)]
        [TestCase(CommonButton.primary2DAxisTouch, ControllerControl.Thumbstick)]
        [TestCase(CommonButton.menuButton, ControllerControl.Menu)]
        [TestCase(CommonButton.none, ControllerControl.None)]
        public void ToControl_MapsAutoHandButtons(CommonButton button, ControllerControl expected)
        {
            Assert.AreEqual(expected, AutoHandControlMap.ToControl(button));
        }

        [TestCase(ControllerControl.Trigger, CommonButton.triggerButton)]
        [TestCase(ControllerControl.Grip, CommonButton.gripButton)]
        [TestCase(ControllerControl.PrimaryButton, CommonButton.primaryButton)]
        [TestCase(ControllerControl.SecondaryButton, CommonButton.secondaryButton)]
        [TestCase(ControllerControl.Thumbstick, CommonButton.primary2DAxisClick)]
        [TestCase(ControllerControl.Menu, CommonButton.menuButton)]
        [TestCase(ControllerControl.System, CommonButton.none)]
        public void ToCommonButton_MapsBack(ControllerControl control, CommonButton expected)
        {
            Assert.AreEqual(expected, AutoHandControlMap.ToCommonButton(control));
        }
    }

    public class AutoHandBindingResolverTests
    {
        private GameObject _go;
        private IPhysicalBindingResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Input");
            AutoHandTutorialInputService service = _go.AddComponent<AutoHandTutorialInputService>();
            SerializedFields.Set(service, "_bindings", new List<AutoHandTutorialInputService.Binding>
            {
                new AutoHandTutorialInputService.Binding(ActionIds.PrimarySelect, XRNodeRole.RightHand, CommonButton.triggerButton),
                new AutoHandTutorialInputService.Binding(ActionIds.SecondarySelect, XRNodeRole.LeftHand, CommonButton.triggerButton),
                new AutoHandTutorialInputService.Binding(ActionIds.Back, XRNodeRole.RightHand, CommonButton.secondaryButton)
            });
            _resolver = service;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        [Test]
        public void PrimarySelect_ResolvesToRightTrigger()
        {
            PhysicalBinding binding;
            Assert.IsTrue(_resolver.TryResolve(new ActionId(ActionIds.PrimarySelect), out binding));
            Assert.AreEqual(new PhysicalBinding(XRNodeRole.RightHand, ControllerControl.Trigger), binding);
        }

        [Test]
        public void SecondarySelect_ResolvesToTheLeftHand()
        {
            PhysicalBinding binding;
            Assert.IsTrue(_resolver.TryResolve(new ActionId(ActionIds.SecondarySelect), out binding));
            Assert.AreEqual(XRNodeRole.LeftHand, binding.Hand);
            Assert.IsTrue(binding.IsLeftHand);
        }

        [Test]
        public void UnboundAction_ReturnsFalse_SoPresentationCanFallBack()
        {
            PhysicalBinding binding;
            Assert.IsFalse(_resolver.TryResolve(new ActionId(ActionIds.Recenter), out binding));
            Assert.IsFalse(binding.IsValid);
        }

        [Test]
        public void InvalidAction_ReturnsFalse()
        {
            PhysicalBinding binding;
            Assert.IsFalse(_resolver.TryResolve(default(ActionId), out binding));
        }

        [Test]
        public void Resolution_DoesNotNeedLiveControllers()
        {
            // No XRPlayer in an EditMode test: the answer comes from the profile, not the hardware.
            PhysicalBinding binding;
            Assert.IsTrue(_resolver.TryResolve(new ActionId(ActionIds.Back), out binding));
            Assert.AreEqual(ControllerControl.SecondaryButton, binding.Control);
        }
    }

    public class ControllerHintView2DFallbackTests
    {
        private GameObject _go;
        private IControllerHintView _view;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Hint2D", typeof(RectTransform));
            GameObject trigger = new GameObject("Trigger", typeof(RectTransform));
            trigger.transform.SetParent(_go.transform, false);
            ControllerHintView view = _go.AddComponent<ControllerHintView>();
            SerializedFields.Set(view, "_parts", new List<ControllerHintView.Part>
            {
                new ControllerHintView.Part { Button = CommonButton.triggerButton, Rect = (RectTransform)trigger.transform }
            });
            _go.SetActive(false);
            _view = view;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        [Test]
        public void Show_DrawnControl_ReturnsTrueAndIsVisible()
        {
            Assert.IsTrue(_view.Show(new PhysicalBinding(XRNodeRole.RightHand, ControllerControl.Trigger)));
            Assert.IsTrue(_view.IsVisible);
        }

        [Test]
        public void Show_ControlNotDrawn_ReturnsFalse()
        {
            Assert.IsFalse(_view.Show(new PhysicalBinding(XRNodeRole.RightHand, ControllerControl.Grip)));
            Assert.IsFalse(_view.IsVisible);
        }

        [Test]
        public void Hide_HidesTheView()
        {
            _view.Show(new PhysicalBinding(XRNodeRole.RightHand, ControllerControl.Trigger));
            _view.Hide();
            Assert.IsFalse(_view.IsVisible);
        }
    }
}
