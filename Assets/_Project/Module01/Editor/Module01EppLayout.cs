using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Lo que queda por colocar en las mesas de EPP cuando el artista ya colocó los suyos: el
    /// signo «!» de lo que aún no tiene modelo, y el punto desde el que se mira cada mesa.
    /// <para>
    /// Hasta el 2026-10-27 esto repartía todos los EPP en fila sobre un mueble por mesa y
    /// apartaba el decorado que estorbaba. Ahora los EPP van donde los dejó el artista
    /// (<see cref="Module01ArtEpp"/>) y aquí <b>no se mueve nada suyo</b>: solo lo que este
    /// proyecto instancia, y solo a huecos libres.
    /// </para>
    /// <para>
    /// Los muebles <b>no tienen collider</b>, así que no se sondea con física: se comparan cajas
    /// de renderers, que es lo que de verdad ocupa sitio.
    /// </para>
    /// </summary>
    public static class Module01EppLayout
    {
        private const string CatalogAsset = "Assets/_Project/Module01/Data/SO_Module01Catalog.asset";
        private const string AnchorsRoot = "Module01_Anchors";
        private const string SpawnedRoot = "Module01_Spawned";
        private const string ArtFolder = "Assets/_Project/Models/";

        /// <summary>Por encima de esto no es un objeto: es el suelo, un muro o el techo.</summary>
        private const float PropSizeLimit = 8f;

        [MenuItem("RideSafe/Módulo 1/Colocar los signos y el punto de vista de las mesas de EPP")]
        public static void LayoutOpenScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Module01] Sal de Play antes de colocar las mesas.");
                return;
            }

            Module01CatalogSO catalog = AssetDatabase.LoadAssetAtPath<Module01CatalogSO>(CatalogAsset);
            if (catalog == null)
            {
                Debug.LogError($"[Module01] No hay catálogo en {CatalogAsset}.");
                return;
            }

            Dictionary<string, WorldObjectTaskEntity> byId =
                Object.FindObjectsByType<WorldObjectTaskEntity>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(e => !string.IsNullOrEmpty(e.ItemId))
                    .GroupBy(e => e.ItemId)
                    .ToDictionary(g => g.Key, g => g.First());

            List<ZoneSO> zones = catalog.Zones.Where(z => z != null).ToList();
            foreach (ZoneSO zone in zones)
                PlaceLoose(zone, Mine(zone, byId));

            // Las cajas de los colliders van con retraso respecto a los transforms recién movidos.
            Physics.SyncTransforms();
            List<Bounds> blocked = Obstacles();
            Bounds floor = GarageFloor();
            foreach (ZoneSO zone in zones)
                PlaceViewPoint(zone, Mine(zone, byId), blocked, floor);

            RevealLoose(zones, byId);
            // Con todo en su sitio: quién se pulsa por su forma y quién por su caja depende de
            // lo que tenga al lado.
            Module01SceneEntities.FitClickShapes();
            Physics.SyncTransforms();
            ReportHidden(zones, byId);
            RemoveRetiredAnchors(zones);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        /// <summary>
        /// Quita las anclas de zonas que ya no existen: las de las tres mesas del Excel desde que
        /// las paradas son muebles, y las de las estaciones de antes. Quedan las de las paradas,
        /// las del vehículo y la de su punto de exhibición.
        /// </summary>
        private static void RemoveRetiredAnchors(List<ZoneSO> zones)
        {
            GameObject root = Find(AnchorsRoot);
            if (root == null)
                return;
            HashSet<string> alive = new HashSet<string>(zones.Select(z => "anchor_" + z.ZoneId)
                .Concat(VehicleProfiles.AllInspectionZones().Select(z => "anchor_" + z.ZoneId))
                .Append("anchor_" + Module01Director.VehicleAnchorId));
            foreach (Transform anchor in root.transform.Cast<Transform>().ToList())
                if (!alive.Contains(anchor.name))
                {
                    Debug.Log($"[Module01] Fuera '{anchor.name}': su zona ya no existe.");
                    Undo.DestroyObjectImmediate(anchor.gameObject);
                }
        }

        /// <summary>
        /// Lo que, con todo colocado, sigue sin verse desde el ancla de su mesa: un EPP que el
        /// artista dejó detrás de otra cosa, o un signo que no encontró hueco a la vista.
        /// </summary>
        private static void ReportHidden(List<ZoneSO> zones, Dictionary<string, WorldObjectTaskEntity> byId)
        {
            Physics.SyncTransforms();
            List<Bounds> occluders = Occluders(null);
            foreach (ZoneSO zone in zones)
            {
                Transform anchor = Find("anchor_" + zone.ZoneId)?.transform;
                if (anchor == null)
                    continue;
                List<string> hidden = Mine(zone, byId)
                    .Where(e => TryBounds(e.gameObject, out Bounds b)
                                && StationLayout.Hidden(anchor.position, new List<Bounds> { Clickable(e, b) }, occluders) > 0)
                    .Select(e => $"{e.ItemId} ({e.name})").ToList();
                if (hidden.Count > 0)
                    Debug.LogWarning($"[Module01] Desde el ancla de '{zone.ZoneId}' no se ve entero: " +
                                     string.Join(", ", hidden) + ".");
            }
        }

        private static List<WorldObjectTaskEntity> Mine(ZoneSO zone, Dictionary<string, WorldObjectTaskEntity> byId) =>
            zone.Items.Where(i => i != null && byId.ContainsKey(i.ItemId)).Select(i => byId[i.ItemId]).ToList();

        /// <summary>Lo de la mesa que instancia este proyecto, no el artista: los signos «!».</summary>
        private static List<WorldObjectTaskEntity> LooseOf(List<WorldObjectTaskEntity> mine) =>
            mine.Where(IsLoose).ToList();

        private static bool IsLoose(WorldObjectTaskEntity e)
        {
            GameObject spawnedRoot = Find(SpawnedRoot);
            return spawnedRoot != null && e.transform.IsChildOf(spawnedRoot.transform);
        }

        /// <summary>Los muebles de la mesa que están en la escena y cuya tapa cuadra.</summary>
        private static List<(Surface Surface, GameObject Go, Bounds Box)> Tops(ZoneSO zone)
        {
            var tops = new List<(Surface, GameObject, Bounds)>();
            if (!Module01Stations.TryGet(zone.ZoneId, out Station station))
            {
                Debug.LogWarning($"[Module01] La zona '{zone.ZoneId}' no tiene mueble declarado en Module01Stations; " +
                                 "sus signos se quedan donde estén.");
                return tops;
            }
            foreach (Surface s in station.Surfaces)
                if (TryTop(zone.ZoneId, s, out GameObject go, out Bounds box))
                    tops.Add((s, go, box));
            return tops;
        }

        /// <summary>
        /// Lo que ocupa sitio en los muebles. El mueble mismo no cuenta: su caja incluye las
        /// alacenas de encima de la tapa, y un signo cabe debajo de ellas.
        /// </summary>
        private static List<Bounds> Occupied(List<(Surface Surface, GameObject Go, Bounds Box)> tops,
            List<WorldObjectTaskEntity> except) =>
            Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(r => r.enabled && r.bounds.size.magnitude <= PropSizeLimit
                            && r.GetComponentInParent<Canvas>() == null
                            && !tops.Any(t => r.transform.IsChildOf(t.Go.transform))
                            && !except.Any(e => r.transform.IsChildOf(e.transform)))
                .Select(r => r.bounds).ToList();

        /// <summary>
        /// Lleva los signos «!» de la mesa a huecos libres de su mueble, lo más cerca posible de
        /// lo que el artista puso de esa mesa.
        /// </summary>
        private static void PlaceLoose(ZoneSO zone, List<WorldObjectTaskEntity> mine)
        {
            List<WorldObjectTaskEntity> loose = LooseOf(mine);
            if (loose.Count == 0)
                return;
            var tops = Tops(zone);
            if (tops.Count == 0)
                return;

            List<WorldObjectTaskEntity> placed = mine.Except(loose).ToList();
            Vector3 near = placed.Count > 0 && TryBounds(placed.Select(e => e.gameObject), out Bounds group)
                ? group.center
                : tops[0].Box.center;
            List<Bounds> occupied = Occupied(tops, loose);

            foreach (WorldObjectTaskEntity entity in loose)
            {
                if (!TryBounds(entity.gameObject, out Bounds box))
                    continue;
                if (!TryFirstSpot(tops, Clickable(entity, box).size, occupied, near, _ => true, out Vector3 spot, out Bounds used))
                {
                    Debug.LogWarning($"[Module01] No queda hueco libre en los muebles de '{zone.ZoneId}' para " +
                                     $"'{entity.ItemId}'; se queda donde está.");
                    continue;
                }
                Move(entity, spot, occupied, used);
            }
        }

        /// <summary>
        /// Con las anclas puestas, cada signo vuelve a buscar sitio: uno que se vea desde el ancla de
        /// su mesa y que no tape nada que antes se viera desde <b>ninguna</b> ancla. El más cercano a
        /// su grupo quedaba detrás del chaleco del artista, o delante del signo de otra mesa.
        /// </summary>
        private static void RevealLoose(List<ZoneSO> zones, Dictionary<string, WorldObjectTaskEntity> byId)
        {
            Physics.SyncTransforms();
            var views = new List<(ZoneSO Zone, Vector3 Eye, List<WorldObjectTaskEntity> Mine)>();
            foreach (ZoneSO zone in zones)
            {
                Transform anchor = Find("anchor_" + zone.ZoneId)?.transform;
                if (anchor != null)
                    views.Add((zone, anchor.position, Mine(zone, byId)));
            }
            List<WorldObjectTaskEntity> allLoose = views.SelectMany(v => LooseOf(v.Mine)).ToList();
            List<Bounds> fixedOccluders = Occluders(allLoose);
            var where = new Dictionary<WorldObjectTaskEntity, Bounds>();
            foreach (WorldObjectTaskEntity e in allLoose)
                if (TryBounds(e.gameObject, out Bounds b))
                    where[e] = Clickable(e, b);

            foreach ((ZoneSO zone, Vector3 eye, List<WorldObjectTaskEntity> mine) in views)
            {
                var tops = Tops(zone);
                if (tops.Count == 0)
                    continue;
                foreach (WorldObjectTaskEntity entity in LooseOf(mine))
                {
                    if (!where.TryGetValue(entity, out Bounds box))
                        continue;
                    where.Remove(entity);
                    List<Bounds> occupied = Occupied(tops, allLoose).Concat(where.Values).ToList();
                    List<Bounds> around = fixedOccluders.Concat(where.Values).ToList();
                    int before = TotalHidden(views, where, around, null);

                    bool Shows(Vector3 c)
                    {
                        Bounds at = new Bounds(c, box.size);
                        // Fuera de la burbuja de foco de su ancla, como todo lo de la mesa. El alcance
                        // del halo del tutorial no cuenta: un signo no tiene malla que delinear.
                        return Vector3.Distance(eye, at.ClosestPoint(eye)) >= Module01Stations.ViewDistance
                               && StationLayout.Hidden(eye, new List<Bounds> { at }, around) == 0
                               && TotalHidden(views, where, around, (entity, at)) <= before;
                    }

                    if (TryFirstSpot(tops, box.size, occupied, box.center, Shows, out Vector3 spot, out Bounds used))
                        box = Move(entity, spot, occupied, used);
                    else
                        Debug.LogWarning($"[Module01] Ningún hueco de '{zone.ZoneId}' deja '{entity.ItemId}' a la vista " +
                                         "sin tapar otra cosa; se queda donde estaba.");
                    where[entity] = box;
                }
            }
        }

        /// <summary>
        /// Cuántos elementos quedan tapados sumando todas las anclas, con los signos donde dice
        /// <paramref name="loose"/> y, si se da, <paramref name="moving"/> en su sitio de prueba.
        /// </summary>
        private static int TotalHidden(List<(ZoneSO Zone, Vector3 Eye, List<WorldObjectTaskEntity> Mine)> views,
            Dictionary<WorldObjectTaskEntity, Bounds> loose, List<Bounds> around,
            (WorldObjectTaskEntity Entity, Bounds At)? moving)
        {
            List<Bounds> occluders = moving == null ? around : around.Append(moving.Value.At).ToList();
            int hidden = 0;
            foreach ((ZoneSO _, Vector3 eye, List<WorldObjectTaskEntity> mine) in views)
            {
                List<Bounds> targets = new List<Bounds>();
                foreach (WorldObjectTaskEntity e in mine)
                {
                    if (loose.TryGetValue(e, out Bounds b))
                        targets.Add(b);
                    else if (moving != null && e == moving.Value.Entity)
                        targets.Add(moving.Value.At);
                    else if (!IsLoose(e) && TryBounds(e.gameObject, out Bounds art))
                        targets.Add(Clickable(e, art));
                }
                hidden += StationLayout.Hidden(eye, targets, occluders);
            }
            return hidden;
        }

        /// <summary>El primer hueco libre de los muebles, en orden, que cumpla <paramref name="accept"/>.</summary>
        private static bool TryFirstSpot(List<(Surface Surface, GameObject Go, Bounds Box)> tops, Vector3 size,
            List<Bounds> occupied, Vector3 near, System.Func<Vector3, bool> accept, out Vector3 spot, out Bounds used)
        {
            foreach ((Surface s, GameObject _, Bounds top) in tops)
                foreach (Vector3 c in StationLayout.FreeCandidates(top, s.WorktopHeight, size, occupied, near))
                    if (accept(c))
                    {
                        spot = c;
                        used = top;
                        return true;
                    }
            spot = default;
            used = default;
            return false;
        }

        /// <summary>Lleva el signo al hueco y deja ocupado su tramo del mueble.</summary>
        private static Bounds Move(WorldObjectTaskEntity entity, Vector3 spot, List<Bounds> occupied, Bounds top)
        {
            TryBounds(entity.gameObject, out Bounds box);
            box = Clickable(entity, box);
            Undo.RecordObject(entity.transform, "Colocar signo");
            entity.transform.position += spot - box.center;
            occupied.Add(DepthColumn(top, spot, box.size));
            return new Bounds(spot, box.size);
        }

        /// <summary>
        /// Lo que un signo deja ocupado: su tramo del mueble <b>en todo el fondo</b>. Otro signo
        /// justo detrás, en el mismo tramo, quedaría tapado por este desde donde se mira la mesa
        /// —pasó con los de «Visibilidad y control» en la captura del 2026-10-27—.
        /// </summary>
        private static Bounds DepthColumn(Bounds top, Vector3 spot, Vector3 size)
        {
            bool alongX = top.size.x >= top.size.z;
            return new Bounds(
                new Vector3(alongX ? spot.x : top.center.x, spot.y, alongX ? top.center.z : spot.z),
                new Vector3(alongX ? size.x : top.size.x, size.y, alongX ? top.size.z : size.z));
        }

        /// <summary>El mueble en la escena, si está y su tapa declarada cae dentro de él.</summary>
        private static bool TryTop(string zoneId, Surface surface, out GameObject go, out Bounds box)
        {
            box = default;
            go = Find(surface.Name);
            if (go == null || !TryBounds(go, out box))
            {
                Debug.LogError($"[Module01] La zona '{zoneId}' apunta al mueble '{surface.Name}', " +
                               "que no está en la escena o no tiene malla.");
                return false;
            }

            // Una tapa declarada que cae fuera del mueble significa que el arte cambió y el
            // dato se quedó viejo. Es la única forma de enterarse: no hay collider que sondear.
            if (surface.WorktopHeight < box.min.y - .05f || surface.WorktopHeight > box.max.y + .05f)
            {
                Debug.LogError($"[Module01] La tapa declarada de '{surface.Name}' está en y={surface.WorktopHeight:F2}, " +
                               $"fuera de su caja (y de {box.min.y:F2} a {box.max.y:F2}). El mueble ha " +
                               "cambiado: hay que volver a medirla.");
                return false;
            }
            box = surface.Clip(box);
            return true;
        }

        /// <summary>
        /// Planta el ancla de la parada de frente a su mueble y centrada en él (ver
        /// <see cref="StationLayout.TryFrontView"/>).
        /// </summary>
        private static void PlaceViewPoint(ZoneSO zone, List<WorldObjectTaskEntity> mine, List<Bounds> blocked, Bounds floor)
        {
            var tops = Tops(zone);
            if (tops.Count == 0)
                return;
            Bounds furniture = tops[0].Box;
            foreach (var top in tops)
                furniture.Encapsulate(top.Box);

            // Solo lo del artista: los signos se recolocan después a la distancia de la burbuja.
            List<Bounds> items = new List<Bounds>();
            foreach (WorldObjectTaskEntity e in mine.Where(e => !IsLoose(e)))
                if (TryBounds(e.gameObject, out Bounds b))
                    items.Add(Clickable(e, b));

            if (!StationLayout.TryFrontView(furniture, items, blocked, floor,
                    Module01Stations.ViewDistance, Module01Stations.EyeHeight, out Pose pose))
            {
                Debug.LogError($"[Module01] Delante de '{tops[0].Surface.Name}' no hay sitio libre dentro del garaje " +
                               $"para mirar '{zone.ZoneId}' de frente.");
                return;
            }

            PlaceAnchor(zone.ZoneId, pose);
            Debug.Log($"[Module01] '{zone.ZoneId}': {items.Count} elementos, ancla en {pose.position:F2} " +
                      $"mirando a {pose.rotation.eulerAngles.y:F0}°.");
        }

        /// <summary>
        /// Lo que ocupa el elemento para el clic: lo que se ve y su collider, que es más grueso a
        /// propósito (6 cm como mínimo) y es lo que de verdad intercepta el rayo del puntero.
        /// </summary>
        private static Bounds Clickable(WorldObjectTaskEntity entity, Bounds visible)
        {
            foreach (Collider c in entity.GetComponentsInChildren<Collider>(false))
                if (c.enabled)
                    visible.Encapsulate(c.bounds);
            return visible;
        }

        /// <summary>
        /// Donde no se puede plantar el aprendiz: el mobiliario, los EPP y los vehículos —también
        /// el que está apagado, que se enciende al elegirlo—. Solo arte importado: la UI y el
        /// propio jugador no ocupan sitio en el suelo.
        /// </summary>
        private static List<Bounds> Obstacles() =>
            Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(r => r.bounds.size.magnitude <= PropSizeLimit && IsArt(r))
                .Select(r => r.bounds).ToList();

        /// <summary>
        /// Lo que puede tapar un EPP: los objetos encendidos del arte que caben en
        /// <see cref="OccluderSizeLimit"/>. Los muebles no: cada uno es un solo renderer cuya caja
        /// incluye las alacenas, y envuelve justo lo que tiene encima.
        /// </summary>
        private static List<Bounds> Occluders(List<WorldObjectTaskEntity> except) =>
            Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(r => r.enabled && r.bounds.size.magnitude <= OccluderSizeLimit && IsArt(r))
                .Select(r => r.bounds)
                .Concat(Object.FindObjectsByType<WorldObjectTaskEntity>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .Where(e => except == null || !except.Contains(e))
                    .SelectMany(e => e.GetComponentsInChildren<Collider>(false))
                    .Where(c => c.enabled).Select(c => c.bounds))
                .ToList();

        /// <summary>Lo más grande que cuenta como objeto que tapa: el gazón mide 1,76 y una mesa 4,1.</summary>
        private const float OccluderSizeLimit = 2f;

        private static bool IsArt(Renderer r)
        {
            Mesh mesh = r is SkinnedMeshRenderer s ? s.sharedMesh
                : r.TryGetComponent(out MeshFilter f) ? f.sharedMesh : null;
            return mesh != null && AssetDatabase.GetAssetPath(mesh).StartsWith(ArtFolder);
        }

        /// <summary>Crea o recoloca <c>anchor_&lt;zona&gt;</c>.</summary>
        private static void PlaceAnchor(string zoneId, Pose pose)
        {
            GameObject root = Find(AnchorsRoot);
            if (root == null)
            {
                root = new GameObject(AnchorsRoot);
                Undo.RegisterCreatedObjectUndo(root, "Raiz de anclas");
            }

            string name = "anchor_" + zoneId;
            Transform anchor = root.transform.Find(name);
            if (anchor == null)
            {
                anchor = new GameObject(name).transform;
                anchor.SetParent(root.transform, false);
                Undo.RegisterCreatedObjectUndo(anchor.gameObject, "Ancla de zona");
            }

            Undo.RecordObject(anchor, "Colocar ancla");
            anchor.SetPositionAndRotation(pose.position, pose.rotation);
        }

        /// <summary>La planta del garaje, un palmo hacia dentro: pegado a la pared tampoco vale.</summary>
        private static Bounds GarageFloor()
        {
            GameObject floor = Find("piso");
            if (floor == null || !TryBounds(floor, out Bounds b))
                return new Bounds(Vector3.zero, new Vector3(1000f, 1000f, 1000f));
            b.Expand(new Vector3(-.60f, 0f, -.60f));
            return b;
        }

        private static GameObject Find(string name) => Object
            .FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(t => t.name == name)?.gameObject;

        private static bool TryBounds(GameObject go, out Bounds box) => TryBounds(new[] { go }, out box);

        private static bool TryBounds(IEnumerable<GameObject> gos, out Bounds box)
        {
            Renderer[] renderers = gos.SelectMany(g => g.GetComponentsInChildren<Renderer>(true)).ToArray();
            if (renderers.Length == 0)
            {
                box = default;
                return false;
            }
            box = renderers[0].bounds;
            foreach (Renderer r in renderers)
                box.Encapsulate(r.bounds);
            return true;
        }
    }
}
