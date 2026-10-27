using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RideSafe.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Las carpetas del Hierarchy solo ordenan: para el código, un objeto dentro de una carpeta
    /// sigue siendo de primer nivel, como si la carpeta no existiera.
    /// </summary>
    public class SceneFolderTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        private GameObject Make(string name, Transform parent = null, bool folder = false)
        {
            GameObject go = new GameObject(name);
            if (parent == null)
                _objects.Add(go);
            go.transform.SetParent(parent, false);
            if (folder)
                go.AddComponent<SceneFolder>();
            return go;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects)
                Object.DestroyImmediate(go);
            _objects.Clear();
        }

        [Test]
        public void Un_objeto_dentro_de_una_carpeta_es_de_primer_nivel()
        {
            GameObject players = Make("--- JUGADOR ---", folder: true);
            GameObject rig = Make("XRPlayer", players.transform);
            GameObject head = Make("Head", rig.transform);

            Assert.AreSame(rig.transform, SceneFolder.TopLevel(head.transform));
            Assert.AreSame(rig.transform, SceneFolder.TopLevel(rig.transform));
        }

        [Test]
        public void Un_objeto_en_la_raiz_sigue_siendo_de_primer_nivel()
        {
            GameObject loose = Make("Loose");
            GameObject child = Make("Child", loose.transform);

            Assert.AreSame(loose.transform, SceneFolder.TopLevel(child.transform));
        }

        [Test]
        public void Los_objetos_de_primer_nivel_atraviesan_carpetas_anidadas()
        {
            GameObject epp = Make("--- EPP ---", folder: true);
            Make("epps", epp.transform);
            GameObject retired = Make("Retirados (apagados)", epp.transform, folder: true);
            Make("coderas", retired.transform);
            Make("Loose");

            string[] names = SceneFolder.TopLevelObjects(SceneManager.GetActiveScene())
                .Where(g => _objects.Contains(g) || _objects.Any(o => g.transform.IsChildOf(o.transform)))
                .Select(g => g.name).ToArray();

            CollectionAssert.AreEquivalent(new[] { "epps", "coderas", "Loose" }, names);
        }
    }
}
