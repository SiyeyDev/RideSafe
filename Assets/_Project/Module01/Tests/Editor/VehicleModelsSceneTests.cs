using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// La patineta del 2026-10-26 (<c>E_Scooter</c>) reemplaza a la vieja en v5: está una sola
    /// vez, a la escala del artista, y la vieja ya no está —ni para que entre en la build—.
    /// </summary>
    public class VehicleModelsSceneTests
    {
        private const string ScenePath = "Assets/_Project/Scene/RideSafe_Garaje_v5.unity";

        /// <summary>
        /// Escala local de cada objeto con ese nombre. Se copia antes de cerrar la escena: los
        /// tests de EditMode corren en una escena temporal, y un Transform de v5 ya cerrada es
        /// un objeto destruido.
        /// </summary>
        private static List<Vector3> Named(string name)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                return scene.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                    .Where(t => t.name == name)
                    .Select(t => t.localScale)
                    .ToList();
            }
            finally
            {
                if (openedHere)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void La_patineta_nueva_esta_una_vez_y_a_la_escala_del_artista()
        {
            List<Vector3> found = Named("E_Scooter");
            Assert.AreEqual(1, found.Count, "Ni falta ni esta repetida.");
            Assert.AreEqual(1.4f, found[0].x, 1e-3f, "La escala que dejo el artista en Garage.");
        }

        /// <summary>
        /// La bici y la patineta se revisan parecido, pero son vehículos distintos: solo la
        /// patineta tiene plataforma, así que solo ella tiene dónde colgar ese punto.
        /// </summary>
        [TestCase("ebike", 0)]
        [TestCase("E_Scooter", 1)]
        public void Solo_la_patineta_tiene_donde_colgar_la_plataforma(string vehicle, int mounts)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                Transform model = scene.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                    .Single(t => t.name == vehicle);
                int found = model.GetComponentsInChildren<Transform>(true).Count(t => t.name == "mount_point_deck");

                Assert.AreEqual(mounts, found, $"'{vehicle}' con {found} mount(s) de plataforma.");
            }
            finally
            {
                if (openedHere)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>
        /// En el editor se ve la bici, que no tiene plataforma: el punto de la plataforma no puede
        /// quedarse encendido colgando de ella. Se vería y respondería al clic en el módulo 0, y
        /// la copia de la vitrina se lo llevaría dentro. En runtime lo cuelga del scooter
        /// <c>VehicleStage</c>.
        /// </summary>
        [Test]
        public void La_plataforma_no_cuelga_encendida_de_la_bici()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                WorldObjectTaskEntity deck = scene.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<WorldObjectTaskEntity>(true))
                    .Single(e => e.ItemId == "point_deck");
                bool underBike = deck.GetComponentsInParent<Transform>(true).Any(t => t.name == "ebike");

                Assert.IsFalse(underBike, "La plataforma cuelga de la bici.");
                Assert.IsFalse(deck.gameObject.activeSelf, "Con la bici en el editor, la plataforma se apaga.");
            }
            finally
            {
                if (openedHere)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
