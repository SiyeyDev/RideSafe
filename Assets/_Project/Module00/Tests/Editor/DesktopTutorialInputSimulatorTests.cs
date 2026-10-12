using System.Collections.Generic;
using System.Reflection;
using Cachacos;
using NUnit.Framework;
using RideSafe.Demo;
using RideSafe.TaskSequence;
using RideSafe.Tutorial;
using UnityEngine;

namespace RideSafe.Module00.Tests
{
    public class DesktopTutorialInputSimulatorTests
    {
        private sealed class ScenePoseProvider : IXRPoseProvider
        {
            public bool IsTracked(XRNodeRole role) => false;
            public bool TryGetPose(XRNodeRole role, out Pose pose)
            {
                pose = default(Pose);
                return false;
            }
        }

        private readonly List<Object> _owned = new List<Object>();
        private ScenePoseProvider _sceneProvider;
        private DesktopTutorialInputSimulator _simulator;
        private Camera _camera;

        [SetUp]
        public void SetUp()
        {
            _sceneProvider = new ScenePoseProvider();
            Assert.IsTrue(ServiceRegistration.TryRegister<IXRPoseProvider>(_sceneProvider));

            GameObject desktop = new GameObject("DesktopMode");
            _owned.Add(desktop);
            _camera = new GameObject("Desktop Camera").AddComponent<Camera>();
            _camera.transform.SetParent(desktop.transform, false);
            _camera.transform.SetPositionAndRotation(new Vector3(1f, 1.8f, 2.6f), Quaternion.Euler(0f, 180f, 0f));
            _simulator = desktop.AddComponent<DesktopTutorialInputSimulator>();
            FieldInfo camera = typeof(DesktopTutorialInputSimulator).GetField("_desktopCamera", BindingFlags.Instance | BindingFlags.NonPublic);
            camera.SetValue(_simulator, _camera);
        }

        [TearDown]
        public void TearDown()
        {
            ServiceRegistration.Deregister<IXRPoseProvider>(_simulator);
            ServiceRegistration.Deregister<IXRPoseProvider>(_sceneProvider);
            foreach (Object owned in _owned)
                Object.DestroyImmediate(owned);
        }

        [Test]
        public void TakesThePoseSlotOver_WithoutLoggingADuplicate()
        {
            _simulator.TakeOverPoseService();

            Assert.AreSame(_simulator, ServiceLocator.Instance.RequestService<IXRPoseProvider>());
            // An unexpected LogError (the duplicate-service error) fails the test by itself.
        }

        [Test]
        public void ReportsTrackedControllers_SoTheTrackingWaitCanPass()
        {
            _simulator.TakeOverPoseService();
            Assert.IsTrue(_simulator.IsTracked(XRNodeRole.LeftHand));
            Assert.IsTrue(_simulator.IsTracked(XRNodeRole.RightHand));
        }

        [Test]
        public void HeadIsTheExistingDesktopCamera_NotMoved()
        {
            _simulator.TakeOverPoseService();
            Pose head;
            Assert.IsTrue(_simulator.TryGetPose(XRNodeRole.Head, out head));
            Assert.AreEqual(_camera.transform.position, head.position);
            Assert.AreEqual(new Vector3(1f, 1.8f, 2.6f), _camera.transform.position);
        }

        [Test]
        public void TakingOverTwice_IsHarmless()
        {
            _simulator.TakeOverPoseService();
            _simulator.TakeOverPoseService();
            Assert.AreSame(_simulator, ServiceLocator.Instance.RequestService<IXRPoseProvider>());
            // An unexpected LogError (the duplicate-service error) fails the test by itself.
        }
    }
}
