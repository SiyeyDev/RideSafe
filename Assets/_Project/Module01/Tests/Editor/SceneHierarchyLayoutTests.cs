using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RideSafe.Module01.Editor;
using RideSafe.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// El Hierarchy de v5 va ordenado por función (PO, 2026-10-27): en la raíz solo hay carpetas,
    /// y cada objeto está en la suya. Las carpetas no mueven nada: están en el origen, sin giro
    /// ni escala.
    /// </summary>
    public class SceneHierarchyLayoutTests
    {
        private const string ScenePath = "Assets/_Project/Scene/RideSafe_Garaje_v5.unity";
        private Scene _scene;
        private bool _openedHere;

        [OneTimeSetUp]
        public void Open()
        {
            _scene = SceneManager.GetSceneByPath(ScenePath);
            _openedHere = !_scene.isLoaded;
            if (_openedHere)
                _scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        [OneTimeTearDown]
        public void Close()
        {
            if (_openedHere)
                EditorSceneManager.CloseScene(_scene, true);
        }

        [Test]
        public void En_la_raiz_solo_hay_carpetas()
        {
            string[] loose = _scene.GetRootGameObjects()
                .Where(r => r.GetComponent<SceneFolder>() == null)
                .Select(r => r.name).ToArray();

            CollectionAssert.IsEmpty(loose, "Fuera de carpeta: " + string.Join(", ", loose));
        }

        [Test]
        public void Cada_objeto_esta_en_su_carpeta()
        {
            List<string> wrong = new List<string>();
            foreach (SceneHierarchyLayout.Folder folder in SceneHierarchyLayout.Folders)
                foreach (string member in folder.Members)
                    foreach (GameObject go in SceneFolder.TopLevelObjects(_scene).Where(g => g.name == member))
                        if (go.transform.parent == null || go.transform.parent.name != folder.Name)
                            wrong.Add($"{member} en '{(go.transform.parent != null ? go.transform.parent.name : "la raíz")}'");

            CollectionAssert.IsEmpty(wrong, string.Join("; ", wrong));
        }

        [Test]
        public void Solo_quedan_las_carpetas_declaradas()
        {
            HashSet<string> declared = new HashSet<string>(SceneHierarchyLayout.Folders.Select(f => f.Name));
            string[] extra = _scene.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<SceneFolder>(true))
                .Select(f => f.name).Where(n => !declared.Contains(n)).ToArray();

            CollectionAssert.IsEmpty(extra, "Carpetas que ya no se usan: " + string.Join(", ", extra));
        }

        [Test]
        public void Las_carpetas_no_mueven_lo_que_guardan()
        {
            foreach (SceneFolder folder in _scene.GetRootGameObjects()
                         .SelectMany(r => r.GetComponentsInChildren<SceneFolder>(true)))
            {
                Transform t = folder.transform;
                Assert.AreEqual(Vector3.zero, t.localPosition, t.name);
                Assert.AreEqual(Quaternion.identity, t.localRotation, t.name);
                Assert.AreEqual(Vector3.one, t.localScale, t.name);
            }
        }
    }
}
