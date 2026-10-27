using System.Collections.Generic;
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
        /// <summary>
        /// (nombre del objeto en la escena, zona, id del elemento del catálogo), para los pocos
        /// elementos cuyo modelo <b>ya está colocado</b> por el paquete de arte.
        /// <para>
        /// Sale de <see cref="Module01Library"/>: solo las filas que declaran
        /// <see cref="Module01Library.Entry.SceneObject"/>. Todo lo demás lo instancia
        /// <see cref="FillMissingEntities"/> desde el <c>DisplayPrefab</c> del catálogo —o con
        /// el signo «!» si todavía no hay modelo—, que es lo que permite que la escena salga
        /// entera del documento del cliente y no de lo que el garaje traiga encima.
        /// </para>
        /// <para>
        /// Esta lista estaba escrita a mano y ataba el ejercicio a los nombres que Daniel puso
        /// a sus modelos. De ahí venían la bufanda, el jean ancho, el pantalón deportivo y los
        /// dos pares, que el cliente nunca pidió.
        /// </para>
        /// </summary>
        private static IEnumerable<(string Obj, string Zone, string Item)> Entities()
        {
            foreach (Module01Library.Entry e in Module01Library.All)
                if (!string.IsNullOrEmpty(e.SceneObject))
                    yield return (e.SceneObject, e.Table, e.ItemId);
        }

        /// <summary>
        /// Los EPP que el arte entrega <b>por pares</b>: (nombre del grupo, zona, piezas).
        /// <para>
        /// Una rodillera suelta no es un elemento del catalogo; el par si. Se agrupan bajo un
        /// padre y la entidad va en el padre, de modo que el clic, el contorno y el reporte
        /// tratan el par como una sola cosa. Si no, la segunda pieza se queda sin entidad y
        /// aparece como decorado suelto encima de la mesa.
        /// </para>
        /// </summary>
        /// <remarks>
        /// Vacía desde el 2026-10-22: los dos pares que había —coderas y rodilleras— no están
        /// en la biblioteca del cliente y salieron del catálogo. El mecanismo se queda montado
        /// porque la hoja sí trae elementos que el arte podría entregar por pares (los guantes,
        /// los mitones), y volver a escribirlo costaría más que dejar la tabla vacía.
        /// </remarks>
        private static readonly (string Group, string Zone, string[] Parts)[] Pairs =
            System.Array.Empty<(string, string, string[])>();

        /// <summary>Hueco entre las dos piezas de un par, a lo ancho de la tapa.</summary>
        private const float PairGap = .03f;

        /// <summary>
        /// Arte que una entrega posterior ha <b>sustituido</b>: (objeto viejo, quien lo sustituye).
        /// <para>
        /// El v9 trae los tres pantalones doblados, y los de pie se quedan en la escena sin
        /// entidad, duplicando el ejercicio: el aprendiz ve dos pantalones anchos y solo uno se
        /// puede pulsar. Encima, al no pertenecer ya a ninguna estacion, nadie los coloca y el
        /// despeje de pasillo los arrincona donde pueda —el ajustado acabo en (-4.49, 0.57,
        /// -4.47), contra la pared del fondo—.
        /// </para>
        /// <para>
        /// <b>Se apagan, no se borran.</b> Son modelos del equipo de arte y el cambio de mundo
        /// los vuelve a traer en cada entrega; apagarlos es reversible con un clic y volver a
        /// correr este comando los deja apagados otra vez.
        /// </para>
        /// </summary>
        private static readonly (string Viejo, string Sustituto)[] Superseded =
        {
            ("jean ajustado",  "jean_ajustado_doblado"),
            ("pantalon ancho", "jean ancho doblado"),
            ("pant low",       "pantalon deportivo doblado")
        };


        [MenuItem("RideSafe/Módulo 1/Etiquetar entidades en la escena abierta")]
        public static void ApplyToOpenScene()
        {
            Apply();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        /// <summary>Idempotente: reutiliza las entidades que ya estén puestas.</summary>
        public static void Apply()
        {
            RetireArtThatLeftTheExercise();
            HideSuperseded();
            GroupPairs();
            TagEntities();
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
        /// Apaga el arte que salió del ejercicio y le quita su entidad, para que no quede un
        /// objeto clicable que el catálogo ya no conoce.
        /// <para>
        /// Va <b>antes</b> de colocar: los EPP nuevos caen sobre las mismas tapas que ocupaba el
        /// arte viejo, así que si no se apaga primero quedan objetos superpuestos.
        /// </para>
        /// </summary>
        private static void RetireArtThatLeftTheExercise()
        {
            int apagados = 0;
            foreach (string nombre in Module01Library.RetiredSceneObjects)
            {
                GameObject go = Find(nombre);
                if (go == null)
                    continue;

                foreach (WorldObjectTaskEntity entity in go.GetComponentsInChildren<WorldObjectTaskEntity>(true))
                    Undo.DestroyObjectImmediate(entity);

                if (go.activeSelf)
                {
                    Undo.RecordObject(go, "Retirar arte del ejercicio");
                    go.SetActive(false);
                    apagados++;
                }
            }

            if (apagados > 0)
                Debug.Log($"[Module01] Arte apagado por salir del ejercicio: {apagados}.");
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

            GameObject spawned = Find("Module01_Spawned");
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

        /// <summary>Pone la entidad de tarea sobre cada objeto de EPP ya colocado por arte.</summary>
        private static void TagEntities()
        {
            foreach ((string objName, string zone, string item) in Entities())
            {
                GameObject go = Find(objName);
                if (go == null)
                {
                    Debug.LogWarning($"[Module01] No encuentro '{objName}' en la escena; {zone}.{item} queda sin entidad.");
                    continue;
                }

                // El collider se reajusta SIEMPRE, no solo cuando falta: al recibir un garaje
                // nuevo el objeto cambia de escala y de sitio, y un collider heredado del
                // garaje anterior deja el clic desplazado respecto a lo que se ve.
                BoxCollider box = Module01EditorComponents.Ensure<BoxCollider>(go);
                FitCollider(go, box);

                // Los colliders de los hijos se apagan: con dos volúmenes solapados gana el
                // que el raycast toque primero, que no siempre es el que lleva la entidad.
                foreach (Collider other in go.GetComponentsInChildren<Collider>(true))
                    if (other != box)
                        other.enabled = false;

                bool wasActive = go.activeSelf;
                go.SetActive(false);
                WorldObjectTaskEntity entity = Module01EditorComponents.Ensure<WorldObjectTaskEntity>(go);
                entity.Configure(new EntityId($"module01.{zone}.{item}"), item);
                go.SetActive(wasActive);
            }
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
            GameObject spawned = Find("Module01_Spawned");
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

            GameObject spare = Find("Module01_Spawned") ?? new GameObject("Module01_Spawned");
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
        private static void FitCollider(GameObject go, BoxCollider box)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return;

            Bounds local = new Bounds();
            bool first = true;
            foreach (Renderer r in renderers)
            {
                Bounds b = r.bounds;
                for (int k = 0; k < 8; k++)
                {
                    Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3(
                        (k & 1) == 0 ? -1 : 1,
                        (k & 2) == 0 ? -1 : 1,
                        (k & 4) == 0 ? -1 : 1));
                    Vector3 p = go.transform.InverseTransformPoint(corner);
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

        /// <summary>
        /// Borra las entidades que este script creó para elementos que ya no están en
        /// ninguna zona. Si no, apagar un elemento del catálogo deja su cubo gris en el
        /// garaje: clicable, mudo —no pertenece a ninguna zona— y confuso.
        /// </summary>
        private static void RemoveOrphanSpawned()
        {
            GameObject spawned = Find("Module01_Spawned");
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

        /// <summary>Nombre del primer objeto de arte de la zona, para colocar su ancla.</summary>
        public static string FirstObjectNameOfZone(string zone)
        {
            foreach ((string objName, string z, string _) in Entities())
                if (z == zone)
                    return objName;
            return null;
        }

        /// <summary>
        /// Apaga el arte que otra entrega ha sustituido, y solo si el sustituto esta en la
        /// escena: si el nuevo no ha llegado, mas vale ver el viejo que no ver nada.
        /// </summary>
        private static void HideSuperseded()
        {
            foreach ((string viejo, string sustituto) in Superseded)
            {
                GameObject nuevo = Find(sustituto);
                if (nuevo == null)
                {
                    Debug.LogWarning(
                        $"[Module01] '{viejo}' deberia sustituirlo '{sustituto}', que no esta en " +
                        "la escena; lo dejo encendido para no quedarnos sin el elemento.");
                    continue;
                }

                GameObject old = Find(viejo);
                if (old == null || !old.activeSelf)
                    continue;

                Undo.RecordObject(old, "Apagar arte sustituido");
                old.SetActive(false);
                Debug.Log($"[Module01] '{viejo}' apagado: lo sustituye '{sustituto}'.");
            }
        }

        /// <summary>
        /// Mete las dos piezas de cada par bajo un padre y las deja <b>a lo ancho</b> de la tapa.
        /// <para>
        /// A lo ancho y no a lo largo porque la fila de la estacion se reparte por el eje largo
        /// del mueble: un par puesto en fila gasta el doble del sitio que hace falta, y la mesa
        /// de ropa va justa —2.62 m para ocho elementos—. La mesa tiene 0.88 m de fondo y un par
        /// ocupa 0.32, asi que a lo ancho sobra sitio y a lo largo no.
        /// </para>
        /// <para>Idempotente: si el grupo ya esta montado, solo recoloca las piezas.</para>
        /// </summary>
        private static void GroupPairs()
        {
            foreach ((string group, string zoneId, string[] parts) in Pairs)
            {
                List<GameObject> pieces = new List<GameObject>();
                foreach (string part in parts)
                {
                    GameObject piece = Find(part);
                    if (piece != null && !pieces.Contains(piece))
                        pieces.Add(piece);
                }

                if (pieces.Count == 0)
                {
                    Debug.LogWarning(
                        $"[Module01] No encuentro ninguna pieza del par '{group}' " +
                        $"({string.Join(", ", parts)}); ese EPP se queda sin entidad.");
                    continue;
                }

                if (!Module01Stations.TryGet(zoneId, out Station station))
                {
                    Debug.LogWarning(
                        $"[Module01] El par '{group}' dice ser de la zona '{zoneId}', que no " +
                        "tiene estacion declarada; lo agrupo pero no lo recoloco.");
                }

                GameObject parent = Find(group);
                if (parent == null)
                {
                    parent = new GameObject(group);
                    parent.transform.position = pieces[0].transform.position;
                    Undo.RegisterCreatedObjectUndo(parent, "Agrupar par de EPP");
                }

                foreach (GameObject piece in pieces)
                {
                    if (piece.transform.parent == parent.transform)
                        continue;
                    Undo.RecordObject(piece.transform, "Agrupar par de EPP");
                    piece.transform.SetParent(parent.transform, true);
                }

                bool rowAlongX = station.Axis == StationAxis.X;

                List<Bounds> boxes = new List<Bounds>();
                foreach (GameObject piece in pieces)
                    boxes.Add(VisibleBounds(piece));

                float total = 0f;
                for (int i = 0; i < boxes.Count; i++)
                {
                    total += rowAlongX ? boxes[i].size.z : boxes[i].size.x;
                    if (i < boxes.Count - 1)
                        total += PairGap;
                }

                // Centrado donde ya estaba la primera pieza: el grupo no viaja, que de eso se
                // encarga despues el reparto de estaciones.
                float cursor = (rowAlongX ? boxes[0].center.z : boxes[0].center.x) - total * .5f;
                for (int i = 0; i < pieces.Count; i++)
                {
                    float half = (rowAlongX ? boxes[i].size.z : boxes[i].size.x) * .5f;
                    float at = cursor + half;

                    // A lo ancho cada pieza a su hueco, y a lo largo TODAS a la misma linea:
                    // si no, el par sigue pisando dos huecos de la fila y no se gana nada.
                    Vector3 delta = Vector3.zero;
                    if (rowAlongX)
                    {
                        delta.z = at - boxes[i].center.z;
                        delta.x = boxes[0].center.x - boxes[i].center.x;
                    }
                    else
                    {
                        delta.x = at - boxes[i].center.x;
                        delta.z = boxes[0].center.z - boxes[i].center.z;
                    }

                    Undo.RecordObject(pieces[i].transform, "Colocar par de EPP");
                    pieces[i].transform.position += delta;

                    cursor = at + half + PairGap;
                }

                Debug.Log(
                    $"[Module01] Par '{group}': {pieces.Count} piezas agrupadas, " +
                    $"{total:F2} m a lo ancho de la tapa.");
            }
        }

        /// <summary>Caja de lo que se ve de un objeto, con sus hijos. Vacia si no tiene malla.</summary>
        private static Bounds VisibleBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(go.transform.position, Vector3.zero);

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return b;
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
