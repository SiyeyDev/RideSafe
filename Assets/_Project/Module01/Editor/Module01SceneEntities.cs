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
        /// <summary>(nombre del objeto en la escena, zona, id del elemento del catálogo).</summary>
        private static readonly (string Obj, string Zone, string Item)[] Entities =
        {
            ("df_g_helmet_01",     "head",     "helmet_ok"),
            ("casco 1",            "head",     "helmet_damaged"),
            ("gafas",              "head",     "glasses"),
            ("botas",              "clothing", "shoes_ok"),
            ("chaleco refectivo",  "clothing", "reflective"),
            // La bufanda es "ropa o cordones sueltos": el riesgo que le faltaba a la zona de
            // ropa desde que se apago clothing_loose por no haber modelo. El v7 lo trajo.
            ("bufanda 01",         "clothing", "clothing_loose"),
            // Los pantalones: ajustado bien, ancho se engancha en la cadena, deportivo da igual.
            // Desde el v9 se usan los DOBLADOS: miden 5 cm de canto en vez de 1,16 m de alto,
            // asi que se apoyan en la tapa como el resto en vez de plantarse en el suelo.
            // Ojo: el arte entrega el del ajustado con un espacio al final del nombre.
            ("jean_ajustado_doblado",      "trousers", "trousers_fitted"),
            ("jean ancho doblado",         "trousers", "trousers_loose"),
            ("pantalon deportivo doblado", "trousers", "trousers_tech"),
            // Los dos pares del v9. La entidad va en el PADRE del par, no en cada pieza.
            ("coderas",            "clothing", "elbow_pads"),
            ("rodilleras",         "clothing", "knee_pads"),
            ("guaya candado",      "load",     "cargo_secured"),
            ("MobilePhone_01",     "load",     "cargo_handheld")
        };

        /// <summary>
        /// Los EPP que el arte entrega <b>por pares</b>: (nombre del grupo, zona, piezas).
        /// <para>
        /// Una rodillera suelta no es un elemento del catalogo; el par si. Se agrupan bajo un
        /// padre y la entidad va en el padre, de modo que el clic, el contorno y el reporte
        /// tratan el par como una sola cosa. Si no, la segunda pieza se queda sin entidad y
        /// aparece como decorado suelto encima de la mesa.
        /// </para>
        /// </summary>
        private static readonly (string Group, string Zone, string[] Parts)[] Pairs =
        {
            ("coderas",    "clothing", new[] { "codera",    "codera (1)" }),
            ("rodilleras", "clothing", new[] { "rodillera", "rodillera (1)" })
        };

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
            HideSuperseded();
            GroupPairs();
            TagEntities();
            FillMissingEntities();
            RemoveOrphanSpawned();
        }

        /// <summary>Pone la entidad de tarea sobre cada objeto de EPP ya colocado por arte.</summary>
        private static void TagEntities()
        {
            foreach ((string objName, string zone, string item) in Entities)
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
        /// Toda zona debe tener una entidad por elemento, o nunca se cierra y el módulo
        /// se queda esperando. Lo que el equipo de arte no colocó se crea aquí: el
        /// modelo del catálogo si lo tiene, y si no una primitiva gris.
        /// </summary>
        private static void FillMissingEntities()
        {
            Module01CatalogSO catalog = AssetDatabase.LoadAssetAtPath<Module01CatalogSO>(
                "Assets/_Project/Module01/Data/SO_Module01Catalog.asset");
            VehicleProfileSO vehicle = AssetDatabase.LoadAssetAtPath<VehicleProfileSO>(
                "Assets/_Project/Module01/Data/SO_VehicleProfile_Ebike.asset");

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
            if (vehicle != null)
                foreach (ZoneSO z in vehicle.InspectionZones) if (z != null) all.Add((z, true));

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

            VehicleProfileSO vehicle = AssetDatabase.LoadAssetAtPath<VehicleProfileSO>(
                "Assets/_Project/Module01/Data/SO_VehicleProfile_Ebike.asset");
            if (vehicle != null)
                foreach (ZoneSO zone in vehicle.InspectionZones)
                    if (zone != null)
                        yield return zone;
        }

        /// <summary>Nombre del primer objeto de arte de la zona, para colocar su ancla.</summary>
        public static string FirstObjectNameOfZone(string zone)
        {
            foreach ((string objName, string z, string _) in Entities)
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
