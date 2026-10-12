using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RideSafe.Module00;
using UnityEngine;

namespace RideSafe.Module00.Tests
{
    public class VehiclePreviewControllerTests
    {
        private const float Frame = 1f / 60f;
        private const float BlendSeconds = 0.8f;

        private readonly List<Object> _owned = new List<Object>();
        private VehiclePreviewController _preview;
        private Transform _ebike;
        private Transform _scooter;
        private GameObject _ebikePlatform;
        private GameObject _scooterPlatform;

        private static readonly Vector3 EbikeRest = new Vector3(-1f, 0.04f, -0.27f);
        private static readonly Quaternion EbikeRestRotation = Quaternion.Euler(0f, 90f, 0f);
        private static readonly Vector3 ScooterRest = new Vector3(1.7f, 0.06f, -0.3f);
        private static readonly Quaternion ScooterRestRotation = Quaternion.Euler(0f, -90f, 0f);

        [SetUp]
        public void SetUp()
        {
            _ebike = Create("ebike", EbikeRest, EbikeRestRotation).transform;
            _scooter = Create("scooter", ScooterRest, ScooterRestRotation).transform;
            _ebikePlatform = Create("plataform", Vector3.zero, Quaternion.identity);
            _scooterPlatform = Create("plataform (1)", Vector3.zero, Quaternion.identity);

            GameObject root = Create("Vehicle Preview", Vector3.zero, Quaternion.identity);
            _preview = root.AddComponent<VehiclePreviewController>();
            Set(_preview, "_vehicles", new List<VehiclePreviewController.Vehicle>
            {
                new VehiclePreviewController.Vehicle("ebike", _ebike, _ebikePlatform),
                new VehiclePreviewController.Vehicle("escooter", _scooter, _scooterPlatform)
            });
            Set(_preview, "_blendSeconds", BlendSeconds);
            _preview.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object owned in _owned)
                Object.DestroyImmediate(owned);
            _owned.Clear();
        }

        [Test]
        public void NoSelection_PlatformsOff_NothingRotating()
        {
            Assert.IsNull(_preview.SelectedId);
            Assert.IsFalse(_ebikePlatform.activeSelf);
            Assert.IsFalse(_scooterPlatform.activeSelf);
            Assert.IsFalse(_preview.IsRotating("ebike"));
            Assert.IsFalse(_preview.IsRotating("escooter"));
        }

        [Test]
        public void SelectEbike_ShowsItsPlatformAndStartsRotatingIt()
        {
            _preview.Select("ebike");
            Run(BlendSeconds + 1f);

            Assert.AreEqual("ebike", _preview.SelectedId);
            Assert.IsTrue(_preview.IsPlatformShown("ebike"));
            Assert.IsTrue(_ebikePlatform.activeSelf);
            Assert.IsTrue(_preview.IsRotating("ebike"));

            Quaternion before = _ebike.localRotation;
            Run(0.5f);
            Assert.Greater(Quaternion.Angle(before, _ebike.localRotation), 1f, "should keep turning");
            AssertAtRest(_scooter, ScooterRest, ScooterRestRotation);
        }

        [Test]
        public void EbikeToScooter_EbikeStopsAndReturnsToItsExactPose_ScooterTakesOver()
        {
            _preview.Select("ebike");
            Run(3f);

            _preview.Select("escooter");
            Assert.IsFalse(_preview.IsRotating("ebike"), "the previous vehicle stops turning at once");
            Assert.IsTrue(_preview.IsRotating("escooter"));
            Run(BlendSeconds + 0.1f);

            AssertAtRest(_ebike, EbikeRest, EbikeRestRotation);
            Assert.IsFalse(_preview.IsPlatformShown("ebike"));
            Assert.IsTrue(_preview.IsPlatformShown("escooter"));

            Quaternion before = _scooter.localRotation;
            Run(0.5f);
            Assert.Greater(Quaternion.Angle(before, _scooter.localRotation), 1f);
        }

        [Test]
        public void ScooterToEbike_IsTheExactInverse()
        {
            _preview.Select("escooter");
            Run(3f);

            _preview.Select("ebike");
            Run(BlendSeconds + 0.1f);

            AssertAtRest(_scooter, ScooterRest, ScooterRestRotation);
            Assert.IsFalse(_preview.IsRotating("escooter"));
            Assert.IsTrue(_preview.IsRotating("ebike"));
            Assert.IsTrue(_preview.IsPlatformShown("ebike"));
            Assert.IsFalse(_preview.IsPlatformShown("escooter"));
        }

