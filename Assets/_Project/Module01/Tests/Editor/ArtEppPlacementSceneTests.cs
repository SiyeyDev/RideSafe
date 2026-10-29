using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RideSafe.Module01.Editor;
using RideSafe.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Los EPP de v5 están exactamente donde el artista los dejó en <c>Garage.unity</c> (PO,
    /// 2026-10-27): misma posición, giro y escala, y las mismas distancias entre ellos. Los que
    /// la hoja del cliente evalúa se pulsan sobre ese mismo objeto; los demás son decorado.
    /// </summary>
    public class ArtEppPlacementSceneTests
    {
        private const string V5Path = "Assets/_Project/Scene/RideSafe_Garaje_v5.unity";
        private const string SpawnedRoot = "Module01_Spawned";

        private Scene _v5, _art;
        private bool _v5Here, _artHere;

        [OneTimeSetUp]
        public void Open()
        {
            _v5 = SceneManager.GetSceneByPath(V5Path);
            _v5Here = !_v5.isLoaded;
            if (_v5Here)
                _v5 = EditorSceneManager.OpenScene(V5Path, OpenSceneMode.Additive);
            _art = SceneManager.GetSceneByPath(Module01ArtEpp.ArtScene);
            _artHere = !_art.isLoaded;
            if (_artHere)
                _art = EditorSceneManager.OpenScene(Module01ArtEpp.ArtScene, OpenSceneMode.Additive);
        }

        [OneTimeTearDown]
        public void Close()
        {
            if (_artHere)
                EditorSceneManager.CloseScene(_art, true);
            if (_v5Here)
                EditorSceneManager.CloseScene(_v5, true);
        }

        private static string Source(GameObject g) =>
            AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(g));

        private static IEnumerable<GameObject> All(Scene scene) => scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject);

        /// <summary>El objeto de v5 que es la copia de <paramref name="art"/>: mismo nombre y mismo FBX.</summary>
        private GameObject CopyOf(GameObject art) => All(_v5).FirstOrDefault(g =>
            g.name == art.name && Source(g) == Source(art) && !g.transform.IsChildOf(Spawned()));

        private Transform Spawned() =>
            All(_v5).First(g => g.name == SpawnedRoot).transform;

        [TestCase("Casco certificado")]
        [TestCase("gafas de sol")]
        [TestCase("mochila")]
        [TestCase("zapatos de seguridad (3)")]
        [TestCase("codera")]
        [TestCase("tenis_deportivos")]
        [TestCase("snorquel")]
        [TestCase("gafas")]
        [TestCase("rompe veintos")]
        [TestCase("Crocs")]
        [TestCase("poncho h")]
        [TestCase("gorra_")]
        [TestCase("soporte botella bici")]
        public void El_arte_cuenta_como_EPP(string name)
        {
            CollectionAssert.Contains(Module01ArtEpp.Collect(_art).Select(g => g.name).ToList(), name);
        }

        [TestCase("crash_dummy")]
        [TestCase("child bike")]
        [TestCase("pliers")]
        [TestCase("cartuchera_low")]
        [TestCase("Tool_Tajima_lowpoly_02")]
        [TestCase("E_Scooter")]
        [TestCase("ebike")]
        public void Lo_que_no_es_EPP_no_se_copia(string name)
        {
            CollectionAssert.DoesNotContain(Module01ArtEpp.Collect(_art).Select(g => g.name).ToList(), name);
        }

        [Test]
        public void Cada_EPP_del_arte_esta_en_v5_en_la_misma_posicion_giro_y_escala()
        {
            var wrong = new List<string>();
            foreach (GameObject art in Module01ArtEpp.Collect(_art))
            {
                GameObject copy = CopyOf(art);
                if (copy == null) { wrong.Add($"{art.name}: no está en v5"); continue; }
                // Lo que la PO pidió mover (el poncho) es la única excepción, y está declarada.
                if (Module01ArtEpp.Moves.Any(m => m.Path.EndsWith("/" + art.name))) continue;
                Transform a = art.transform, c = copy.transform;
                if (Vector3.Distance(a.position, c.position) > .001f)
                    wrong.Add($"{art.name}: en {c.position:F3}, el artista lo dejó en {a.position:F3}");
                if (Quaternion.Angle(a.rotation, c.rotation) > .1f)
                    wrong.Add($"{art.name}: girado {c.eulerAngles:F1}, el artista {a.eulerAngles:F1}");
                if ((a.lossyScale - c.lossyScale).magnitude > .001f * a.lossyScale.magnitude)
                    wrong.Add($"{art.name}: escala {c.lossyScale:F3}, el artista {a.lossyScale:F3}");
                if (a.gameObject.activeInHierarchy != c.gameObject.activeInHierarchy)
                    wrong.Add($"{art.name}: encendido={c.gameObject.activeInHierarchy}, en el arte {a.gameObject.activeInHierarchy}");
            }
            CollectionAssert.IsEmpty(wrong, string.Join("\n", wrong));
        }

        [Test]
        public void Cada_elemento_que_el_artista_coloco_se_pulsa_sobre_su_objeto()
        {
            List<GameObject> art = Module01ArtEpp.Collect(_art).ToList();
            var wrong = new List<string>();
            foreach (Module01Library.Entry e in Module01Library.All)
            {
                if (e.NeedsMarker || !art.Any(g => Source(g) == e.Model))
                    continue;
                WorldObjectTaskEntity[] mine = All(_v5)
                    .Select(g => g.GetComponent<WorldObjectTaskEntity>())
                    .Where(x => x != null && x.ItemId == e.ItemId).ToArray();
                if (mine.Length != 1) { wrong.Add($"{e.ItemId}: {mine.Length} entidades"); continue; }
                if (Source(mine[0].gameObject) != e.Model)
                    wrong.Add($"{e.ItemId}: se pulsa sobre {Source(mine[0].gameObject)}");
                if (mine[0].transform.IsChildOf(Spawned()))
                    wrong.Add($"{e.ItemId}: es una copia instanciada, no el objeto del artista");
            }
            CollectionAssert.IsEmpty(wrong, string.Join("\n", wrong));
        }

        /// <summary>
        /// «Todos los que ha hecho el artista son interactuables» (PO, 2026-10-28): cada EPP suyo
        /// es un elemento o una pieza de uno. Lo que la hoja no evalúa es neutral, no decorado.
        /// </summary>
        [Test]
        public void Todo_EPP_del_artista_se_puede_pulsar()
        {
            var wrong = new List<string>();
            foreach (GameObject art in Module01ArtEpp.Collect(_art))
            {
                GameObject copy = CopyOf(art);
                if (copy != null && copy.GetComponentInParent<WorldObjectTaskEntity>(true) == null)
                    wrong.Add($"{art.name} ({Source(art)}) no es ningún elemento: falta en Module01Library");
            }
            CollectionAssert.IsEmpty(wrong, string.Join("\n", wrong));
        }

        /// <summary>
        /// Lo que vio la PO (2026-10-28): coderas que no se podían pulsar porque el collider de
        /// las rodilleras, delante, era mucho mayor que ellas, y un candado suelto que no había
        /// forma de pulsar dentro de la caja del candado de cable. Desde el ancla de su parada, el
        /// rayo del puntero tiene que llegar a <b>buena parte</b> de cada elemento: con un punto
        /// suelto se puede, pero quien juega cree que falla.
        /// </summary>
        /// <summary>La parte de la superficie a la que tiene que llegar el rayo desde la parada.</summary>
        private const float MinReached = .5f;

        [Test]
        public void Desde_su_parada_el_rayo_llega_a_cada_elemento()
        {
            Physics.SyncTransforms();
            var wrong = new List<string>();
            foreach (Module01Library.Stop stop in Module01Library.Stops)
            {
                Vector3 eye = All(_v5).First(g => g.name == "anchor_" + stop.Id).transform.position;
                foreach (string itemId in stop.ItemIds)
                {
                    WorldObjectTaskEntity e = All(_v5).Select(g => g.GetComponent<WorldObjectTaskEntity>())
                        .FirstOrDefault(x => x != null && x.ItemId == itemId);
                    if (e == null || e.GetComponent<Collider>() == null)
                        continue;
                    // Puntos de su superficie, no de su caja: la caja de unas gafas es casi aire.
                    List<Vector3> surface = e.GetComponentsInChildren<MeshFilter>()
                        .Where(mf => mf.sharedMesh != null)
                        .SelectMany(mf => mf.sharedMesh.vertices
                            .Where((_, i) => i % Mathf.Max(1, mf.sharedMesh.vertexCount / 150) == 0)
                            .Select(v => mf.transform.TransformPoint(v)))
                        .ToList();
                    int reached = surface.Count(p =>
                        Physics.Raycast(eye, p - eye, out RaycastHit hit, Vector3.Distance(eye, p) + .5f)
                        && hit.collider.GetComponentInParent<WorldObjectTaskEntity>() == e);
                    if (surface.Count == 0 || reached < surface.Count * MinReached)
                        wrong.Add($"{stop.Id}/{itemId} ({e.name}): {100 * reached / Mathf.Max(1, surface.Count)} %");
                }
            }
            CollectionAssert.IsEmpty(wrong, "no se pueden pulsar desde su ancla:\n" + string.Join("\n", wrong));
        }

        [Test]
        public void Las_botas_del_decorado_del_garaje_tambien_se_pulsan()
        {
            Transform boots = Find(_v5, "enviroment_decoration/botas");
            Assert.IsNotNull(boots);
            Assert.IsNotNull(boots.GetComponentInParent<WorldObjectTaskEntity>(true), "las botas rojas no tienen entidad");
        }

        [Test]
        public void Los_signos_no_pisan_lo_que_puso_el_artista()
        {
            List<Bounds> art = Module01ArtEpp.Collect(_art)
                .SelectMany(g => g.GetComponentsInChildren<Renderer>(false)).Select(r => r.bounds).ToList();
            var wrong = new List<string>();
            foreach (WorldObjectTaskEntity e in Spawned().GetComponentsInChildren<WorldObjectTaskEntity>(false))
            {
                if (!e.Id.Value.StartsWith("module01.") || e.ItemId.StartsWith("point_"))
                    continue;
                foreach (Renderer r in e.GetComponentsInChildren<Renderer>(false))
                    if (art.Any(b => b.Intersects(r.bounds)))
                        wrong.Add($"{e.ItemId} cae encima de un EPP del artista");
            }
            CollectionAssert.IsEmpty(wrong.Distinct(), string.Join("\n", wrong.Distinct()));
        }

        /// <summary>
        /// Las paradas son muebles (PO, 2026-10-27): un EPP del artista tiene que estar en el
        /// mueble de su parada —el más cercano de todos—, o se elegiría mirando a otro sitio. Si
        /// el artista lo mueve, esto avisa de que hay que cambiarlo de parada en Module01Library.
        /// </summary>
        [Test]
        public void Cada_EPP_del_artista_esta_en_el_mueble_de_su_parada()
        {
            var furniture = Module01Stations.All.ToDictionary(s => s.ZoneId,
                s => s.Surfaces.Select(top => Box(All(_v5).First(g => g.name == top.Name))).ToList());
            var wrong = new List<string>();
            foreach (Module01Library.Stop stop in Module01Library.Stops)
                foreach (string itemId in stop.ItemIds)
                {
                    WorldObjectTaskEntity e = All(_v5).Select(g => g.GetComponent<WorldObjectTaskEntity>())
                        .FirstOrDefault(x => x != null && x.ItemId == itemId);
                    if (e == null || e.transform.IsChildOf(Spawned()))
                        continue;   // los signos se colocan solos sobre su mueble
                    Vector3 at = Box(e.gameObject).center;
                    string nearest = furniture.OrderBy(f => f.Value.Min(b => Flat(b.ClosestPoint(at) - at))).First().Key;
                    if (nearest != stop.Id)
                        wrong.Add($"{itemId} está en el mueble de '{nearest}', no en el de '{stop.Id}'");
                }
            CollectionAssert.IsEmpty(wrong, string.Join("\n", wrong));
        }

        private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        [Test]
        public void Cada_signo_descansa_sobre_un_mueble_de_su_mesa()
        {
            Module01CatalogSO catalog = AssetDatabase.LoadAssetAtPath<Module01CatalogSO>(
                "Assets/_Project/Module01/Data/SO_Module01Catalog.asset");
            var wrong = new List<string>();
            foreach (ZoneSO zone in catalog.Zones)
            {
                Assert.IsTrue(Module01Stations.TryGet(zone.ZoneId, out Station station), zone.ZoneId);
                foreach (WorldObjectTaskEntity e in Spawned().GetComponentsInChildren<WorldObjectTaskEntity>(true))
                {
                    if (!zone.Items.Any(i => i != null && i.ItemId == e.ItemId))
                        continue;
                    Bounds box = Box(e.gameObject);
                    bool onOne = station.Surfaces.Any(s =>
                    {
                        Bounds top = s.Clip(Box(All(_v5).First(g => g.name == s.Name)));
                        return box.center.x > top.min.x && box.center.x < top.max.x
                               && box.center.z > top.min.z && box.center.z < top.max.z
                               && Mathf.Abs(box.min.y - s.WorktopHeight) < .01f;
                    });
                    if (!onOne)
                        wrong.Add($"{zone.ZoneId}/{e.ItemId} en {box.center:F2}, base a {box.min.y:F3}");
                }
            }
            CollectionAssert.IsEmpty(wrong, string.Join("\n", wrong));
        }

        private static Bounds Box(GameObject go)
        {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>
        /// Lo que el despeje de pasillo apartó —el gazón, la regla, el medidor— y el arte viejo
        /// que estaba apagado vuelven a donde los dejó el artista.
        /// </summary>
        [TestCase("interior garage/gazon_lp")]
        [TestCase("enviroment_decoration/tools herramientas/regla")]
        [TestCase("enviroment_decoration/tools herramientas/regla_l")]
        [TestCase("enviroment_decoration/tools herramientas/medidor1")]
        [TestCase("enviroment_decoration/botas")]
        public void El_mobiliario_que_acompana_a_los_EPP_esta_como_lo_dejo_el_artista(string path)
        {
            Transform a = Find(_art, path), c = Find(_v5, path);
            Assert.IsNotNull(a, "no está en el arte");
            Assert.IsNotNull(c, "no está en v5");
            Assert.Less(Vector3.Distance(a.position, c.position), .001f, $"en {c.position:F3}, el artista {a.position:F3}");
            Assert.Less(Quaternion.Angle(a.rotation, c.rotation), .1f);
            Assert.AreEqual(a.gameObject.activeInHierarchy, c.gameObject.activeInHierarchy, "encendido");
        }

        private static Transform Find(Scene scene, string path)
        {
            string[] parts = path.Split('/');
            foreach (GameObject top in SceneFolder.TopLevelObjects(scene))
                if (top.name == parts[0])
                    return parts.Length == 1 ? top.transform : top.transform.Find(string.Join("/", parts.Skip(1)));
            return null;
        }
    }
}
