using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Reparte las tres zonas personales por el garaje y apoya los EPP de cada una sobre el
    /// mueble que le toca.
    /// <para>
    /// Antes las tres estaban en el mismo mostrador del fondo: el aprendiz se teletransportaba
    /// tres veces para mirar siempre a la misma pared. Lo pidió Charlie el 2026-10-14 —«tenemos
    /// un entorno bonito, podemos usarlo»—. Qué mueble le toca a cada zona es dato, en
    /// <see cref="Module01Stations"/>; la aritmética de la fila y del punto de vista es
    /// <see cref="StationLayout"/>, que se afirma sin abrir una escena. Aquí solo queda
    /// aplicarlo a la escena abierta y apartar lo que estorbe.
    /// </para>
    /// <para>
    /// <b>El ancla de cada zona deja de estar escrita a mano:</b> se deriva del grupo que acaba
    /// de colocarse, a la distancia y altura de siempre, por el lado libre que declara la
    /// estación. Antes eran tres posiciones fijas en fila, que es justo lo que amarraba las tres
    /// zonas al mismo mueble.
    /// </para>
    /// <para>
    /// Los muebles <b>no tienen collider</b> —ni uno—, así que la altura de la tapa no se puede
    /// sondear con física y la oclusión no se puede comprobar con un rayo: contesta «despejado»
    /// y miente. Las tapas son dato medido y el despeje se hace comparando cajas.
    /// </para>
    /// </summary>
    public static class Module01EppLayout
    {
        private const string CatalogAsset = "Assets/_Project/Module01/Data/SO_Module01Catalog.asset";
        private const string AnchorsRoot = "Module01_Anchors";

        /// <summary>Lo que hay que dejar libre entre el aprendiz y su estación.</summary>
        private const float CorridorMargin = .30f;

        /// <summary>Hasta qué altura estorba de verdad. Por encima están las luces del techo.</summary>
        private const float EyeBand = 1.75f;

        /// <summary>Hueco entre el mueble y lo que se planta de pie en el suelo delante de él.</summary>
        private const float FloorStandoff = .30f;

        /// <summary>Por encima de esto ya no es un prop: es mobiliario, un muro o el suelo.</summary>
        private const float PropSizeLimit = 8f;

        /// <summary>
        /// El objeto que hay que mover para apartar esta malla: el ancestro más alto que
        /// <b>sigue siendo un prop</b>.
        /// <para>
        /// Se sube mientras el ancestro quepa en <see cref="PropSizeLimit"/>, así que un
        /// contenedor grande —<c>epps</c>, el garaje entero— corta la subida y no se mueve media
        /// escena por apartar una linterna. Sin esto se movían los hijos por separado y los
        /// modelos de varias piezas se partían.
        /// </para>
        /// </summary>
        private static Transform PropRoot(Transform mesh, GameObject surface)
        {
            Transform mejor = mesh;
            for (Transform t = mesh.parent; t != null; t = t.parent)
            {
                if (surface != null && t == surface.transform) break;
                Bounds? b = WorldBounds(t);
                if (b == null || b.Value.size.magnitude > PropSizeLimit) break;
                mejor = t;
            }
            return mejor;
        }

        /// <summary>La caja de todas las mallas visibles que cuelgan de este objeto.</summary>
        private static Bounds? WorldBounds(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(false);
            if (rs.Length == 0) return null;
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        private static Bounds Encapsulated(Bounds a, Bounds b)
        {
            a.Encapsulate(b);
            return a;
        }

        [MenuItem("RideSafe/Módulo 1/Repartir las estaciones de EPP por el garaje")]
        public static void LayoutOpenScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Module01] Sal de Play antes de recolocar los EPP.");
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

            int done = 0;
            foreach (ZoneSO zone in catalog.Zones)
            {
                if (zone != null && LayoutZone(zone, byId))
                    done++;
            }

            // El collider de cada EPP es local y los objetos se han movido: hay que reajustarlo
            // o el clic queda desplazado respecto a lo que se ve.
            Module01SceneEntities.Apply();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[Module01] {done} estaciones repartidas por el garaje.");
        }

        private static bool LayoutZone(ZoneSO zone, Dictionary<string, WorldObjectTaskEntity> byId)
        {
            if (!Module01Stations.TryGet(zone.ZoneId, out Station station))
            {
                Debug.LogWarning(
                    $"[Module01] La zona '{zone.ZoneId}' no tiene estación declarada en " +
                    "Module01Stations; sus EPP se quedan donde el arte los dejó.");
                return false;
            }

            GameObject surfaceGo = Find(station.SurfaceName);
            if (surfaceGo == null || !TryBounds(surfaceGo, out Bounds surface))
            {
                Debug.LogError(
                    $"[Module01] La zona '{zone.ZoneId}' apunta al mueble '{station.SurfaceName}', " +
                    "que no está en la escena o no tiene malla. Esa estación se queda sin montar.");
                return false;
            }

            // Una tapa declarada que cae fuera del mueble significa que el arte cambió y el
            // dato se quedó viejo. Es la única forma de enterarse: no hay collider que sondear.
            if (station.WorktopHeight < surface.min.y - .05f || station.WorktopHeight > surface.max.y + .05f)
            {
                Debug.LogError(
                    $"[Module01] La tapa declarada de '{zone.ZoneId}' está en y={station.WorktopHeight:F2}, " +
                    $"fuera de la caja de '{station.SurfaceName}' (y de {surface.min.y:F2} a {surface.max.y:F2}). " +
                    "El mueble ha cambiado: hay que volver a medirla.");
                return false;
            }

            List<(WorldObjectTaskEntity Entity, Bounds Box)> plan = new List<(WorldObjectTaskEntity, Bounds)>();
            foreach (SafetyItemSO item in zone.Items)
            {
                if (item == null || !byId.TryGetValue(item.ItemId, out WorldObjectTaskEntity entity))
                    continue;
                if (!TryBounds(entity.gameObject, out Bounds box))
                {
                    Debug.LogWarning($"[Module01] '{item.ItemId}' no tiene renderer; no se recoloca.");
                    continue;
                }
                plan.Add((entity, box));
            }

            if (plan.Count == 0)
            {
                Debug.LogWarning($"[Module01] La zona '{zone.ZoneId}' no tiene ningún EPP en la escena.");
                return false;
            }

            List<Vector3> sizes = plan.Select(p => p.Box.size).ToList();

            // El hueco declarado es un maximo comodo: si la fila no entra, se aprieta hasta el
            // minimo legible antes de dejarla colgando por los lados. Con los pantalones
            // doblados y los dos pares, la ropa pide mas de lo que da la mesa al hueco de
            // siempre, y apretar es mejor que que las botas floten fuera de la tapa.
            float gap = StationLayout.FittingGap(surface, station.Axis, sizes, Module01Stations.Gap);
            float span = StationLayout.Span(sizes, station.Axis, gap);
            float room = station.Axis == StationAxis.X ? surface.size.x : surface.size.z;

            Vector3[] spots;
            string forma;
            if (span <= room)
            {
                spots = StationLayout.Arrange(surface, station.WorktopHeight, station.Axis, sizes, gap);
                forma = "una fila";
            }
            else
            {
                // No cabe en una sola fila, asi que se usa el FONDO de la tapa: lo plano
                // delante y lo alto detras. Lo plano no puede tapar nada -5 cm de canto- y la
                // alternativa es dejar la fila colgando por los lados, que es lo que pasaba.
                spots = ArrangeTwoRanks(surface, station, sizes, out gap, out span, out int delante);
                forma = $"dos filas ({delante} planos delante)";
            }

            if (span > room)
                Debug.LogWarning(
                    $"[Module01] Los {plan.Count} elementos de '{zone.ZoneId}' miden {span:F2} m y " +
                    $"'{station.SurfaceName}' solo da {room:F2} m, ya en {forma} y con el hueco " +
                    $"apretado a {gap:F2}: van a colgar por los lados.");

            float floorY = GarageFloorTop();
            for (int i = 0; i < plan.Count; i++)
            {
                // Lo que no cabe de pie sobre la tapa -los pantalones miden mas de un metro- se
                // planta en el suelo, conservando su hueco en la fila para no tapar a nadie.
                if (StationLayout.StandsOnFloor(sizes[i]))
                    spots[i] = StationLayout.OnFloor(
                        spots[i], sizes[i], surface, floorY, station.Outward, FloorStandoff);

                // Se mueve por la diferencia entre el centro visible y el destino, no se asigna
                // la posición: el pivote de un FBX casi nunca está en el centro de la malla, y
                // asignarla lo deja descolocado y hundido.
                Undo.RecordObject(plan[i].Entity.transform, "Repartir EPP");
                plan[i].Entity.transform.position += spots[i] - plan[i].Box.center;
            }

            Bounds group = new Bounds(spots[0], sizes[0]);
            for (int i = 1; i < spots.Length; i++)
                group.Encapsulate(new Bounds(spots[i], sizes[i]));

            Pose pose = StationLayout.ViewPoint(
                group, station.Outward, Module01Stations.ViewDistance, Module01Stations.EyeHeight,
                Module01Stations.HudClearance);
            PlaceAnchor(zone.ZoneId, pose);
            ClearCorridor(zone.ZoneId, group, station.Outward, pose.position, surfaceGo);

            Debug.Log(
                $"[Module01] '{zone.ZoneId}' -> {station.SurfaceName}: {plan.Count} elementos sobre " +
                $"y={station.WorktopHeight:F3}, {forma}, {span:F2} m de los {room:F2} que da el " +
                $"mueble, hueco {gap:F2}, ancla en {pose.position:F2}.");
            return true;
        }

        /// <summary>
        /// Reparte en <b>dos filas</b>: lo plano delante, hacia el aprendiz, y lo alto detras.
        /// <para>
        /// Es lo que permite meter ocho elementos en una mesa de 2.62 m. El orden importa y es
        /// este y no el contrario: un elemento plano delante de uno alto no lo tapa —la vista y
        /// el rayo del clic le pasan por encima—, mientras que el chaleco, que mide 60 cm,
        /// esconderia detras de si un pantalon doblado de 5 cm.
        /// </para>
        /// <para>
        /// Cada fila se coloca con <see cref="StationLayout.Arrange"/> sobre la misma tapa
        /// corrida en profundidad, de modo que la aritmetica de centrado y de huecos es la misma
        /// y no hay una segunda version que se pueda desincronizar.
        /// </para>
        /// </summary>
        private static Vector3[] ArrangeTwoRanks(
            Bounds surface, Station station, List<Vector3> sizes,
            out float gap, out float span, out int flatCount)
        {
            List<int> planos = new List<int>();
            List<int> altos = new List<int>();
            for (int i = 0; i < sizes.Count; i++)
            {
                if (StationLayout.IsFlat(sizes[i]))
                    planos.Add(i);
                else
                    altos.Add(i);
            }

            flatCount = planos.Count;

            float fondo = station.Axis == StationAxis.X ? surface.size.z : surface.size.x;
            float corrimiento = fondo * StationLayout.RankOffset;

            Bounds detras = StationLayout.Rank(surface, station.Outward, -corrimiento);
            Bounds delante = StationLayout.Rank(surface, station.Outward, corrimiento);

            Vector3[] spots = new Vector3[sizes.Count];
            gap = Module01Stations.Gap;
            span = 0f;

            // La fila de atras manda en el hueco y en el ancho, que es la que va justa.
            foreach ((List<int> fila, Bounds tapa) in new[] { (altos, detras), (planos, delante) })
            {
                if (fila.Count == 0)
                    continue;

                List<Vector3> tam = fila.Select(i => sizes[i]).ToList();
                float g = StationLayout.FittingGap(tapa, station.Axis, tam, Module01Stations.Gap);
                float s = StationLayout.Span(tam, station.Axis, g);

                Vector3[] sitios = StationLayout.Arrange(
                    tapa, station.WorktopHeight, station.Axis, tam, g);

                for (int k = 0; k < fila.Count; k++)
                    spots[fila[k]] = sitios[k];

                if (s > span) { span = s; gap = g; }
            }

            return spots;
        }

        /// <summary>Crea o recoloca <c>anchor_&lt;zona&gt;</c> con la pose derivada del grupo.</summary>
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

        /// <summary>
        /// Aparta lo que el paquete de arte planta entre el aprendiz y su estación.
        /// <para>
        /// No se busca por nombre. El v6 ponía ahí tres maniquíes; el v7 los cambió por seis
        /// prendas de pie. Una lista escrita a mano habría que reescribirla en cada entrega, y
        /// entre entrega y entrega la estación queda inservible sin que nada avise.
        /// </para>
        /// <para>
        /// <b>Y no se puede comprobar con física.</b> El arte llega sin colliders, así que un
        /// <c>Raycast</c> contesta «despejado» y miente; y un solo rayo al centro tampoco vale
        /// —medido, la línea de visión pasaba a 4 cm de una bufanda y daba por buena una
        /// estación que en la captura no se veía—. Se compara contra la caja de cada renderer,
        /// que es lo que de verdad ocupa sitio.
        /// </para>
        /// <para>
        /// Se aparta, no se apaga: es decorado del equipo de arte, no basura.
        /// </para>
        /// </summary>
        private static void ClearCorridor(
            string zoneId, Bounds group, Vector3 outward, Vector3 eye, GameObject surface)
        {
            // El pasillo va de la cara del grupo que mira al aprendiz hasta sus ojos, y NO
            // incluye la rebanada que ocupa el propio grupo.
            //
            // Esta distincion es todo. La primera version cogia la caja que envuelve al grupo y
            // al ojo, y con eso aparto CATORCE herramientas del banco de la derecha -regla,
            // martillo, machete, segueta- que no tapaban nada: estaban AL LADO de la estacion,
            // sobre el mismo banco, no ENTRE el aprendiz y ella. Dos acabaron fuera del garaje.
            // Vaciar el banco de herramientas es justo lo contrario de "usar el entorno".
            bool alongX = Mathf.Abs(outward.x) > Mathf.Abs(outward.z);
            float sign = alongX ? Mathf.Sign(outward.x) : Mathf.Sign(outward.z);

            float groupNear = alongX
                ? (sign > 0 ? group.max.x : group.min.x)
                : (sign > 0 ? group.max.z : group.min.z);
            float eyeAt = alongX ? eye.x : eye.z;

            float from = Mathf.Min(groupNear, eyeAt);
            float to = Mathf.Max(groupNear, eyeAt);

            // A lo ancho, solo lo que se cruza con el grupo mas un margen.
            float sideMin = (alongX ? group.min.z : group.min.x) - CorridorMargin;
            float sideMax = (alongX ? group.max.z : group.max.x) + CorridorMargin;

            // El pasillo arranca un palmo POR DELANTE de la estacion, no en su borde. Sin esto
            // se marca lo que descansa en la misma tapa rozando el frente -un candado, una
            // regla, un soporte-, que esta al lado y no en medio.
            const float clearance = .25f;
            if (sign > 0) from = Mathf.Max(from, groupNear + clearance);
            else to = Mathf.Min(to, groupNear - clearance);

            Renderer[] todos = Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            List<Renderer> estorban = todos.Where(r =>
            {
                if (r.GetComponentInParent<WorldObjectTaskEntity>() != null) return false;
                if (r.GetComponentInParent<Canvas>() != null) return false;
                // El mueble de la propia estacion toca el pasillo por definicion: no es estorbo.
                if (surface != null && r.transform.IsChildOf(surface.transform)) return false;

                Bounds b = r.bounds;
                if (b.size.magnitude > 8f) return false;        // ni suelo, ni muros, ni techo
                if (b.min.y > EyeBand) return false;            // las luces del techo se quedan
                if (b.max.y < group.min.y - .25f) return false; // lo que esta por debajo no tapa

                // Se mide el CENTRO, no el roce. Un objeto cuyo centro cae en el pasillo esta
                // de verdad en medio; uno que solo lo roza con una esquina, no. Comparando
                // cajas se marcaban catorce herramientas del banco que no tapaban nada.
                float at = alongX ? b.center.x : b.center.z;
                if (at < from || at > to) return false;

                float side = alongX ? b.center.z : b.center.x;
                return side > sideMin && side < sideMax;
            }).ToList();

            if (estorban.Count == 0)
                return;

            List<Renderer> ocupado = todos.Where(r =>
                !estorban.Contains(r) && r.bounds.size.magnitude <= 8f
                && r.GetComponentInParent<Canvas>() == null).ToList();

            // Se apartan a lo largo de la pared de la estación, alejándose del grupo: es el
            // sitio que con más probabilidad sigue siendo decorado creíble.
            Vector3 along = new Vector3(-outward.z, 0f, outward.x);
            List<Bounds> puestos = new List<Bounds>();
            int moved = 0;

            // Se aparta el OBJETO, no cada malla suelta. Un prop con la malla repartida en
            // hijos —'luz grontal' la tiene en 'model_0:Mesh' y 'soporte'— se partía en dos:
            // un hijo se iba al otro extremo del garaje y el otro se quedaba, y el modelo
            // acababa midiendo 8,5 m. Agrupar por raíz y mover una vez lo evita.
            Dictionary<Transform, Bounds> props = new Dictionary<Transform, Bounds>();
            foreach (Renderer r in estorban)
            {
                Transform raiz = PropRoot(r.transform, surface);
                props[raiz] = props.TryGetValue(raiz, out Bounds acum)
                    ? Encapsulated(acum, r.bounds)
                    : r.bounds;
            }

            foreach (KeyValuePair<Transform, Bounds> prop in props)
            {
                Transform raiz = prop.Key;
                Bounds caja = WorldBounds(raiz) ?? prop.Value;

                if (!TryFindSpot(caja, group.center, along, ocupado, puestos, out Vector3 destino))
                {
                    Debug.LogWarning(
                        $"[Module01] '{raiz.name}' tapa la estación '{zoneId}' y no he encontrado sitio " +
                        "libre donde apartarlo. Se queda donde está: hay que moverlo a mano.");
                    continue;
                }

                Undo.RecordObject(raiz, "Apartar de la estacion");
                raiz.position += destino - caja.center;
                puestos.Add(new Bounds(destino, caja.size));
                moved++;

                Debug.LogWarning(
                    $"[Module01] '{raiz.name}' estaba entre el aprendiz y la estación '{zoneId}'. " +
                    $"Apartado a {destino:F2}. Conviene que el equipo de arte no lo coloque ahí " +
                    "en la próxima entrega.");
            }

            if (moved > 0)
                Debug.Log($"[Module01] {moved} objetos de decorado apartados del frente de '{zoneId}'.");
        }

        /// <summary>
        /// Busca un hueco libre alejándose del grupo a lo largo de la pared.
        /// <para>
        /// Hace falta buscar y no calcular: la primera versión dejaba las prendas en una fila a
        /// intervalos fijos y cuatro de las seis acabaron metidas dentro de la estantería. Un
        /// sitio «libre» hay que comprobarlo contra lo que ya hay y contra lo que acabamos de
        /// poner, o se apilan entre ellas.
        /// </para>
        /// </summary>
        private static bool TryFindSpot(Bounds piece, Vector3 from, Vector3 along,
            List<Renderer> ocupado, List<Bounds> puestos, out Vector3 spot)
        {
            const float step = .25f, margin = .08f, reach = 9f;
            Bounds garaje = GarageFloor();

            for (float d = 1f; d <= reach; d += step)
            {
                foreach (int sign in new[] { 1, -1 })
                {
                    Vector3 candidate = from + along * (d * sign);
                    spot = new Vector3(candidate.x, piece.center.y, candidate.z);
                    Bounds probe = new Bounds(spot, piece.size + Vector3.one * margin);

                    // Dentro del garaje. Sin esto el buscador empuja hacia fuera hasta que no
                    // choca con nada, que es lo que hay al otro lado de la pared: medido, una
                    // regla acabo en z=7.01 y una tuerca en z=-4.99, en la calle.
                    if (!garaje.Contains(new Vector3(spot.x, garaje.center.y, spot.z))) continue;
                    if (ocupado.Any(r => r.bounds.Intersects(probe))) continue;
                    if (puestos.Any(b => b.Intersects(probe))) continue;
                    return true;
                }
            }

            spot = default;
            return false;
        }

        /// <summary>Cara superior del suelo del garaje. Donde se apoya lo que va de pie.</summary>
        private static float GarageFloorTop()
        {
            GameObject floor = Find("piso");
            if (floor != null && TryBounds(floor, out Bounds b))
                return b.max.y;
            Debug.LogWarning("[Module01] No encuentro 'piso'; lo que vaya de pie se apoya en y=0.");
            return 0f;
        }

        /// <summary>La planta del garaje, para no apartar nada a la calle.</summary>
        private static Bounds GarageFloor()
        {
            GameObject floor = Find("piso");
            if (floor == null || !TryBounds(floor, out Bounds b))
                return new Bounds(Vector3.zero, new Vector3(1000f, 1000f, 1000f));
            // Un palmo hacia dentro: pegado a la pared tampoco vale.
            b.Expand(new Vector3(-.60f, 0f, -.60f));
            return b;
        }

        private static GameObject Find(string name) => Object
            .FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(t => t.name == name)?.gameObject;

        private static bool TryBounds(GameObject go, out Bounds box)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
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
