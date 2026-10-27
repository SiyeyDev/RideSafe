using RideSafe.UI;
using NUnit.Framework;
using RideSafe.Module00.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RideSafe.Module00.Tests
{
    /// <summary>RideSafe_Garaje_v5 shows module 0's own display vehicles on particle platforms.</summary>
    public class GarajeV5ShowroomTests
    {
        private Scene _scene;
        private bool _openedHere;
        private GameObject _root;

        [OneTimeSetUp]
        public void Open()
        {
            _scene = SceneManager.GetSceneByPath(Module00ShowroomSetup.ScenePath);
            _openedHere = !_scene.isLoaded;
            if (_openedHere)
                _scene = EditorSceneManager.OpenScene(Module00ShowroomSetup.ScenePath, OpenSceneMode.Additive);
            foreach (GameObject root in SceneFolder.TopLevelObjects(_scene))
            {
                if (root.name == Module00ShowroomSetup.RootName)
                    _root = root;
            }
        }

        [OneTimeTearDown]
        public void Close()
        {
            if (_openedHere)
                EditorSceneManager.CloseScene(_scene, true);
        }

        [Test]
        public void Showroom_IsWired_ToTheModule0VehicleScreen()
        {
            Assert.IsNotNull(_root, "a " + Module00ShowroomSetup.RootName + " root");
            VehiclePanelPreviewBinding binding = _root.GetComponent<VehiclePanelPreviewBinding>();
            VehicleShowroom showroom = _root.GetComponent<VehicleShowroom>();
            Assert.IsNotNull(binding);
            Assert.IsNotNull(showroom);
            Object module = new SerializedObject(binding).FindProperty("_module").objectReferenceValue;
            Assert.IsNotNull(module);
            Assert.AreEqual("PF_Module00_UI", ((Component)module).gameObject.name);
            Assert.AreSame(module, new SerializedObject(showroom).FindProperty("_module").objectReferenceValue);
            GameObject stage = (GameObject)new SerializedObject(showroom).FindProperty("_stage").objectReferenceValue;
            Assert.IsFalse(stage.activeSelf, "hidden until the Vehicle screen opens");
        }

        [TestCase(0, "ebike")]
        [TestCase(1, "escooter")]
        public void EachVehicle_IsADisplayCopy_OnAParticlePlatform(int index, string id)
        {
            SerializedProperty entry = new SerializedObject(_root.GetComponent<VehiclePreviewController>())
                .FindProperty("_vehicles").GetArrayElementAtIndex(index);
            Assert.AreEqual(id, entry.FindPropertyRelative("_id").stringValue);
            Transform model = (Transform)entry.FindPropertyRelative("_model").objectReferenceValue;
            GameObject platform = (GameObject)entry.FindPropertyRelative("_platform").objectReferenceValue;
            Assert.IsTrue(model.IsChildOf(_root.transform), "a copy, not module 1's vehicle");
            Assert.Greater(model.GetComponentsInChildren<Renderer>(true).Length, 0);
            Assert.AreEqual(0, model.GetComponentsInChildren<Collider>(true).Length, "a display copy is not selectable");
            Assert.AreEqual(0, model.GetComponentsInChildren<MonoBehaviour>(true).Length, "no module behaviour on the copy");
            Assert.Greater(platform.GetComponentsInChildren<ParticleSystem>(true).Length, 0);
            Assert.AreEqual(model.parent, platform.transform.parent, "vehicle and platform share a slot");
        }
    }
}