        [Test]
        public void NeverTwoVehiclesRotating_EvenWhenSwitchingFast()
        {
            string[] sequence = { "ebike", "escooter", "ebike", "escooter", "ebike" };
            foreach (string id in sequence)
            {
                _preview.Select(id);
                for (int frame = 0; frame < 20; frame++)
                {
                    _preview.Tick(Frame);
                    int rotating = (_preview.IsRotating("ebike") ? 1 : 0) + (_preview.IsRotating("escooter") ? 1 : 0);
                    Assert.LessOrEqual(rotating, 1);
                }
            }
        }

        [Test]
        public void Transitions_AreContinuous_NoPops()
        {
            _preview.Select("ebike");
            Run(2f);
            _preview.Select("escooter");

            Vector3 lastEbike = _ebike.localPosition;
            Quaternion lastEbikeRotation = _ebike.localRotation;
            Vector3 lastScooter = _scooter.localPosition;
            for (int frame = 0; frame < 120; frame++)
            {
                _preview.Tick(Frame);
                Assert.Less(Vector3.Distance(lastEbike, _ebike.localPosition), 0.01f);
                Assert.Less(Quaternion.Angle(lastEbikeRotation, _ebike.localRotation), 8f);
                Assert.Less(Vector3.Distance(lastScooter, _scooter.localPosition), 0.01f);
                lastEbike = _ebike.localPosition;
                lastEbikeRotation = _ebike.localRotation;
                lastScooter = _scooter.localPosition;
            }
        }

        [Test]
        public void SelectingTheSameVehicleAgain_DoesNotRestartIt()
        {
            _preview.Select("ebike");
            Run(2f);
            Quaternion before = _ebike.localRotation;

            _preview.Select("ebike");
            _preview.Tick(Frame);

            Assert.Less(Quaternion.Angle(before, _ebike.localRotation), 1f);
            Assert.IsTrue(_preview.IsRotating("ebike"));
        }

        [Test]
        public void ClearSelection_ReturnsEverythingToRestAndHidesPlatforms()
        {
            _preview.Select("ebike");
            Run(2f);

            _preview.ClearSelection();
            Run(BlendSeconds + 0.1f);

            Assert.IsNull(_preview.SelectedId);
            AssertAtRest(_ebike, EbikeRest, EbikeRestRotation);
            Assert.IsFalse(_preview.IsRotating("ebike"));
            Assert.IsFalse(_preview.IsPlatformShown("ebike"));
        }

        [Test]
        public void UnknownVehicle_IsIgnored()
        {
            _preview.Select("ebike");
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning,
                new System.Text.RegularExpressions.Regex("no vehicle 'moped'"));

            _preview.Select("moped");

            Assert.AreEqual("ebike", _preview.SelectedId);
            Assert.IsTrue(_preview.IsRotating("ebike"));
        }

        [Test]
        public void Vehicles_AreNeverDeactivated()
        {
            _preview.Select("ebike");
            Run(1f);
            _preview.Select("escooter");
            Run(1f);

            Assert.IsTrue(_ebike.gameObject.activeSelf);
            Assert.IsTrue(_scooter.gameObject.activeSelf);
        }

        private void Run(float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
                _preview.Tick(Frame);
        }

        private static void AssertAtRest(Transform vehicle, Vector3 position, Quaternion rotation)
        {
            Assert.Less(Vector3.Distance(position, vehicle.localPosition), 1e-4f, vehicle.name + " position");
            Assert.Less(Quaternion.Angle(rotation, vehicle.localRotation), 0.05f, vehicle.name + " rotation");
            Assert.Less(Vector3.Distance(Vector3.one, vehicle.localScale), 1e-4f, vehicle.name + " scale");
        }

        private GameObject Create(string name, Vector3 position, Quaternion rotation)
        {
            GameObject go = new GameObject(name);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            _owned.Add(go);
            return go;
        }

        private static void Set(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, field);
            info.SetValue(target, value);
        }
    }
}
