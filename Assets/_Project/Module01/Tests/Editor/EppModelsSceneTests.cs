using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Los EPP que llegaron con modelo nuevo el 26 y el 27-10 están en v5 con ese modelo: lo que
    /// el aprendiz pulsa es el FBX de esenciales, no el signo «!» ni el arte viejo, y el arte
    /// viejo está apagado para que no haya dos en la mesa.
    /// </summary>
    public class EppModelsSceneTests
    {
        private const string ScenePath = "Assets/_Project/Scene/RideSafe_Garaje_v5.unity";
        private const string Essentials = "Assets/_Project/Models/CITY/MODELADOS 3d/EPPS ESCENCIALES/";

        /// <summary>Lo que hay que saber de un objeto, copiado antes de cerrar la escena.</summary>
        private struct Seen
        {
            public string Name;
            public bool Active;
            public string Source;
        }

        private static List<Seen> Collect(System.Func<GameObject, bool> which)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                return scene.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                    .Select(t => t.gameObject)
                    .Where(which)
                    .Select(g => new Seen
                    {
                        Name = g.name,
                        Active = g.activeInHierarchy,
                        Source = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(g)),
                    })
                    .ToList();
            }
            finally
            {
                if (openedHere)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        [TestCase("sunglasses", "gafas de sol.fbx")]
        [TestCase("backpack", "mochila.fbx")]
        [TestCase("cargo_secured", "candado.fbx")]
        public void El_elemento_se_pulsa_sobre_su_modelo_nuevo(string itemId, string file)
        {
            List<Seen> entities = Collect(g =>
                g.TryGetComponent(out WorldObjectTaskEntity e) && e.ItemId == itemId);

            Assert.AreEqual(1, entities.Count, $"Una sola entidad para {itemId}.");
            Assert.AreEqual(Essentials + file, entities[0].Source,
                $"{itemId} tiene que ser el FBX nuevo, no el signo ni el arte viejo.");
            Assert.IsTrue(entities[0].Active, $"{itemId} tiene que verse.");
        }

        [TestCase("gafas")]
        [TestCase("candado")]
        public void El_arte_viejo_queda_apagado(string sceneObject)
        {
            List<Seen> found = Collect(g => g.name == sceneObject);

            Assert.IsTrue(found.All(s => !s.Active),
                $"'{sceneObject}' sigue encendido: el aprendiz veria dos en la mesa.");
        }
    }
}
