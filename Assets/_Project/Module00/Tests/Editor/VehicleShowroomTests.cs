using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RideSafe.Module00.Tests
{
    public class VehicleShowroomTests
    {
        private static readonly Pose Viewer = new Pose(new Vector3(0f, 1.5f, 0f), Quaternion.identity);
        private readonly List<GameObject> _objects = new List<GameObject>();
        private GameObject _panel, _stage;
        private Transform _label, _anchor;
        private VehicleShowroom _showroom;

        [SetUp]
        public void SetUp()
        {
            _panel = Track(new GameObject("Vehicle"));
            _label = Track(new GameObject("Option_ebike")).transform;
            _label.SetParent(_panel.transform, false);
            _label.position = new Vector3(-0.5f, 1.6f, 1.8f);
            _stage = Track(new GameObject("Stage"));
            _anchor = Track(new GameObject("Slot_ebike")).transform;
            _anchor.SetParent(_stage.transform, false);
            _showroom = Track(new GameObject("Showroom")).AddComponent<VehicleShowroom>();
            _showroom.Configure(_panel, _stage, 3.5f, 0f, (_anchor, "Option_ebike"));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects)
                Object.DestroyImmediate(go);
            _objects.Clear();
        }

        private GameObject Track(GameObject go)
        {
            _objects.Add(go);
            return go;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        [Test]
        public void TheStage_IsShownOnlyWhileTheVehicleScreenIsOpen()
        {
            _showroom.Tick(Viewer);
            Assert.IsTrue(_stage.activeSelf);
            _panel.SetActive(false);
            _showroom.Tick(Viewer);
            Assert.IsFalse(_stage.activeSelf);
        }

        [Test]
        public void EachVehicle_StandsOnTheGround_UnderItsLabel_AsTheLearnerSeesIt()
        {
            _showroom.Tick(Viewer);
            Assert.AreEqual(0f, _anchor.position.y, 1e-4f, "on the ground");
            Assert.AreEqual(3.5f, Flat(_anchor.position - Viewer.position).magnitude, 1e-3f, "at the showroom distance");
            Assert.Less(Vector3.Angle(Flat(_anchor.position - Viewer.position), Flat(_label.position - Viewer.position)), 0.1f,
                        "on the learner's line of sight through its label");
            Assert.Less(Vector3.Angle(_anchor.forward, Flat(_anchor.position - Viewer.position)), 0.1f, "turned to the learner");
        }

        [Test]
        public void Placement_HappensWhenTheScreenOpens_NotOnEveryHeadMove()
        {
            _showroom.Tick(Viewer);
            Vector3 placed = _anchor.position;
            _showroom.Tick(new Pose(new Vector3(1f, 1.5f, 0f), Quaternion.identity));
            Assert.AreEqual(placed, _anchor.position);

            _panel.SetActive(false);
            _showroom.Tick(Viewer);
            _panel.SetActive(true);
            _showroom.Tick(new Pose(new Vector3(1f, 1.5f, 0f), Quaternion.identity));
            Assert.AreNotEqual(placed, _anchor.position, "placed again for the new opening");
        }
    }
}
