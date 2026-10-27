using RideSafe.UI;
using System.Collections.Generic;
using NUnit.Framework;
using RideSafe.Tutorial;
using RideSafe.Tutorial.Presentation;
using RideSafe.Help.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RideSafe.Help.Tests
{
    /// <summary>RideSafe_Garaje_v5 has one tutorial, wired, and no module knows it exists.</summary>
    public class GarajeV5ContractTests
    {
        private Scene _scene;
        private bool _openedHere;

        [OneTimeSetUp]
        public void Open()
        {
            _scene = SceneManager.GetSceneByPath(TutorialHelpSceneSetup.ScenePath);
            _openedHere = !_scene.isLoaded;
            if (_openedHere)
                _scene = EditorSceneManager.OpenScene(TutorialHelpSceneSetup.ScenePath, OpenSceneMode.Additive);
        }

        [OneTimeTearDown]
        public void Close()
        {
            if (_openedHere)
                EditorSceneManager.CloseScene(_scene, true);
        }

        private GameObject Root(string name)
        {
            // De primer nivel, también dentro de las carpetas del Hierarchy.
            foreach (GameObject root in SceneFolder.TopLevelObjects(_scene))
            {
                if (root.name == name)
                    return root;
            }
            return null;
        }

        [Test]
        public void TheOldTutorialIsGone()
        {
            Assert.IsNull(Root(TutorialHelpSceneSetup.OldRootName));
        }

        [Test]
        public void ThereIsExactlyOneTutorialHelp_FullyWired()
        {
            List<TutorialHelpManager> managers = new List<TutorialHelpManager>();
            foreach (GameObject root in _scene.GetRootGameObjects())
                managers.AddRange(root.GetComponentsInChildren<TutorialHelpManager>(true));
            Assert.AreEqual(1, managers.Count);
            TutorialHelpManager manager = managers[0];
            Assert.AreEqual(TutorialHelpSceneSetup.RootName, manager.gameObject.name);
            Assert.IsTrue(manager.gameObject.activeSelf);

            SerializedObject serialized = new SerializedObject(manager);
            TutorialGestureSO gesture = (TutorialGestureSO)serialized.FindProperty("_selectGesture").objectReferenceValue;
            Assert.IsNotNull(gesture);
            Assert.AreEqual(ControllerControl.Trigger, gesture.Control);
            Assert.AreEqual(TutorialGestureSO.HandChoice.Dominant, gesture.Hand);
            Assert.AreEqual(HelpTextKeys.SelectInterface, gesture.TextKeyFor(InteractionKind.Interface));
            Assert.IsNotNull(serialized.FindProperty("_hintView").objectReferenceValue);
            Assert.IsNotNull(serialized.FindProperty("_highlighter").objectReferenceValue);
        }

        [Test]
        public void TheHint_HasBothTouchPlus_WithATrigger()
        {
            GestureHintView view = Root(TutorialHelpSceneSetup.RootName).GetComponentInChildren<GestureHintView>(true);
            SerializedObject serialized = new SerializedObject(view);
            foreach (string field in new[] { "_left", "_right" })
            {
                ControllerModelVisual model = (ControllerModelVisual)serialized.FindProperty(field).objectReferenceValue;
                Assert.IsNotNull(model, field);
                Assert.IsTrue(model.TryGetButton(ControllerControl.Trigger, out _), field + " trigger");
            }
            Assert.IsNotNull(serialized.FindProperty("_caption").objectReferenceValue);
        }

        [Test]
        public void TheHalo_UsesTheTutorialsOwnShader()
        {
            TargetHighlighter highlighter = Root(TutorialHelpSceneSetup.RootName).GetComponent<TargetHighlighter>();
            Material material = (Material)new SerializedObject(highlighter).FindProperty("_haloMaterial").objectReferenceValue;
            Assert.AreEqual("RideSafe/Tutorial Halo", material.shader.name);
        }

        [Test]
        public void Module1Orientation_PausesTheTutorial_FromOutsideTheModule()
        {
            TutorialPause pause = Root(TutorialHelpSceneSetup.RootName).GetComponentInChildren<TutorialPause>(true);
            Assert.IsNotNull(pause, "a pause under TutorialHelp");
            GameObject watched = (GameObject)new SerializedObject(pause).FindProperty("_whileActive").objectReferenceValue;
            Assert.IsNotNull(watched);
            Assert.AreEqual("Orientation", watched.name);
            Assert.AreEqual("PF_Module01_UI", watched.transform.parent.name);
        }

        [Test]
        public void NoModuleReferencesTheTutorial()
        {
            Transform tutorial = Root(TutorialHelpSceneSetup.RootName).transform;
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null || behaviour.transform.IsChildOf(tutorial))
                        continue;
                    string ns = behaviour.GetType().Namespace ?? string.Empty;
                    Assert.IsFalse(ns.StartsWith("RideSafe.Help"), behaviour.name + " carries a tutorial component outside TutorialHelp");
                }
            }
        }
    }
}
