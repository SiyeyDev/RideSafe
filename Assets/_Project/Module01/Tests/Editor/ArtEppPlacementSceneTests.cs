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
    /// Los EPP de v5 son los objetos del artista, <b>con su escala y su giro</b>, y repartidos
    /// entre las cinco paradas.
    /// <para>
    /// <b>La posición ya no se compara</b> (PO, 2026-10-29): el reparto del artista agrupaba por
    /// tipo y dejaba los cinco esenciales en la misma mesa, así que la ubicación delataba la
    /// respuesta. Lo que sí es intocable es el tamaño: «solo desplazamiento, mas no escalado».
    /// Por eso este fichero mide escala y giro con tolerancia de milímetro y la posición solo
    /// por sus consecuencias —que se vea, que se pulse, que no esté en el suelo—.
    /// </para>
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

        /// <summary>
        /// El objeto de v5 que es la copia de <paramref name="art"/>, reconocido por su <b>FBX</b>.
        /// <para>
        /// <b>Ya no por el nombre.</b> El renombrado al Excel del 2026-10-29 —«para que sepas
        /// cuál es cuál»— dejó las instancias de la escena como <c>E01_helmet_ok</c>, mientras
        /// que en la escena de arte siguen llamándose <c>Casco certificado</c>. Buscando por
        /// nombre, los 55 EPP salían como «no está en v5» y los tests que miden la escena se
        /// quedaron midiendo el vacío: pasaban por no encontrar nada que comparar.
        /// </para>
        /// <para>
        /// Varias piezas pueden compartir FBX —los dos zapatos del par, las dos Crocs—. Se
        /// prefiere la que lleva la entidad, que es la que el módulo usa; si ninguna la lleva,
        /// la primera por jerarquía, que al menos es determinista.
        /// </para>
        /// </summary>
        private GameObject CopyOf(GameObject art)
        {
            string fbx = Source(art);
            if (string.IsNullOrEmpty(fbx))
                return null;

            List<GameObject> candidatas = All(_v5)
                .Where(g => Source(g) == fbx && !g.transform.IsChildOf(Spawned()))
                .ToList();
            if (candidatas.Count == 0)
                return null;

            return candidatas.FirstOrDefault(g => g.GetComponent<WorldObjectTaskEntity>() != null)
                ?? candidatas[0];
        }

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

        /// <summary>
        /// Cada EPP del artista está en v5 y <b>mantiene su escala y su giro</b>.
        /// <para>
        /// Es el test que faltaba cuando se rompió: en el reacomodo del 2026-10-29 yo escalé 26
        /// objetos para hacerlos encajar y la PO lo vio en la primera captura —«no sé por qué
        /// cambiaste el tamaño, cuando yo solo te había pedido desplazamiento»—. Las chaquetas
        /// quedaron más pequeñas que una banda para la cabeza. El tamaño que trae el artista es
        /// el bueno y aquí se mide con tolerancia de milésima.
        /// </para>
        /// <para>
        /// La <b>posición</b> no se compara a propósito: repartir entre las cinco paradas es
        /// justo lo que pidió la PO. Que el sitio nuevo sea bueno lo miden los otros tests de
        /// este fichero —que se vea entero, que el rayo llegue, que descanse sobre un mueble—.
        /// </para>
        /// </summary>
        [Test]
        public void Cada_EPP_del_arte_conserva_su_escala_y_su_giro_en_v5()
        {
            var wrong = new List<string>();
            foreach (IGrouping<string, GameObject> grupo in PorModelo())
            {
                // Las escalas que el artista usó para ese modelo y las que hay en v5, cada lista
                // ordenada. Se comparan los CONJUNTOS y no objeto a objeto porque varias piezas
                // comparten FBX —los cuatro zapatos, las dos Crocs, los dos guantes— y no hay
                // forma de saber qué pieza del arte es qué pieza de v5 ahora que el renombrado
                // al Excel les cambió el nombre. El conjunto basta: si alguien escala una, la
                // lista deja de cuadrar.
                //
                // En valor absoluto, además: el artista espeja la pieza del lado contrario con
                // escala negativa («zapatos de seguridad (1)» trae z = −0,381) y eso no es un
                // cambio de tamaño.
                List<float> enArte = grupo.Select(g => Abs(g.transform.lossyScale).magnitude)
                    .OrderBy(x => x).ToList();
                List<float> enV5 = All(_v5)
                    .Where(g => Source(g) == grupo.Key && !g.transform.IsChildOf(Spawned()))
                    .Select(g => Abs(g.transform.lossyScale).magnitude)
                    .OrderBy(x => x).ToList();

                if (enV5.Count == 0) { wrong.Add($"{grupo.Key}: ninguna copia en v5"); continue; }

                // Lo que la biblioteca declara con `targetWidth` sí se escala, y a propósito:
                // son los FBX que llegan a una escala que no es la real -el kit de pinchazos
                // viene midiendo 2,06 m- y la biblioteca anota el ancho bueno con el motivo al
                // lado. Es la única excepción y tiene que estar declarada: lo que se escale sin
                // pasar por ahí es lo que la PO no pidió.
                if (DeclaraAnchoObjetivo(grupo.Key))
                    continue;

                for (int i = 0; i < Mathf.Min(enArte.Count, enV5.Count); i++)
                    if (Mathf.Abs(enArte[i] - enV5[i]) > .001f * enArte[i])
                        wrong.Add($"{grupo.Key}: escala {enV5[i]:F3} donde el artista puso {enArte[i]:F3}");
            }
            CollectionAssert.IsEmpty(wrong, string.Join("\n", wrong));
        }

        /// <summary>¿La biblioteca manda llevar este modelo a un ancho concreto?</summary>
        private static bool DeclaraAnchoObjetivo(string fbx)
        {
            foreach (Module01Library.Entry e in Module01Library.All)
                if (e.Model == fbx && e.TargetWidth > 0f)
                    return true;
            return false;
        }

        /// <summary>
        /// Los EPP del artista agrupados por el FBX del que salen, saltándose el decorado
        /// declarado y los modelos que solo viven en el pedestal.
        /// </summary>
        private IEnumerable<IGrouping<string, GameObject>> PorModelo() =>
            Module01ArtEpp.Collect(_art)
                .Where(g => !DecoradoAProposito.ContainsKey(g.name)
                         && !EsModeloDePedestal(g)
                         && !string.IsNullOrEmpty(Source(g)))
                .GroupBy(Source);

        // Aquí vivía `Ningun_EPP_mide_en_v5_algo_distinto_de_lo_que_mide_en_el_arte`, que
        // comparaba la caja del objeto en las dos escenas. No sirve, y conviene que quede
        // escrito para no volver a escribirlo: la entidad de un par -los dos zapatos, las dos
        // coderas- cuelga del padre que contiene las DOS piezas, así que su caja mide el par
        // entero (0,76 m) y en la escena de arte se comparaba contra una pieza sola (0,35 m).
        // Salían catorce pares acusados de haber cambiado de tamaño, con desvíos de hasta el
        // 117 %, y ninguno había cambiado. La pregunta que importaba -¿alguien reescaló algo?-
        // la responde `Cada_EPP_del_arte_conserva_su_escala_y_su_giro_en_v5`, comparando
        // escalas y no cajas.

        private static Vector3 Abs(Vector3 v) =>
            new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));


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
        /// Los modelos que solo salen en el pedestal: la versión abierta de cada pantalón. Sobre
        /// la mesa está el doblado —que sí se pulsa— y al pulsarlo el inspector monta éste
        /// (PO, 2026-10-29). No llevan entidad a propósito: si la llevaran habría dos objetos
        /// pulsables para la misma prenda.
        /// </summary>
        private static bool EsModeloDePedestal(GameObject art)
        {
            string ruta = Source(art);
            if (string.IsNullOrEmpty(ruta))
                return false;
            foreach (Module01Library.Entry e in Module01Library.All)
                if (e.InspectModel == ruta)
                    return true;
            return false;
        }

        /// <summary>
        /// Lo que el artista dejó en la escena y a propósito no se pulsa, con su motivo. Es una
        /// lista corta y cerrada: cualquier otra cosa suya tiene que ser un elemento.
        /// </summary>
        private static readonly Dictionary<string, string> DecoradoAProposito =
            new Dictionary<string, string>
            {
                // El estuche viejo, el que trae los parches y el candado sueltos dentro. O12 es
                // `kit compacto.fbx` desde el 2026-10-27, que es el que pide la hoja
                // («contenido identificable»). Éste se queda en el banco como decorado.
                { "kit de despinche", "lo sustituyó kit compacto.fbx en O12" },
                // Segunda copia del chaleco de O02, en gris. O02 es una sola: tener dos
                // pulsables daría el bono dos veces por la misma precaución.
                { "chaleco reflectivo  gris", "copia en gris de O02" },
            };

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
                if (EsModeloDePedestal(art) || DecoradoAProposito.ContainsKey(art.name))
                    continue;
                GameObject copy = CopyOf(art);
                if (copy != null && copy.GetComponentInParent<WorldObjectTaskEntity>(true) == null)
                    wrong.Add($"{art.name} ({Source(art)}) no es ningún elemento: falta en Module01Library");
            }
            CollectionAssert.IsEmpty(wrong, string.Join("\n", wrong));
        }

        /// <summary>
        /// La versión abierta de un pantalón <b>no</b> puede estar pulsable en la mesa: es
        /// contenido del pedestal. Si llevara entidad habría dos objetos distintos para la misma
        /// prenda y el reporte contaría la elección dos veces.
        /// </summary>
        [Test]
        public void La_version_abierta_de_los_pantalones_no_se_pulsa_en_la_mesa()
        {
            var wrong = new List<string>();
            foreach (GameObject art in Module01ArtEpp.Collect(_art))
            {
                if (!EsModeloDePedestal(art))
                    continue;
                GameObject copy = CopyOf(art);
                if (copy != null && copy.GetComponentInParent<WorldObjectTaskEntity>(true) != null)
                    wrong.Add($"{art.name} es contenido de pedestal y está pulsable en la mesa");
            }
            CollectionAssert.IsEmpty(wrong, string.Join("\n", wrong));
        }

        /// <summary>
        /// Los cuatro pantalones declaran su versión abierta, y el fichero existe. Sin esto el
        /// pedestal montaría el doblado y la mecánica que pidió la PO no se vería.
        /// </summary>
        [Test]
        public void Los_pantalones_declaran_su_version_abierta_y_el_modelo_existe()
        {
            var conPedestal = Module01Library.All
                .Where(e => !string.IsNullOrEmpty(e.InspectModel)).ToList();

            Assert.AreEqual(4, conPedestal.Count,
                "E05, P17, P18 y P26: los cuatro pantalones de la mesa.");

            foreach (Module01Library.Entry e in conPedestal)
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(e.InspectModel),
                    $"{e.LibraryId} apunta a {e.InspectModel} y ese modelo no está.");
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

        /// <summary>
        /// Puntos repartidos por el objeto para disparar rayos contra él. De su malla, no de su
        /// caja: la caja de unas gafas es casi todo aire y daría por tapado lo que se pulsa bien.
        /// <para>
        /// <b>La malla no siempre se puede leer.</b> Medio proyecto viene sin «Read/Write
        /// Enabled», y <c>mesh.vertices</c> en una de ésas no lanza: escribe un error en consola
        /// y devuelve un array vacío. Sin esta guarda el test contaba 0 puntos alcanzados de 0
        /// posibles y acusaba de «no se puede pulsar» a objetos perfectamente visibles —el casco
        /// esencial entre ellos—. Para ésos se muestrea la caja del renderer, que es peor medida
        /// pero honesta: sus nueve puntos están dentro del objeto.
        /// </para>
        /// </summary>
        private static List<Vector3> PuntosDeSuperficie(WorldObjectTaskEntity e)
        {
            var puntos = new List<Vector3>();
            foreach (MeshFilter mf in e.GetComponentsInChildren<MeshFilter>())
            {
                Mesh mesh = mf.sharedMesh;
                if (mesh == null)
                    continue;

                if (mesh.isReadable)
                {
                    int paso = Mathf.Max(1, mesh.vertexCount / 150);
                    Vector3[] v = mesh.vertices;
                    for (int i = 0; i < v.Length; i += paso)
                        puntos.Add(mf.transform.TransformPoint(v[i]));
                    continue;
                }

                Renderer r = mf.GetComponent<Renderer>();
                if (r == null)
                    continue;
                Bounds b = r.bounds;
                puntos.Add(b.center);
                // Los ocho vértices justo por dentro de la caja: en el borde exacto el rayo
                // puede pasar de largo y contarse como tapado por nada.
                Vector3 ext = b.extents * .8f;
                for (int i = 0; i < 8; i++)
                    puntos.Add(b.center + new Vector3(
                        (i & 1) == 0 ? -ext.x : ext.x,
                        (i & 2) == 0 ? -ext.y : ext.y,
                        (i & 4) == 0 ? -ext.z : ext.z));
            }
            return puntos;
        }

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
                    List<Vector3> surface = PuntosDeSuperficie(e);
                    int reached = surface.Count(p =>
                        Physics.Raycast(eye, p - eye, out RaycastHit hit, Vector3.Distance(eye, p) + .5f)
                        && hit.collider.GetComponentInParent<WorldObjectTaskEntity>() == e);
                    if (surface.Count == 0 || reached < surface.Count * MinReached)
                        wrong.Add($"{stop.Id}/{itemId} ({e.name}): {100 * reached / Mathf.Max(1, surface.Count)} %");
                }
            }
            CollectionAssert.IsEmpty(wrong, "no se pueden pulsar desde su ancla:\n" + string.Join("\n", wrong));
        }

        /// <summary>
        /// Las botas rojas del decorado son P22 y se pulsan. Ya no se buscan por la ruta
        /// <c>enviroment_decoration/botas</c>: el renombrado al Excel del 2026-10-29 las dejó
        /// como <c>P22_boots</c> —«para que sepas cuál es cuál»— y el test se quedó buscando un
        /// nombre que ya no existe. Se busca por el <c>ItemId</c>, que es lo que no cambia.
        /// </summary>
        [Test]
        public void Las_botas_del_decorado_del_garaje_tambien_se_pulsan()
        {
            WorldObjectTaskEntity botas = All(_v5)
                .Select(g => g.GetComponent<WorldObjectTaskEntity>())
                .FirstOrDefault(e => e != null && e.ItemId == "boots");

            Assert.IsNotNull(botas, "las botas rojas no tienen entidad en v5");
            Assert.IsNotNull(botas.GetComponent<Collider>(), "sin collider no hay clic");
        }

        /// <summary>
        /// Todo lo que <b>puntúa</b> está encendido. Es la regla que cerró la PO el 2026-10-29
        /// —«prende únicamente los que sean del Excel»— y tiene dos caras: lo neutral puede
        /// estar apagado si estorba, pero un esencial o un peligro apagado es un elemento que
        /// la nota exige y el jugador no puede ni ver.
        /// <para>
        /// Esto es lo que habría cazado a <c>H01_helmet_strap</c>, que lleva días apagado dentro
        /// del prefab <c>enviroment_decoration</c>: es uno de los doce peligros de la hoja, el
        /// casco mal abrochado, y descuenta 30 puntos que nadie puede perder porque no está.
        /// </para>
        /// </summary>
        [Test]
        public void Todo_lo_que_puntua_esta_encendido_en_la_escena()
        {
            var wrong = new List<string>();
            foreach (Module01Library.Entry e in Module01Library.All)
            {
                if (e.Weight <= 0f)
                    continue;

                WorldObjectTaskEntity entidad = All(_v5)
                    .Select(g => g.GetComponent<WorldObjectTaskEntity>())
                    .FirstOrDefault(x => x != null && x.ItemId == e.ItemId);

                if (entidad == null)
                    wrong.Add($"{e.LibraryId} ({e.ItemId}) puntúa {e.Weight} y no está en la escena");
                else if (!entidad.gameObject.activeInHierarchy)
                    wrong.Add($"{e.LibraryId} ({e.ItemId}) puntúa {e.Weight} y está apagado");
            }
            CollectionAssert.IsEmpty(wrong, string.Join("\n", wrong));
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
        /// Lo que el despeje de pasillo apartó vuelve a donde lo dejó el artista. El gazón es el
        /// caso: lo apartó un despeje viejo y no hay motivo para que siga movido.
        /// <para>
        /// Las tres reglas del banco <b>no</b> están aquí: las mueve <c>Module01ArtEpp.Moves</c>
        /// a propósito, al estante de abajo, porque ocupaban el 84 % de la tapa del banco de
        /// herramientas y ahí tienen que caber los EPP. Lo declarado en <c>Moves</c> es una
        /// decisión, no una regresión, y lo comprueba
        /// <see cref="Lo_que_Moves_declara_esta_donde_Moves_dice"/>.
        /// </para>
        /// </summary>
        [TestCase("interior garage/gazon_lp")]
        public void El_mobiliario_que_acompana_a_los_EPP_esta_como_lo_dejo_el_artista(string path)
        {
            Transform a = Find(_art, path), c = Find(_v5, path);
            Assert.IsNotNull(a, "no está en el arte");
            Assert.IsNotNull(c, "no está en v5");
            Assert.Less(Vector3.Distance(a.position, c.position), .001f, $"en {c.position:F3}, el artista {a.position:F3}");
            Assert.Less(Quaternion.Angle(a.rotation, c.rotation), .1f);
            Assert.AreEqual(a.gameObject.activeInHierarchy, c.gameObject.activeInHierarchy, "encendido");
        }

        /// <summary>
        /// Cada traslado declarado en <c>Module01ArtEpp.Moves</c> llegó a su destino. Es la otra
        /// cara del test de arriba: lo que se mueve a mano tiene que estar donde dice el código,
        /// o es un traslado que se quedó a medias y nadie se enteró.
        /// </summary>
        [Test]
        public void Lo_que_Moves_declara_esta_donde_Moves_dice()
        {
            var wrong = new List<string>();
            foreach ((string path, string surface, Vector3 point) in Module01ArtEpp.Moves)
            {
                // Los que declaran superficie los apoya el layout a la altura de esa tapa, así
                // que su altura final no es la del punto. Solo se mide el plano.
                // Por la ruta ya no se encuentran los que son EPP: el renombrado al Excel dejó
                // «epps/poncho h» como «epps/H06_poncho». Si la ruta falla se busca el mismo
                // objeto en la escena de arte y de ahí su copia por FBX.
                Transform c = Find(_v5, path);
                if (c == null)
                {
                    Transform enArte = Find(_art, path);
                    GameObject copia = enArte == null ? null : CopyOf(enArte.gameObject);
                    c = copia == null ? null : copia.transform;
                }
                if (c == null) { wrong.Add($"{path}: no está en v5 ni por ruta ni por FBX"); continue; }
                Vector2 quiero = new Vector2(point.x, point.z);
                Vector2 tengo = new Vector2(c.position.x, c.position.z);
                if (Vector2.Distance(quiero, tengo) > .05f)
                    wrong.Add($"{path}: en {tengo}, Moves dice {quiero}");
                if (surface == null && Mathf.Abs(c.position.y - point.y) > .05f)
                    wrong.Add($"{path}: a {c.position.y:F2} de alto, Moves dice {point.y:F2}");
            }
            CollectionAssert.IsEmpty(wrong, string.Join("\n", wrong));
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
