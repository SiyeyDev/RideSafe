using NUnit.Framework;
using UnityEngine;

namespace RideSafe.Module02.Tests
{
    public class Module02LogicTests
    {
        private static readonly Pose Start = new Pose(Vector3.zero, Quaternion.identity);
        private static readonly Pose Cp1 = new Pose(new Vector3(0, 0, 50), Quaternion.Euler(0, 90, 0));

        [TestCase(0f, SpeedBand.Green)]
        [TestCase(15f, SpeedBand.Green)]
        [TestCase(15.1f, SpeedBand.Yellow)]
        [TestCase(20f, SpeedBand.Yellow)]
        [TestCase(20.1f, SpeedBand.Red)]
        public void Banda_LimiteYTolerancia(float kmh, SpeedBand expected)
        {
            Assert.AreEqual(expected, SpeedBands.Classify(kmh, 15f, 5f));
        }

        [Test]
        public void Practica_FallarAntesDeCheckpoint_VuelveAlInicio()
        {
            var t = new CheckpointTracker(RunMode.Practice, Start);
            Assert.IsTrue(t.Fail("garaje"));
            Assert.IsNull(t.LastId);
            Assert.AreEqual(Start, t.LastPose);
        }

        [Test]
        public void Practica_Fallar_VuelveAlUltimoCheckpoint()
        {
            var t = new CheckpointTracker(RunMode.Practice, Start);
            t.Reach("cp1", Cp1);
            Assert.IsTrue(t.Fail("semaforo"));
            Assert.AreEqual("cp1", t.LastId);
            Assert.AreEqual(Cp1, t.LastPose);
        }

        [Test]
        public void Prueba_Fallar_NoPresentaNiReaparece_YNoAprueba()
        {
            var t = new CheckpointTracker(RunMode.Test, Start);
            Assert.IsTrue(t.Passed);
            Assert.IsFalse(t.Fail("cebra"));
            Assert.IsFalse(t.Passed);
            CollectionAssert.AreEquivalent(new[] { "cebra" }, t.FailedZones);
        }

        [Test]
        public void RouteCheckpoints_Practica_AvisaYReapareceEnElInicio()
        {
            var go = new GameObject("route");
            go.transform.SetPositionAndRotation(new Vector3(1, 0, 2), Quaternion.Euler(0, 45, 0));
            try
            {
                var rc = go.AddComponent<RouteCheckpoints>();
                string failed = null;
                Pose? respawn = null;
                rc.Failed += z => failed = z;
                rc.RespawnRequested += p => respawn = p;

                rc.Fail("garaje");
                rc.Respawn();

                Assert.AreEqual("garaje", failed);
                Assert.AreEqual(new Pose(go.transform.position, go.transform.rotation), respawn);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
