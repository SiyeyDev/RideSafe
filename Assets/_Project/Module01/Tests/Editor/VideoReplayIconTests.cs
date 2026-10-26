using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// «Repetir» del video con el icono redibujado: el del atlas tenía el aro más fino que la
    /// pausa y la flecha suelta, y no se leía como repetir (PO, 2026-10-26). Contra la escena v5,
    /// que es donde una sobrescritura de la instancia lo habría tapado.
    /// </summary>
    public class VideoReplayIconTests
    {
        private const string ScenePath = "Assets/_Project/Scene/RideSafe_Garaje_v5.unity";

        [Test]
        public void El_boton_repetir_del_video_usa_el_icono_redibujado()
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                Transform replay = scene.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                    .Single(t => t.name == "ReplayButton" && t.parent.name == "VideoCard");
                Image icon = replay.Find("Icon").GetComponent<Image>();
                Assert.IsNotNull(icon.sprite);
                Assert.AreEqual("Icon_Replay_Round", icon.sprite.name);
                Assert.IsTrue(icon.gameObject.activeSelf);
            }
            finally
            {
                if (openedHere)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
