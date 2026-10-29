using System.Collections.Generic;
using System.Linq;
using RideSafe.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Pone los EPP de la escena abierta exactamente donde el artista los dejó en
    /// <c>Garage.unity</c>: misma posición, giro y escala, y por tanto las mismas distancias entre
    /// ellos (PO, 2026-10-27, «es algo que antes ya te había pedido»).
    /// <para>
    /// <b>Se copian los objetos del artista, no se recolocan los nuestros.</b> Hasta ahora los EPP
    /// se instanciaban desde su FBX a un ancho declarado y se repartían en fila sobre tres
    /// muebles; eso tiraba el trabajo de colocación del artista en cada montaje. Ahora se traen
    /// sus objetos tal cual y el clic se monta sobre ellos (<see cref="Module01SceneEntities"/>).
    /// Lo que la hoja no evalúa viene igual, como decorado.
    /// </para>
    /// <para>
    /// Repetible: cada vez quita la copia anterior y trae la del arte, así que una entrega nueva
    /// del artista se refleja volviendo a correr el comando. La escena de arte <b>no se
    /// modifica</b>: se abre, se le sacan los objetos y se cierra sin guardar.
    /// </para>
    /// </summary>
    public static class Module01ArtEpp
    {
        public const string ArtScene = "Assets/_Project/Models/CITY/scenas/Garage.unity";

        /// <summary>Donde el artista agrupa los EPP, y donde se agrupan en la escena de trabajo.</summary>
        private const string EppRoot = "epps";

        private const string SpawnedRoot = "Module01_Spawned";

        /// <summary>
        /// Los muebles y el decorado sobre los que descansan los EPP. Se dejan como en el arte
        /// porque el despeje de pasillo de antes apartó piezas (el gazón, la regla, el medidor)
        /// y apagó el arte viejo que hay encima (botas, casco).
        /// </summary>
        private static readonly string[] WorldWithEpp = { "interior garage", "enviroment_decoration" };

        /// <summary>
        /// Los EPP de la escena de arte: todo lo que cuelga de su raíz <c>epps</c>, y lo suelto
        /// que es un modelo de la carpeta de esenciales o de la hoja. El maniquí, los vehículos y
        /// las herramientas del banco no lo son.
        /// </summary>
        public static List<GameObject> Collect(Scene art)
        {
            HashSet<string> models = DeclaredModels();
            var epp = new List<GameObject>();
            foreach (GameObject root in art.GetRootGameObjects())
            {
                if (IsEppContainer(root, models))
                {
                    foreach (Transform child in root.transform)
                        epp.Add(child.gameObject);
                    continue;
                }
                string source = SourceOf(root);
                if (models.Contains(source) || source.StartsWith(Module01Library.Essentials))
                    epp.Add(root);
            }
            return epp;
        }

        /// <summary>
        /// Si un objeto raíz de la escena de arte es una <b>caja de EPP</b>: un contenedor cuyos
        /// hijos son, en su mayoría, modelos que declara la biblioteca.
        /// <para>
        /// Hasta el 2026-10-29 esto era «se llama <c>epps</c>», y por eso nos dejábamos cuarenta
        /// objetos del artista. Él reorganizó su escena por la categoría del Excel —
        /// <c>EPPS_Esencial</c>, <c>EPPS_Recomendada condicional</c>, <c>Peligrosa visible</c>— y
        /// esos grupos no se llaman <c>epps</c>. El síntoma que lo delató: las chanclas y las
        /// Crocs salían <b>desparejadas</b>. Él puso los dos pies; nosotros no recogíamos
        /// ninguno y caíamos a instanciar una sola copia del FBX (PO, con captura).
        /// </para>
        /// <para>
        /// La regla es por contenido y no por nombre, para que el próximo grupo que se invente
        /// entre solo. Se exige mayoría para no tragarse <c>interior garage</c> ni
        /// <c>enviroment_decoration</c>, que son mobiliario y decorado con algún EPP suelto
        /// dentro; de ésos se sigue sacando pieza a pieza por su malla.
        /// </para>
        /// </summary>
        private static bool IsEppContainer(GameObject root, HashSet<string> models)
        {
            if (PrefabUtility.IsAnyPrefabInstanceRoot(root) || root.transform.childCount == 0)
                return false;
            if (root.GetComponent<Renderer>() != null)
                return false;   // tiene malla propia: es un objeto, no una caja

            int declarados = 0;
            foreach (Transform child in root.transform)
            {
                string source = SourceOf(child.gameObject);
                if (models.Contains(source) || source.StartsWith(Module01Library.Essentials))
                    declarados++;
            }
            return declarados * 2 > root.transform.childCount;
        }

        [MenuItem("RideSafe/Módulo 1/Colocar los EPP como en la escena de arte")]
        public static void ApplyToOpenScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Module01] Sal de Play antes de colocar los EPP.");
                return;
            }

            Scene work = SceneManager.GetActiveScene();
            if (work.path == ArtScene)
            {
                Debug.LogError("[Module01] La escena abierta ES la de arte. Abre la escena de trabajo antes.");
                return;
            }

            // Sus objetos se traen moviéndolos: con la escena de arte abierta en el editor se
            // quedaría sin sus EPP a la vista de quien la está mirando.
            if (SceneManager.GetSceneByPath(ArtScene).isLoaded)
            {
                Debug.LogError($"[Module01] Cierra {ArtScene} antes: sus EPP se traen moviéndolos.");
                return;
            }

            Scene art = EditorSceneManager.OpenScene(ArtScene, OpenSceneMode.Additive);
            int matched, removed, copied;
            try
            {
                matched = MatchWorld(work, art);
                List<GameObject> incoming = Collect(art);
                removed = RemoveOldEpp(work, incoming);
                copied = Bring(incoming, work);
            }
            finally
            {
                EditorSceneManager.CloseScene(art, true);
            }

            SceneManager.SetActiveScene(work);
            ApplyMoves(work);
            Module01SceneEntities.Apply();
            RetireAndClean(work);
            Module01EppLayout.LayoutOpenScene();
            SceneHierarchyLayout.Apply(work);
            EditorSceneManager.MarkSceneDirty(work);

            Debug.Log($"[Module01] EPP como en {ArtScene}: {copied} objetos traídos, {removed} copias viejas " +
                      $"fuera y {matched} piezas del mobiliario devueltas a su sitio.");
        }

        /// <summary>
        /// Lo único que <b>no</b> queda como lo dejó el artista, por decisión de la PO
        /// (2026-10-28): (ruta desde el primer nivel, mueble sobre el que va o <c>null</c>,
        /// punto). Con mueble, el punto es dónde buscar el hueco libre más cercano de su tapa; sin
        /// él, es dónde va el centro de la base del objeto.
        /// <para>
        /// El poncho colgaba delante de la mesa del fondo y tapaba la luz delantera, el teléfono,
        /// las gafas y los audífonos: va al rincón entre esa mesa y el mostrador del fondo. El casco
        /// sin correa (H01) estaba en el mostrador de la derecha, fuera de toda parada: va al
        /// mostrador de la ropa. El soporte del celular quedaba detrás de los AirPods y del candado
        /// mirando desde su parada (48 % alcanzable) y en esa mesa no queda hueco: va a la mesa de
        /// la entrada, que es su parada desde entonces (PO: «se puede distribuir en la otra mesa»).
        /// </para>
        /// </summary>
        public static readonly (string Path, string Surface, Vector3 Point)[] Moves =
        {
            ("epps/poncho h", null, new Vector3(3.55f, 0.035f, -3.64f)),
            ("enviroment_decoration/df_g_helmet_01", "cabinetmid 1", new Vector3(0.75f, 1.08f, -4.45f)),
            ("epps/soporte celular", "mesa", new Vector3(4.38f, 1.02f, 4.82f)),
            // La mesa del centro es la única parada con decorado encima, y era la más apretada del
            // garaje: 16 % de tapa libre (medido el 2026-10-29). La culpa es de tres reglas del
            // artista, planas sobre la tapa, que entre las tres cubren 1,38 m² de los 1,27 que
            // mide. Las otras 26 herramientas de su grupo **cuelgan del panel** y no estorban:
            // moverlas también las dejaba flotando delante del logo de la pared del fondo.
            // Van al entrepaño bajo del banco de herramientas, que no es tapa de parada.
            ("enviroment_decoration/tools herramientas/regla", null, new Vector3(-4.40f, 0.30f, 1.40f)),
            ("enviroment_decoration/tools herramientas/medidor1", null, new Vector3(-4.42f, 0.30f, 2.30f)),
            ("enviroment_decoration/tools herramientas/regla_l", null, new Vector3(-4.45f, 0.30f, 3.00f)),
        };

        /// <summary>
        /// Modelos del artista que <b>se apagan</b> en la escena de trabajo: están en el garaje
        /// pero ya no son nada.
        /// <para>
        /// Los cuatro pantalones de pie dejaron de ser elementos el 2026-10-29: cada uno es ahora
        /// el modelo de pedestal de su doblado (<c>Entry.InspectModel</c>). Si se dejan puestos,
        /// se quedan de decorado ocupando el suelo del garaje, y el cliente no quiere nada en el
        /// suelo salvo los vehículos.
        /// </para>
        /// </summary>
        public static readonly string[] Retired =
        {
            Module01Library.Essentials + "Slim_fit_pants.fbx",
            Module01Library.Models + "jean ajustado.fbx",
            Module01Library.Models + "pant low.fbx",
            Module01Library.Models + "pantalon ancho.fbx",
        };

        /// <summary>
        /// Apaga lo retirado y borra lo que quedó instanciado sin ser nada: el prefab de las
        /// gafas dañadas aparecía duplicado bajo <c>Module01_Spawned</c>, sin entidad, tirado en
        /// el suelo del centro del garaje (PO, 2026-10-29). Sin entidad no se pulsa ni cuenta:
        /// es basura que solo se ve.
        /// </summary>
        private static void RetireAndClean(Scene work)
        {
            var retired = new HashSet<string>(Retired);
            int apagados = 0, borrados = 0;
            foreach (GameObject go in work.GetRootGameObjects()
                         .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                         .Select(t => t.gameObject).ToList())
            {
                if (go == null)
                    continue;
                if (retired.Contains(SourceOf(go)) && go.activeSelf)
                {
                    Undo.RecordObject(go, "Retirar del garaje");
                    go.SetActive(false);
                    apagados++;
                }
            }

            // A cualquier profundidad: `SceneHierarchyLayout` mete `Module01_Spawned` dentro de
            // una carpeta de la jerarquía, así que buscarlo solo entre los objetos raíz no lo
            // encontraba y el huérfano de las gafas seguía tirado en el suelo (PO, 2026-10-29).
            GameObject spawned = work.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == SpawnedRoot)?.gameObject;
            if (spawned != null)
                foreach (Transform child in spawned.transform.Cast<Transform>().ToList())
                    if (child.GetComponentInChildren<WorldObjectTaskEntity>(true) == null)
                    {
                        Debug.Log($"[Module01] Fuera '{child.name}': instanciado y sin entidad.");
                        Undo.DestroyObjectImmediate(child.gameObject);
                        borrados++;
                    }

            if (apagados > 0 || borrados > 0)
                Debug.Log($"[Module01] {apagados} modelos retirados del garaje y {borrados} huérfanos borrados.");
        }

        private static void ApplyMoves(Scene work)
        {
            foreach ((string path, string surface, Vector3 point) in Moves)
            {
                Transform t = FindPath(work, path);
                if (t == null)
                {
                    Debug.LogWarning($"[Module01] No encuentro '{path}' para moverlo; el arte cambió.");
                    continue;
                }
                Bounds box = Box(t.gameObject);
                Vector3 bottom = point;
                if (surface != null)
                {
                    Transform top = SceneFolder.TopLevelObjects(work)
                        .SelectMany(g => g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(x => x.name == surface);
                    if (top == null || !Module01Stations.All.SelectMany(s => s.Surfaces).Any(s => s.Name == surface))
                    {
                        Debug.LogWarning($"[Module01] '{surface}' no es un mueble de parada; '{path}' se queda donde está.");
                        continue;
                    }
                    Surface declared = Module01Stations.All.SelectMany(s => s.Surfaces).First(s => s.Name == surface);
                    List<Bounds> occupied = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                        .Where(r => r.enabled && r.bounds.size.magnitude <= 8f && !r.transform.IsChildOf(top)
                                    && !r.transform.IsChildOf(t))
                        .Select(r => r.bounds).ToList();
                    List<Vector3> spot = StationLayout.FreeSpots(declared.Clip(Box(top.gameObject)), declared.WorktopHeight,
                                                                 box.size, 1, occupied, point);
                    if (spot.Count == 0)
                    {
                        Debug.LogWarning($"[Module01] No hay hueco en '{surface}' para '{path}'; se queda donde está.");
                        continue;
                    }
                    bottom = new Vector3(spot[0].x, declared.WorktopHeight, spot[0].z);
                }
                Undo.RecordObject(t, "Mover por decisión de la PO");
                t.position += bottom - new Vector3(box.center.x, box.min.y, box.center.z);
                Debug.Log($"[Module01] '{path}' movido a {bottom:F2} (PO).");
            }
        }

        /// <summary>El objeto en esa ruta, contando desde el primer nivel (las carpetas no cuentan).</summary>
        public static Transform FindPath(Scene scene, string path)
        {
            string[] parts = path.Split('/');
            foreach (GameObject top in SceneFolder.TopLevelObjects(scene))
                if (top.name == parts[0])
                    return parts.Length == 1 ? top.transform : top.transform.Find(string.Join("/", parts.Skip(1)));
            return null;
        }

        private static Bounds Box(GameObject go)
        {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>Deja los muebles de <see cref="WorldWithEpp"/> como en el arte: sitio y encendido.</summary>
        private static int MatchWorld(Scene work, Scene art)
        {
            int changed = 0;
            foreach (string name in WorldWithEpp)
            {
                GameObject mine = SceneFolder.TopLevelObjects(work).FirstOrDefault(g => g.name == name);
                GameObject theirs = art.GetRootGameObjects().FirstOrDefault(g => g.name == name);
                if (mine == null || theirs == null)
                {
                    Debug.LogWarning($"[Module01] '{name}' no está en las dos escenas; no se iguala.");
                    continue;
                }

                foreach (Transform source in theirs.GetComponentsInChildren<Transform>(true))
                {
                    if (source == theirs.transform)
                        continue;
                    Transform target = mine.transform.Find(PathFrom(theirs.transform, source));
                    if (target == null)
                        continue;
                    if (target.localPosition == source.localPosition && target.localRotation == source.localRotation
                        && target.localScale == source.localScale && target.gameObject.activeSelf == source.gameObject.activeSelf)
                        continue;

                    Undo.RecordObject(target, "Mobiliario como en el arte");
                    Undo.RecordObject(target.gameObject, "Mobiliario como en el arte");
                    target.localPosition = source.localPosition;
                    target.localRotation = source.localRotation;
                    target.localScale = source.localScale;
                    target.gameObject.SetActive(source.gameObject.activeSelf);
                    changed++;
                }
            }
            return changed;
        }

        /// <summary>
        /// Quita de la escena de trabajo los EPP de la copia anterior: lo que, en primer nivel,
        /// está hecho solo de modelos de EPP. No se busca por nombre: así sale también el arte de
        /// entregas viejas que el artista ya no trae.
        /// </summary>
        private static int RemoveOldEpp(Scene work, List<GameObject> incoming)
        {
            HashSet<string> models = DeclaredModels();
            foreach (GameObject g in incoming)
                models.Add(SourceOf(g));

            int removed = 0;
            foreach (GameObject go in SceneFolder.TopLevelObjects(work).ToList())
            {
                // El contenedor de la copia anterior se va siempre, traiga lo que traiga: si el
                // artista retira un EPP —la cartuchera, el 2026-10-28— su modelo ya no cuenta como
                // EPP, el contenedor dejaba de estar «hecho solo de EPP» y se quedaba duplicado.
                bool previousCopy = go.name == EppRoot && !PrefabUtility.IsAnyPrefabInstanceRoot(go);
                if (go.name == SpawnedRoot || (!previousCopy && !MadeOnlyOf(go, models)))
                    continue;
                Undo.DestroyObjectImmediate(go);
                removed++;
            }
            return removed;
        }

        /// <summary>Trae los EPP del arte bajo <c>epps</c>, en el origen: conservan su pose de mundo.</summary>
        private static int Bring(List<GameObject> incoming, Scene work)
        {
            GameObject container = new GameObject(EppRoot);
            SceneManager.MoveGameObjectToScene(container, work);

            foreach (GameObject go in incoming)
            {
                go.transform.SetParent(null, true);
                SceneManager.MoveGameObjectToScene(go, work);
                go.transform.SetParent(container.transform, true);
                LightWithProbes(go);
            }
            return incoming.Count;
        }

        /// <summary>
        /// Los EPP se iluminan con las sondas, no con el horneado de v5.
        /// <para>
        /// Su horneado es el de la escena de arte y no viaja con ellos: un objeto que contribuye
        /// a la iluminación global y llega sin mapa de luz se ve negro hasta que alguien vuelva a
        /// hornear v5. Con las sondas se ven igual que los EPP que se instanciaban hasta ahora.
        /// </para>
        /// </summary>
        private static void LightWithProbes(GameObject root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(t.gameObject);
                if ((flags & StaticEditorFlags.ContributeGI) != 0)
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags & ~StaticEditorFlags.ContributeGI);
            }
            foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
                r.receiveGI = ReceiveGI.LightProbes;
        }

        /// <summary>Los FBX de los elementos de la biblioteca.</summary>
        private static HashSet<string> DeclaredModels()
        {
            var models = new HashSet<string>();
            foreach (Module01Library.Entry e in Module01Library.All)
            {
                if (!e.NeedsMarker)
                    models.Add(e.Model);
                models.UnionWith(e.ExtraModels);
            }
            return models;
        }

        private static bool MadeOnlyOf(GameObject go, HashSet<string> models)
        {
            int pieces = 0;
            foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!IsEppMesh(mf.sharedMesh, models)) return false;
                pieces++;
            }
            foreach (SkinnedMeshRenderer smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!IsEppMesh(smr.sharedMesh, models)) return false;
                pieces++;
            }
            return pieces > 0;
        }

        private static bool IsEppMesh(Mesh mesh, HashSet<string> models)
        {
            string path = AssetDatabase.GetAssetPath(mesh);
            return models.Contains(path) || path.StartsWith(Module01Library.Essentials);
        }

        public static string SourceOf(GameObject go) =>
            AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(go));

        private static string PathFrom(Transform root, Transform t)
        {
            string path = t.name;
            for (Transform p = t.parent; p != null && p != root; p = p.parent)
                path = p.name + "/" + path;
            return path;
        }
    }
}
