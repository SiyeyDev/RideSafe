using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RideSafe.Module00.Tests
{
    /// <summary>
    /// The module 0 showroom shows a hand-made copy of each vehicle, not a prefab instance, so a
    /// new FBX does not reach it: with the e-bike of 2026-10-21 the copy kept pointing at
    /// <c>manubrio</c> and <c>rueda_trasera</c>, which no longer exist, and showed the bike
    /// without its front end or its rear wheel.
    /// </summary>
    public class ShowroomDisplayCopyTests
    {
        private const string ScenePath = "Assets/_Project/Scene/RideSafe_Garaje_v5.unity";

        [TestCase("ebike")]
        [TestCase("scooter")]
        public void TheShowroomCopy_HasTheSameMeshesAsTheVehicle(string vehicle)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                Transform copy = null, source = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == vehicle + " (display)")
                            copy = t;
                        else if (t.name == vehicle && source == null && !InShowroom(t))
                            source = t;
                    }
                Assert.IsNotNull(copy, "the showroom has no copy of " + vehicle);
                Assert.IsNotNull(source, "the scene has no " + vehicle);

                List<string> missing = copy.GetComponentsInChildren<MeshFilter>(true)
                    .Where(f => f.sharedMesh == null).Select(f => f.name).ToList();
                CollectionAssert.IsEmpty(missing, "meshes the FBX no longer has; run RideSafe > Módulo 0 > Rehacer las copias de la vitrina");

                CollectionAssert.AreEquivalent(Meshes(source), Meshes(copy), "the copy shows what the vehicle shows");
            }
            finally
            {
                if (openedHere)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static bool InShowroom(Transform t) =>
            t.GetComponentsInParent<Transform>(true).Any(p => p.name == "Module00 Vehicle Showroom");

        private static IEnumerable<string> Meshes(Transform root) =>
            root.GetComponentsInChildren<MeshFilter>(true)
                .Where(f => f.sharedMesh != null && f.GetComponent<MeshRenderer>() != null && f.GetComponent<MeshRenderer>().enabled)
                .Select(f => f.sharedMesh.name);
    }
}
