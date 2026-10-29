using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RideSafe.TaskSequence;
using EntityId = RideSafe.TaskSequence.EntityId;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Pone las entidades de tarea sobre los objetos del garaje y crea las que falten.
    /// <para>
    /// Separado de <c>Module01SceneBuilder</c> y aplicable a la escena abierta: aquel
    /// <b>regenera</b> la escena desde la de lógica y se lleva por delante la presentación.
    /// Esto se puede repetir sin perder nada, que es lo que hace falta al cambiar un modelo,
    /// añadir un EPP o recibir una versión nueva del garaje.
    /// </para>
    /// </summary>
    public static class Module01SceneEntities
    {
        private const string SpawnedRoot = "Module01_Spawned";

        [MenuItem("RideSafe/Módulo 1/Etiquetar entidades en la escena abierta")]
        public static void ApplyToOpenScene()
        {
            Apply();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        /// <summary>Idempotente: reutiliza las entidades que ya estén puestas.</summary>
        public static void Apply()
        {
            TagArt();
            ReplaceOutdatedSpawned();
            FillMissingEntities();
            RemoveOrphanSpawned();
            RemoveStaleEntitiesOnArt();
        }

        /// <summary>
        /// Lleva el objeto al ancho que declara la biblioteca, si lo declara.
        /// <para>
        /// Los modelos de la carpeta de esenciales llegan a escalas que no son la real —el casco
        /// mide 1,02 m de ancho y el kit de pinchazos 2,06— y así no caben en ninguna tapa del
        /// garaje. Se escala por <b>lo que mide de verdad</b>, no por un factor fijo: cuando 3D
        /// reexporte a escala real, <c>objetivo / medido</c> dará 1 y esto dejará de hacer nada
        /// sin que haya que acordarse de quitarlo.
        /// </para>
        /// <para>
        /// Escala uniforme a propósito: corregir cada eje por separado deformaría el objeto, y
        /// lo que está mal es la escala del export, no las proporciones.
        /// </para>
        /// </summary>
        private static void ScaleToTargetWidth(GameObject go, string itemId)
        {
            float objetivo = 0f;
            foreach (Module01Library.Entry e in Module01Library.All)
                if (e.ItemId == itemId) { objetivo = e.TargetWidth; break; }

            if (objetivo <= 0f)
                return;

            Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0)
                return;

            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs) b.Encapsulate(r.bounds);

            float medido = Mathf.Max(b.size.x, b.size.z);
            if (medido <= 0.0001f)
                return;

            float factor = objetivo / medido;
            if (Mathf.Abs(factor - 1f) < 0.02f)
                return;   // ya llega bien: no se toca la escala por un 2 %

            go.transform.localScale *= factor;
            Debug.Log($"[Module01] {itemId}: medía {medido:F2} m de ancho y la biblioteca pide "
                      + $"{objetivo:F2}; escalado x{factor:F3}. Cuando 3D lo reexporte a escala "
                      + "real este factor será 1.");
        }

        /// <summary>
        /// Quita las entidades que quedaron sobre objetos de arte con un id que el catálogo ya
        /// no conoce —por ejemplo <c>module01.head.helmet_ok</c> cuando la zona <c>head</c> pasó
        /// a ser <c>protection</c>—.
        /// <para>
        /// <see cref="RemoveOrphanSpawned"/> no las ve: solo mira bajo <c>Module01_Spawned</c>, y
        /// estas viven en el mueble del arte. Sin esto quedan esferas y modelos que responden al
        /// clic y no pertenecen a ninguna zona, así que el recorrido nunca las cierra.
        /// </para>
        /// </summary>
        private static void RemoveStaleEntitiesOnArt()
        {
            HashSet<string> wanted = new HashSet<string>();
            foreach (ZoneSO zone in AllZones())
                foreach (SafetyItemSO item in zone.Items)
                    if (item != null)
                        wanted.Add($"module01.{zone.ZoneId}.{item.ItemId}");

            GameObject spawned = Find(SpawnedRoot);
            int quitadas = 0;

            foreach (WorldObjectTaskEntity entity in
                     Object.FindObjectsByType<WorldObjectTaskEntity>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string id = entity.Id.Value;
                if (!id.StartsWith("module01.") || wanted.Contains(id))
                    continue;
                // Lo de dentro de Module01_Spawned ya lo borra RemoveOrphanSpawned, objeto
                // incluido. Aquí solo se quita el componente: el modelo es del arte.
                if (spawned != null && entity.transform.IsChildOf(spawned.transform))
                    continue;

                Debug.Log($"[Module01] Quitada la entidad obsoleta '{id}' de '{entity.name}'.");
                Undo.DestroyObjectImmediate(entity);
                quitadas++;
            }

            if (quitadas > 0)
                Debug.Log($"[Module01] Entidades obsoletas quitadas del arte: {quitadas}.");
        }

        /// <summary>
        /// Pone la entidad de cada elemento sobre el objeto que el artista colocó con su modelo
        /// (ver <see cref="Module01ArtEpp"/>), y quita la copia instanciada si la había.
        /// <para>
        /// El objeto se reconoce por su <b>FBX</b>, no por su nombre: el nombre lo pone el artista y
        /// cambia de una entrega a otra (<c>"jean_ajustado_doblado "</c> llegó con un espacio al
        /// final), el modelo es el que declara la hoja.
        /// </para>
        /// <para>
        /// Si el artista puso varias piezas del mismo modelo —los cuatro zapatos, los dos
        /// guantes, las dos chanclas— son <b>un</b> elemento: las demás cuelgan de la primera, sin
        /// moverse, y el clic, el contorno y el reporte las tratan como una sola cosa.
        /// </para>
        /// </summary>
        private static void TagArt()
        {
            GameObject spawned = Find(SpawnedRoot);
            List<GameObject> art = new List<GameObject>();
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (PrefabUtility.IsAnyPrefabInstanceRoot(go) && (spawned == null || !go.transform.IsChildOf(spawned.transform)))
                    art.Add(go);

            foreach (Module01Library.Entry e in Module01Library.All)
            {
                if (e.NeedsMarker)
                    continue;
                List<GameObject> pieces = art.FindAll(g => Module01ArtEpp.SourceOf(g) == e.Model);
                if (pieces.Count == 0)
                    pieces = MeshOwners(e.Model, spawned);
                if (pieces.Count == 0)
                    continue;
                pieces.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
                // Lo de otro FBX que es parte del mismo elemento —el portabotella del termo— va
                // detrás: la entidad la lleva siempre una pieza del modelo principal.
                foreach (string extra in e.ExtraModels)
                    pieces.AddRange(art.FindAll(g => Module01ArtEpp.SourceOf(g) == extra));

                GameObject host = pieces[0];
                for (int i = 1; i < pieces.Count; i++)
                {
                    foreach (WorldObjectTaskEntity stale in pieces[i].GetComponentsInChildren<WorldObjectTaskEntity>(true))
                        Undo.DestroyObjectImmediate(stale);
                    if (pieces[i].transform.parent != host.transform)
                        Undo.SetTransformParent(pieces[i].transform, host.transform, "Piezas de un EPP");
                }

                string id = $"module01.{Module01Library.StopOf(e.ItemId)}.{e.ItemId}";
                RemoveSpawnedCopy(spawned, id);
                Tag(host, id, e.ItemId);
            }
        }

        /// <summary>
        /// Los objetos que llevan una malla de <paramref name="model"/> sin ser una instancia
        /// suelta de ese FBX: las botas rojas vienen <b>dentro</b> del prefab de decoración del
        /// garaje. Solo los de más arriba, para no contar dos veces un objeto y su hijo.
        /// </summary>
        private static List<GameObject> MeshOwners(string model, GameObject spawned)
        {
            var owners = new List<GameObject>();
            foreach (MeshFilter mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (mf.sharedMesh != null && AssetDatabase.GetAssetPath(mf.sharedMesh) == model
                    && (spawned == null || !mf.transform.IsChildOf(spawned.transform)))
                    owners.Add(mf.gameObject);
            owners.RemoveAll(g => owners.Exists(o => o != g && g.transform.IsChildOf(o.transform)));
            return owners;
        }

        private static void RemoveSpawnedCopy(GameObject spawned, string id)
        {
            if (spawned == null)
                return;
            foreach (WorldObjectTaskEntity copy in spawned.GetComponentsInChildren<WorldObjectTaskEntity>(true))
                if (copy.Id.Value == id)
                {
                    Debug.Log($"[Module01] '{id}' se pulsa ya sobre el objeto del artista; fuera la copia instanciada.");
                    Undo.DestroyObjectImmediate(copy.gameObject);
                }
        }

        private static void Tag(GameObject go, string id, string itemId)
        {
            // El collider se reajusta SIEMPRE, no solo cuando falta: al recibir un garaje
            // nuevo el objeto cambia de escala y de sitio, y un collider heredado del
            // garaje anterior deja el clic desplazado respecto a lo que se ve.
            BoxCollider box = Module01EditorComponents.Ensure<BoxCollider>(go);
            FitCollider(go, box);

            // Los colliders de los hijos se apagan: con dos volúmenes solapados gana el
            // que el raycast toque primero, que no siempre es el que lleva la entidad. Los que
            // traiga el propio objeto se quitan: apagados seguirían siendo «su» collider para
            // GetComponent, con una caja vacía (le pasaba al casco retro del decorado).
            foreach (Collider other in go.GetComponentsInChildren<Collider>(true))
            {
                if (other == box)
                    continue;
                if (other.gameObject == go)
                    Undo.DestroyObjectImmediate(other);
                else
                    other.enabled = false;
            }

            // Lo que se inspecciona viaja al pedestal y se delinea: agrupado estático no puede.
            Module01EditorComponents.ClearBatchingStatic(go);

            bool wasActive = go.activeSelf;
            go.SetActive(false);
            WorldObjectTaskEntity entity = Module01EditorComponents.Ensure<WorldObjectTaskEntity>(go);
            entity.Configure(new EntityId(id), itemId);
            go.SetActive(wasActive);
        }

        /// <summary>
        /// Borra lo que este script instanció para un elemento cuyo modelo cambió después —el
        /// signo «!» de un elemento que ya tiene FBX, o un FBX que el catálogo cambió—, para que
        /// <see cref="FillMissingEntities"/> lo vuelva a crear con el modelo del catálogo.
        /// <para>
        /// Sin esto la entidad ya existe, <see cref="FillMissingEntities"/> se la salta y el
        /// elemento se queda con el signo aunque 3D haya entregado su modelo. Pasó con la mochila
        /// el 2026-10-27.
        /// </para>
        /// </summary>
        private static void ReplaceOutdatedSpawned()
        {
            GameObject spawned = Find(SpawnedRoot);
            if (spawned == null)
                return;

            Dictionary<string, SafetyItemSO> items = new Dictionary<string, SafetyItemSO>();
            foreach (ZoneSO zone in AllZones())
                foreach (SafetyItemSO item in zone.Items)
                    if (item != null)
                        items[item.ItemId] = item;

            int replaced = 0;
            foreach (WorldObjectTaskEntity entity in
                     spawned.GetComponentsInChildren<WorldObjectTaskEntity>(true))
            {
                // Uno que ya no está en ninguna zona lo borra RemoveOrphanSpawned.
                if (!items.TryGetValue(entity.ItemId, out SafetyItemSO item))
                    continue;

                Object shown = PrefabUtility.GetCorrespondingObjectFromSource(entity.gameObject);
                if (shown == item.DisplayPrefab)
                    continue;

                Debug.Log($"[Module01] '{entity.Id.Value}' cambió de modelo; se vuelve a crear.");
                Undo.DestroyObjectImmediate(entity.gameObject);
                replaced++;
            }

            if (replaced > 0)
                Debug.Log($"[Module01] EPP vueltos a crear con su modelo nuevo: {replaced}.");
        }

        /// <summary>
        /// Toda zona debe tener una entidad por elemento, o nunca se cierra y el módulo
        /// se queda esperando. Lo que el equipo de arte no colocó se crea aquí: el
        /// modelo del catálogo si lo tiene, y si no una primitiva gris.
        /// </summary>
        private static void FillMissingEntities()
        {
            Module01CatalogSO catalog = AssetDatabase.LoadAssetAtPath<Module01CatalogSO>(
                "Assets/_Project/Module01/Data/SO_Module01Catalog.asset");

            HashSet<string> present = new HashSet<string>();
            foreach (WorldObjectTaskEntity e in Object.FindObjectsByType<WorldObjectTaskEntity>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                present.Add(e.Id.Value);

            GameObject spare = Find(SpawnedRoot) ?? new GameObject(SpawnedRoot);
            GameObject bike = Find("ebike");
            int created = 0;

            List<(ZoneSO Zone, bool OnVehicle)> all = new List<(ZoneSO, bool)>();
            if (catalog != null)
                foreach (ZoneSO z in catalog.Zones) if (z != null) all.Add((z, false));
            // Todos los vehiculos, no solo la bici: la plataforma es zona solo del scooter.
            foreach (ZoneSO z in VehicleProfiles.AllInspectionZones())
                all.Add((z, true));

            foreach ((ZoneSO zone, bool onVehicle) in all)
            {
                for (int i = 0; i < zone.Items.Count; i++)
                {
                    SafetyItemSO item = zone.Items[i];
                    if (item == null)
                        continue;

                    string id = $"module01.{zone.ZoneId}.{item.ItemId}";
                    if (present.Contains(id))
                        continue;

                    GameObject go;
                    if (item.DisplayPrefab != null)
                    {
                        go = (GameObject)PrefabUtility.InstantiatePrefab(item.DisplayPrefab);
                        go.name = item.ItemId;
                        ScaleToTargetWidth(go, item.ItemId);
                    }
                    else
                    {
                        // Esfera SOLO como volumen de clic: el renderer nace apagado. Lo que
                        // se ve es el signo o el contorno de la pieza, nunca una bola blanca.
                        go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        go.name = item.ItemId;
                        go.transform.localScale = Vector3.one * 0.15f;
                        foreach (MeshRenderer mr in go.GetComponentsInChildren<MeshRenderer>(true))
                            mr.enabled = false;
                    }

                    if (onVehicle && bike != null)
                    {
                        // Marcador sobre el vehiculo real, repartido a lo largo de el.
                        go.transform.SetParent(bike.transform, false);
                        go.transform.localPosition = new Vector3(0f, 0.4f + i * 0.25f, -0.4f + i * 0.4f);
                    }
                    else
                    {
                        go.transform.SetParent(spare.transform, false);
                        go.transform.position = new Vector3(i * 0.6f, 1.0f, 0f);
                    }

                    if (go.GetComponent<Collider>() == null)
                    {
                        BoxCollider box = go.AddComponent<BoxCollider>();
                        FitCollider(go, box);
                    }

                    bool wasActive = go.activeSelf;
                    go.SetActive(false);
                    WorldObjectTaskEntity entity =
                        Module01EditorComponents.Ensure<WorldObjectTaskEntity>(go);
                    entity.Configure(new EntityId(id), item.ItemId);
                    go.SetActive(wasActive);

                    // Sin modelo, el elemento se señala con el signo «!», igual que los puntos
                    // del vehículo que no tienen pieza propia. Sin esto quedaba una esfera
                    // invisible: clicable pero imposible de encontrar.
                    if (item.DisplayPrefab == null)
                        Module01VehicleStaging.BuildMarker(entity);

                    present.Add(id);
                    created++;
                }
            }

            Debug.Log($"[Module01] Entidades creadas para completar las zonas: {created}.");
        }

        /// <summary>
        /// Lo más grande, en metros de mundo, que sigue pulsándose por su caja. La caja de un
        /// objeto pequeño es casi el objeto, y con su engorde (<see cref="MinClickThickness"/>)
        /// las gafas, el cable o un cordón se atinan; en uno grande la caja abarca aire.
        /// </summary>
        private const float SmallItem = .16f;

        /// <summary>
        /// Cada EPP se pulsa por su <b>forma</b> y no por su caja, salvo los pequeños que no
        /// tocan a nadie (PO, 2026-10-28).
        /// <para>
        /// La caja de un objeto abierto abarca aire: la del candado de cable envolvía el candado
        /// suelto que el artista dejó dentro de la espiral, que no había forma de pulsar, y la
        /// chaqueta, los pantalones de pie o las rodilleras se comían el clic de lo de detrás.
        /// Medido desde cada parada: doce elementos con menos del 70 % alcanzable.
        /// </para>
        /// <para>
        /// La forma son <c>MeshCollider</c> no convexos en cada pieza, con la malla del FBX: sin
        /// mallas nuevas. Esas mallas no son legibles, y PhysX solo las cocina en el build si el
        /// collider tiene escala uniforme y opciones por defecto; una pieza con escala no uniforme
        /// o negativa lleva una caja ajustada solo a ella. El objeto que lleva la entidad conserva
        /// siempre un collider propio (lo exige la entidad y lo leen la ayuda y los tests): si su
        /// malla va en hijos, su caja se ajusta a la pieza mayor.
        /// </para>
        /// <para>
        /// Se recalcula desde cero en cada pasada —primero todos con caja— porque los vecinos de
        /// un elemento cambian cuando el artista o la colocación lo mueven.
        /// </para>
        /// </summary>
        public static void FitClickShapes()
        {
            var entities = new List<WorldObjectTaskEntity>();
            foreach (WorldObjectTaskEntity e in Object.FindObjectsByType<WorldObjectTaskEntity>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (e.Id.Value.StartsWith("module01.") && !e.ItemId.StartsWith("point_")
                    && e.GetComponentInChildren<MeshFilter>(true) != null)
                    entities.Add(e);

            var boxes = new Dictionary<WorldObjectTaskEntity, BoxCollider>();
            foreach (WorldObjectTaskEntity e in entities)
            {
                BoxCollider box = Module01EditorComponents.Ensure<BoxCollider>(e.gameObject);
                box.enabled = true;
                FitCollider(e.gameObject, box);
                foreach (Collider other in e.GetComponentsInChildren<Collider>(true))
                    if (other != box)
                        other.enabled = false;
                boxes[e] = box;
            }
            Physics.SyncTransforms();

            // Se decide con todas las cajas puestas y luego se cambia: quitar la caja de uno a
            // mitad de la pasada dejaría a sus vecinos comparándose con una caja destruida.
            var needShape = new List<WorldObjectTaskEntity>();
            foreach (WorldObjectTaskEntity e in entities)
            {
                Vector3 size = boxes[e].bounds.size;
                bool small = Mathf.Max(size.x, size.y, size.z) <= SmallItem;
                if (!small || entities.Exists(o => o != e && Overlap(boxes[e], boxes[o])))
                    needShape.Add(e);
            }

            int meshes = 0, pieceBoxes = 0;
            foreach (WorldObjectTaskEntity e in needShape)
            {
                BoxCollider box = boxes[e];
                List<MeshFilter> pieces = new List<MeshFilter>();
                foreach (MeshFilter mf in e.GetComponentsInChildren<MeshFilter>(false))
                    if (mf.sharedMesh != null && mf.TryGetComponent(out Renderer r) && r.enabled)
                        pieces.Add(mf);
                if (pieces.Count == 0)
                    continue;

                // La pieza que lleva el collider de la entidad: la del propio objeto, o si su
                // malla va en hijos —el candado, el kit— la mayor, con la caja del objeto
                // ajustada solo a ella.
                MeshFilter hostPiece = pieces.Find(mf => mf.gameObject == e.gameObject)
                    ?? pieces.OrderByDescending(mf => Volume(mf.GetComponent<Renderer>().bounds)).First();

                foreach (MeshFilter mf in pieces)
                {
                    if (mf == hostPiece)
                        continue;
                    if (UniformScale(mf.transform)) { Shape(mf); meshes++; }
                    else { PieceBox(mf); pieceBoxes++; }
                }

                if (hostPiece.gameObject == e.gameObject && UniformScale(hostPiece.transform))
                {
                    Shape(hostPiece);
                    meshes++;
                    // Después de la forma: la entidad exige un collider y no deja quitar el
                    // único. Sin la caja, el del objeto es el que devuelve GetComponent<Collider>().
                    Undo.DestroyObjectImmediate(box);
                }
                else
                {
                    FitCollider(e.gameObject, box, new[] { hostPiece.GetComponent<Renderer>() });
                    pieceBoxes++;
                }
            }

            Debug.Log($"[Module01] Clic por la forma: {needShape.Count} EPP ({meshes} piezas por su malla, "
                      + $"{pieceBoxes} por su propia caja); {entities.Count - needShape.Count} pequeños y sueltos con su caja.");
        }

        /// <summary>La malla misma, para las piezas con escala uniforme (ver <see cref="FitClickShapes"/>).</summary>
        private static void Shape(MeshFilter piece)
        {
            MeshCollider shape = Module01EditorComponents.Ensure<MeshCollider>(piece.gameObject);
            shape.sharedMesh = piece.sharedMesh;
            shape.convex = false;
            // Las opciones por defecto: con otras, PhysX ya no cocina la malla no legible en el build.
            shape.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation
                                   | MeshColliderCookingOptions.EnableMeshCleaning
                                   | MeshColliderCookingOptions.WeldColocatedVertices
                                   | MeshColliderCookingOptions.UseFastMidphase;
            shape.enabled = true;
        }

        /// <summary>
        /// Una caja ajustada a la pieza sola, girada con ella: con escala no uniforme la malla no
        /// se cocina en el build, pero la caja de un zapato sigue siendo casi el zapato.
        /// </summary>
        private static void PieceBox(MeshFilter piece)
        {
            BoxCollider box = Module01EditorComponents.Ensure<BoxCollider>(PositiveHolder(piece.transform));
            box.enabled = true;
            FitCollider(box.gameObject, box, new[] { piece.GetComponent<Renderer>() });
        }

        private const string MirrorHolder = "clic";

        /// <summary>
        /// Dónde va la caja de una pieza: en ella, o si el artista la espejó (el zapato izquierdo,
        /// escala −1) en un hijo que deshace el espejo. Una caja con escala negativa funciona, pero
        /// PhysX avisa en la consola cada vez que se carga la escena.
        /// </summary>
        private static GameObject PositiveHolder(Transform piece)
        {
            Vector3 s = piece.lossyScale;
            Transform holder = piece.Find(MirrorHolder);
            if (s.x > 0f && s.y > 0f && s.z > 0f)
            {
                if (holder != null)
                    Undo.DestroyObjectImmediate(holder.gameObject);
                return piece.gameObject;
            }
            if (holder == null)
            {
                holder = new GameObject(MirrorHolder).transform;
                Undo.RegisterCreatedObjectUndo(holder.gameObject, "Caja de la pieza espejada");
                holder.SetParent(piece, false);
            }
            holder.localPosition = Vector3.zero;
            holder.localRotation = Quaternion.identity;
            holder.localScale = new Vector3(Mathf.Sign(s.x), Mathf.Sign(s.y), Mathf.Sign(s.z));
            // La caja vieja de la propia pieza, la de antes de esto, fuera.
            if (piece.TryGetComponent(out BoxCollider old))
                Undo.DestroyObjectImmediate(old);
            return holder.gameObject;
        }

        private static float Volume(Bounds b) => b.size.x * b.size.y * b.size.z;

        private static bool Overlap(BoxCollider a, BoxCollider b) =>
            a.bounds.Intersects(b.bounds)
            && Physics.ComputePenetration(a, a.transform.position, a.transform.rotation,
                                          b, b.transform.position, b.transform.rotation, out _, out float depth)
            && depth > .005f;

        private static bool UniformScale(Transform t)
        {
            Vector3 s = t.lossyScale;
            if (s.x <= 0f || s.y <= 0f || s.z <= 0f)
                return false;
            return Mathf.Abs(s.x - s.y) <= s.x * .01f && Mathf.Abs(s.y - s.z) <= s.y * .01f;
        }

        /// <summary>
        /// Grosor mínimo de un lado del collider, en metros de mundo. La guaya y el teléfono
        /// son láminas de 2 cm: de canto no se ven y el clic falla por un par de milímetros,
        /// que es la mitad del «se hace clic en la nada». Un objeto plano sigue viéndose
        /// plano; lo que se engorda es solo su volumen de acierto.
        /// </summary>
        private const float MinClickThickness = .06f;

        /// <summary>
        /// Ajusta el collider al volumen visible, que los FBX no traen uno.
        /// <para>
        /// Las ocho esquinas de cada renderer se pasan a espacio local <b>antes</b> de
        /// encerrarlas. Tomar <c>bounds.size</c> del mundo y metérselo a <c>box.size</c>,
        /// que es local, deja el collider descolocado en cuanto el objeto tiene escala —y
        /// los objetos que coloca el equipo de arte la tienen casi siempre.
        /// </para>
        /// </summary>
        private static void FitCollider(GameObject go, BoxCollider box) =>
            FitCollider(go, box, go.GetComponentsInChildren<Renderer>(true));

        private static void FitCollider(GameObject go, BoxCollider box, Renderer[] renderers)
        {
            if (renderers.Length == 0)
                return;

            Bounds local = new Bounds();
            bool first = true;
            foreach (Renderer r in renderers)
            {
                // La caja de la propia malla, en su espacio, y no la del mundo: la del mundo de
                // una pieza girada es mucho mayor que la pieza, y el collider que sale de ella
                // tapaba lo de detrás. Medido: las rodilleras, giradas 285°, se comían el clic de
                // las coderas (PO, 2026-10-28).
                Bounds b = MeshBounds(r);
                for (int k = 0; k < 8; k++)
                {
                    Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3(
                        (k & 1) == 0 ? -1 : 1,
                        (k & 2) == 0 ? -1 : 1,
                        (k & 4) == 0 ? -1 : 1));
                    Vector3 p = go.transform.InverseTransformPoint(r.transform.TransformPoint(corner));
                    if (first) { local = new Bounds(p, Vector3.zero); first = false; }
                    else local.Encapsulate(p);
                }
            }
            if (first)
                return;

            box.center = local.center;

            // El mínimo se expresa en mundo, así que se traduce a local por la escala del
            // objeto: si no, en un objeto diminuto el engorde se lo come la escala.
            Vector3 scale = go.transform.lossyScale;
            Vector3 size = local.size;
            size.x = Mathf.Max(size.x, MinClickThickness / Mathf.Max(Mathf.Abs(scale.x), 1e-4f));
            size.y = Mathf.Max(size.y, MinClickThickness / Mathf.Max(Mathf.Abs(scale.y), 1e-4f));
            size.z = Mathf.Max(size.z, MinClickThickness / Mathf.Max(Mathf.Abs(scale.z), 1e-4f));
            box.size = size;
        }

        /// <summary>La caja de la malla en el espacio del renderer; sin malla, la del mundo pasada a él.</summary>
        private static Bounds MeshBounds(Renderer r)
        {
            if (r is SkinnedMeshRenderer skinned)
                return skinned.localBounds;
            MeshFilter filter = r.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
                return filter.sharedMesh.bounds;
            Bounds world = r.bounds;
            return new Bounds(r.transform.InverseTransformPoint(world.center),
                              r.transform.InverseTransformVector(world.size));
        }

        /// <summary>
        /// Borra las entidades que este script creó para elementos que ya no están en
        /// ninguna zona. Si no, apagar un elemento del catálogo deja su cubo gris en el
        /// garaje: clicable, mudo —no pertenece a ninguna zona— y confuso.
        /// </summary>
        private static void RemoveOrphanSpawned()
        {
            GameObject spawned = Find(SpawnedRoot);
            if (spawned == null)
                return;

            HashSet<string> wanted = new HashSet<string>();
            foreach (ZoneSO zone in AllZones())
                foreach (SafetyItemSO item in zone.Items)
                    if (item != null)
                        wanted.Add($"module01.{zone.ZoneId}.{item.ItemId}");

            int removed = 0;
            foreach (WorldObjectTaskEntity entity in
                     spawned.GetComponentsInChildren<WorldObjectTaskEntity>(true))
            {
                if (wanted.Contains(entity.Id.Value))
                    continue;
                Debug.Log($"[Module01] Borrada la entidad huerfana '{entity.Id.Value}'.");
                Undo.DestroyObjectImmediate(entity.gameObject);
                removed++;
            }

            if (removed > 0)
                Debug.Log($"[Module01] Entidades huerfanas borradas: {removed}.");
        }

        private static IEnumerable<ZoneSO> AllZones()
        {
            Module01CatalogSO catalog = AssetDatabase.LoadAssetAtPath<Module01CatalogSO>(
                "Assets/_Project/Module01/Data/SO_Module01Catalog.asset");
            if (catalog != null)
                foreach (ZoneSO zone in catalog.Zones)
                    if (zone != null)
                        yield return zone;

            foreach (ZoneSO zone in VehicleProfiles.AllInspectionZones())
                yield return zone;
        }

        /// <summary>
        /// Busca por nombre <b>sin los espacios de los bordes</b>.
        /// <para>
        /// El v9 entrega el pantalon ajustado doblado como <c>"jean_ajustado_doblado "</c>, con
        /// un espacio al final. Comparando el nombre tal cual, ese EPP se queda sin entidad y
        /// lo unico que se ve es un aviso en la consola: un caracter invisible no puede decidir
        /// si un elemento entra al ejercicio.
        /// </para>
        /// </summary>
        private static GameObject Find(string name)
        {
            string wanted = name.Trim();
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (go.name.Trim() == wanted)
                    return go;
            return null;
        }
    }
}
