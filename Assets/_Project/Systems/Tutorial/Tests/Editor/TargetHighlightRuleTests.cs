using NUnit.Framework;
using RideSafe.TaskSequence;
using RideSafe.Tutorial.AutoHand;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.Tutorial.Tests
{
    public class TargetHighlightRuleTests
    {
        private GameObject _cards;
        private GameObject _target3D;

        [SetUp]
        public void SetUp()
        {
            _cards = new GameObject("Options", typeof(RectTransform));
            GameObject card = new GameObject("Option_en", typeof(RectTransform), typeof(Toggle));
            card.transform.SetParent(_cards.transform, false);
            _target3D = GameObject.CreatePrimitive(PrimitiveType.Cube);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_cards);
            Object.DestroyImmediate(_target3D);
        }

        [Test]
        public void Auto_SkipsUIControlsWithTheirOwnStates()
        {
            Assert.IsFalse(TargetHighlightRule.ShouldHighlight(TargetHighlightMode.Auto, _cards.transform, false));
        }

        [Test]
        public void Auto_SkipsAButtonTargetItself()
        {
            GameObject button = new GameObject("ContinueButton", typeof(RectTransform), typeof(Button));
            try
            {
                Assert.IsFalse(TargetHighlightRule.ShouldHighlight(TargetHighlightMode.Auto, button.transform, false));
            }
            finally
            {
                Object.DestroyImmediate(button);
            }
        }

        [Test]
        public void Auto_Keeps3DTutorialTargets()
        {
            Assert.IsTrue(TargetHighlightRule.ShouldHighlight(TargetHighlightMode.Auto, _target3D.transform, false));
        }

        [Test]
        public void Auto_IgnoresInactiveControls()
        {
            _cards.transform.GetChild(0).gameObject.SetActive(false);
            Assert.IsTrue(TargetHighlightRule.ShouldHighlight(TargetHighlightMode.Auto, _cards.transform, false));
        }

        [Test]
        public void StepOverride_WinsOverTheDefault()
        {
            Assert.IsTrue(TargetHighlightRule.ShouldHighlight(TargetHighlightMode.Show, _cards.transform, false));
            Assert.IsFalse(TargetHighlightRule.ShouldHighlight(TargetHighlightMode.Hide, _target3D.transform, true));
        }

        [Test]
        public void PresenterOption_CanStillHighlightUI()
        {
            Assert.IsTrue(TargetHighlightRule.ShouldHighlight(TargetHighlightMode.Auto, _cards.transform, true));
        }
    }
}
