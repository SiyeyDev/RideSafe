using System.Collections.Generic;
using NUnit.Framework;
using RideSafe.UI;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// The dark bubble around the learner's head while a vehicle point is open (PO, 2026-10-23):
    /// everything farther than its radius is dimmed, the pedestal and the popup inside are not.
    /// It never switches on or off at once: the alpha fades, as the PO asked, also when the popup is
    /// answered halfway through the fade.
    /// </summary>
    public class FocusDomeTests
    {
        private const float Dim = .88f;
        private const float Fade = .45f;

        private readonly List<Object> _owned = new List<Object>();
        private FocusDome _dome;
        private MeshRenderer _renderer;
        private Transform _head;

        [SetUp]
        public void SetUp()
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _owned.Add(sphere);
            Object.DestroyImmediate(sphere.GetComponent<Collider>());
            Material material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _owned.Add(material);
            _renderer = sphere.GetComponent<MeshRenderer>();
            _renderer.sharedMaterial = material;
            _head = new GameObject("head").transform;
            _owned.Add(_head.gameObject);
            _dome = sphere.AddComponent<FocusDome>();
            _dome.ConfigureForTests(_renderer, _head, 1.6f, Dim, Fade);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _owned)
                Object.DestroyImmediate(o);
            _owned.Clear();
        }

        [Test]
        public void Off_ItIsHidden_AndCostsNothing()
        {
            Assert.IsFalse(_dome.IsOn);
            Assert.AreEqual(0f, _dome.CurrentAlpha);
            Assert.IsFalse(_renderer.enabled);
        }

        [Test]
        public void SwitchingOn_FadesTheAlphaUp_NotAtOnce()
        {
            _dome.SetOn(true);
            Assert.IsTrue(_renderer.enabled, "visible from the first frame of the fade");
            Assert.AreEqual(0f, _dome.CurrentAlpha, 1e-4f, "starts transparent");

            _dome.Tick(Fade * .5f);
            Assert.Greater(_dome.CurrentAlpha, 0f, "halfway it is darkening");
            Assert.Less(_dome.CurrentAlpha, Dim, "but not yet fully");

            _dome.Tick(Fade);
            Assert.AreEqual(Dim, _dome.CurrentAlpha, 1e-4f);
        }

        [Test]
        public void SwitchingOff_FadesDown_AndHidesOnlyAtZero()
        {
            _dome.SetOnImmediate(true);

            _dome.SetOn(false);
            _dome.Tick(Fade * .5f);
            Assert.IsTrue(_renderer.enabled, "still visible while it fades out");
            Assert.Greater(_dome.CurrentAlpha, 0f);
            Assert.Less(_dome.CurrentAlpha, Dim);

            _dome.Tick(Fade);
            Assert.AreEqual(0f, _dome.CurrentAlpha, 1e-4f);
            Assert.IsFalse(_renderer.enabled, "hidden once transparent");
        }

        [Test]
        public void AnsweredHalfwayThroughTheFadeIn_FadesOutFromWhereItWas()
        {
            _dome.SetOn(true);
            _dome.Tick(Fade * .5f);
            float halfway = _dome.CurrentAlpha;

            _dome.SetOn(false);
            Assert.AreEqual(halfway, _dome.CurrentAlpha, 1e-4f, "no jump when the fade turns around");
            _dome.Tick(Fade * .1f);
            Assert.Less(_dome.CurrentAlpha, halfway);
            Assert.Greater(_dome.CurrentAlpha, 0f);
        }

        [Test]
        public void SetOnImmediate_DoesNotFade()
        {
            _dome.SetOnImmediate(true);
            Assert.AreEqual(Dim, _dome.CurrentAlpha, 1e-4f);
            Assert.IsTrue(_renderer.enabled);
        }

        [Test]
        public void ItFollowsTheHead_WithItsRadius()
        {
            _head.position = new Vector3(1f, 2f, 3f);
            _dome.SetOnImmediate(true);

            _dome.Tick(0f);

            Assert.AreEqual(Vector3.zero, _dome.transform.position - _head.position);
            Assert.AreEqual(3.2f, _dome.transform.lossyScale.x, 1e-4f, "Unity's sphere is 1 m across: scale = 2 x radius");
        }
    }
}
