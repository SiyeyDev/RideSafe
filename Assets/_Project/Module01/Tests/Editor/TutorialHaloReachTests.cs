using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RideSafe.Help;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// El halo del tutorial sobre los EPP (PO, 2026-10-26: «los EPP no tienen de nuevo el outline
    /// para identificar los clickeables»).
    /// <para>
    /// El tutorial solo resalta objetos hasta <see cref="TargetScanner.MaxObjectDistance"/>, y las
    /// mesas del Excel dejaron EPP a 4,05–4,88 m del punto de vista de su estación: con 4 m, media
    /// mesa de preparación se quedaba sin halo. Los EPP sin modelo (el signo «!») no tienen malla
    /// que delinear y se quedan fuera: los señala su propio signo.
    /// </para>
    /// </summary>
    public class TutorialHaloReachTests
    {
        private const string ScenePath = "Assets/_Project/Scene/RideSafe_Garaje_v5.unity";

        [Test]
        public void Desde_cada_estacion_el_tutorial_alcanza_todos_los_EPP_de_su_mesa()
        {
            WithScene(all =>
            {
                var far = new List<string>();
                foreach (ZoneSO zone in Catalog(all).Zones)
                {
                    Transform anchor = all.First(t => t.name == "anchor_" + zone.ZoneId);
                    foreach (WorldObjectTaskEntity entity in Modelled(all, zone))
                    {
                        // Al alcance, no a la vista: el escáner mira desde la cabeza real, y la
                        // mochila de la mesa del centro está en el suelo, por debajo del encuadre.
                        Vector3 at = entity.GetComponentInChildren<Collider>().transform.position;
                        float distance = Vector3.Distance(anchor.position, at);
                        if (distance > TargetScanner.MaxObjectDistance)
                            far.Add($"{zone.ZoneId}/{entity.ItemId} at {distance:0.00} m");
                    }
                }
                Assert.IsEmpty(far, "out of the tutorial's reach, so never haloed:\n  " + string.Join("\n  ", far));
            });
        }

        [Test]
        public void Desde_fuera_del_garaje_ningun_EPP_queda_al_alcance()
        {
            // Why the limit exists: from module 0's spot outside, the EPP are in view through the open
            // door (which has no collider) and must not be haloed before the learner gets there.
            WithScene(all =>
            {
                Vector3 outside = all.First(t => t.name == "Desktop Camera").position;
                foreach (ZoneSO zone in Catalog(all).Zones)
                    foreach (WorldObjectTaskEntity entity in Modelled(all, zone))
                        Assert.Greater(Vector3.Distance(outside, entity.transform.position), TargetScanner.MaxObjectDistance + .5f,
                                       entity.ItemId + " is within the tutorial's reach from outside the garage");
            });
        }

        private static IEnumerable<WorldObjectTaskEntity> Modelled(Transform[] all, ZoneSO zone) =>
            zone.Items
                .Select(item => all.Select(t => t.GetComponent<WorldObjectTaskEntity>())
                                   .FirstOrDefault(e => e != null && e.ItemId == item.ItemId))
                .Where(e => e != null && e.GetComponentInChildren<Collider>() != null &&
                            e.GetComponentsInChildren<MeshRenderer>(false).Any(r => r.enabled));

        private static Module01CatalogSO Catalog(Transform[] all)
        {
            Module01Director director = all.Select(t => t.GetComponent<Module01Director>()).First(d => d != null);
            return (Module01CatalogSO)new SerializedObject(director).FindProperty("_catalog").objectReferenceValue;
        }

        private static void WithScene(System.Action<Transform[]> check)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.isLoaded;
            if (opened)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                check(scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray());
            }
            finally
            {
                if (opened)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
