using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Agrupa los EPP del mostrador por zona y los apoya exactamente sobre la tapa.
    /// <para>
    /// El garaje v6 ya los trae puestos sobre el mostrador y a su altura, que es muchísimo
    /// mejor que la fila flotante que montaba el presentador. Le faltan dos cosas:
    /// </para>
    /// <list type="bullet">
    /// <item><b>Están desparramados.</b> Medido: la zona <c>head</c> ocupa 4.99 m y la
    /// <c>clothing</c> 3.30 m, porque el casco bueno y las botas quedaron al otro extremo del
    /// mostrador. Desde un único punto de vista sus elementos se ven diminutos, y alejar la
    /// cámara no lo arregla: hay que juntarlos.</item>
    /// <item><b>Tres están hundidos en la tapa.</b> <c>casco 1</c> tiene la base 16 cm por
    /// debajo, y el chaleco y el casco bueno unos 2 cm. Eso es lo que queda del viejo «los
    /// objetos no se apoyan en la mesa».</item>
    /// </list>
    /// <para>
    /// <b>La altura de la tapa no se escribe a mano:</b> sale de la mediana de las bases que
    /// puso el equipo de arte. La mediana y no la media, porque con un objeto 16 cm hundido la
    /// media arrastraría a los seis que estaban bien. Así, si en el garaje v7 el mostrador
    /// cambia de alto, esto se adapta solo.
    /// </para>
    /// <para>
    /// Los armarios del mostrador <b>no tienen collider</b> —los siete rayos hacia abajo caen
    /// al suelo del garaje—, así que la altura no se puede sondear con física. Tampoco conviene
    /// ponerles uno de caja: el armario mide 2.73 m de alto y su caja envolvería los objetos de
    /// la tapa, de modo que el clic chocaría con el armario antes de llegar al EPP.
    /// </para>
    /// </summary>
    public static class Module01EppLayout
    {
        private const string CatalogAsset = "Assets/_Project/Module01/Data/SO_Module01Catalog.asset";

        /// <summary>Hueco entre dos elementos de la misma zona, en metros.</summary>
        private const float GapWithinZone = .34f;

        /// <summary>Hueco entre zonas. Mayor, para que se lean como grupos distintos.</summary>
        private const float GapBetweenZones = .85f;

        [MenuItem("RideSafe/Módulo 1/Agrupar los EPP del mostrador por zona")]
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

            // Orden del recorrido: zonas del catálogo, y dentro de cada una el orden del dato.
            List<(string Zone, WorldObjectTaskEntity Entity, Bounds Box)> plan =
                new List<(string, WorldObjectTaskEntity, Bounds)>();

            foreach (ZoneSO zone in catalog.Zones)
            {
                if (zone == null)
                    continue;
                foreach (SafetyItemSO item in zone.Items)
                {
                    if (item == null || !byId.TryGetValue(item.ItemId, out WorldObjectTaskEntity entity))
                        continue;
                    if (!TryBounds(entity, out Bounds box))
                    {
                        Debug.LogWarning($"[Module01] '{item.ItemId}' no tiene renderer; no se recoloca.");
                        continue;
                    }
                    plan.Add((zone.ZoneId, entity, box));
                }
            }

            if (plan.Count == 0)
            {
                Debug.LogWarning("[Module01] No hay EPP que recolocar en la escena.");
                return;
            }

            float top = WorktopHeight(plan.Select(p => p.Box.min.y));
            float depth = Median(plan.Select(p => p.Box.center.z).ToList());

            // Ancho total: la suma de los anchos reales más los huecos que toquen.
            float total = 0f;
            for (int i = 0; i < plan.Count; i++)
            {
                total += plan[i].Box.size.x;
                if (i == plan.Count - 1)
                    continue;
                total += plan[i].Zone == plan[i + 1].Zone ? GapWithinZone : GapBetweenZones;
            }

            // Centrado en el tramo de mostrador que ya ocupaban.
            float center = (plan.Min(p => p.Box.min.x) + plan.Max(p => p.Box.max.x)) * .5f;
            float cursor = center - total * .5f;

            Debug.Log(
                $"[Module01] Tapa del mostrador a y={top:F3} (mediana de las bases del arte), " +
                $"profundidad z={depth:F2}, ancho a repartir {total:F2} m centrado en x={center:F2}.");

            foreach ((string zoneId, WorldObjectTaskEntity entity, Bounds box) in plan)
            {
                float targetX = cursor + box.size.x * .5f;
                Vector3 target = new Vector3(targetX, top + box.size.y * .5f, depth);

                // Se mueve el transform por la diferencia entre el centro visible y el destino,
                // no se le asigna la posición: el pivote de un FBX casi nunca está en el centro
                // de la malla, y asignar posición directamente lo deja descolocado y hundido.
                Vector3 delta = target - box.center;
                Undo.RecordObject(entity.transform, "Agrupar EPP");
                entity.transform.position += delta;

                cursor = targetX + box.size.x * .5f;
                cursor += SameZoneNext(plan, entity) ? GapWithinZone : GapBetweenZones;

                Debug.Log(
                    $"[Module01] {entity.ItemId} ({zoneId}): base {box.min.y:F3} -> {top:F3}, " +
                    $"x {box.center.x:F2} -> {targetX:F2}");
            }

            ClearCounterFrontage(
                plan.Min(p => p.Box.min.x), plan.Max(p => p.Box.max.x), depth);

            // El collider se reajusta porque el objeto se ha movido y el suyo es local.
            Module01SceneEntities.Apply();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[Module01] {plan.Count} EPP agrupados por zona y apoyados en la tapa.");
        }

        /// <summary>
        /// Aparta lo que el paquete de arte planta entre el aprendiz y la tapa del mostrador.
        /// <para>
        /// No se busca por nombre. El v6 ponía ahí tres maniquíes <c>Ch20_nonPBR</c>; el v7 se
        /// los llevó y puso en su lugar <b>seis prendas de pie</b> —dos bufandas, un tapabocas
        /// y tres pantalones— ocupando x de −0.01 a 1.85 justo delante de la zona de ropa. Una
        /// lista de nombres habría que reescribirla en cada entrega, y entre entrega y entrega
        /// la estación queda inservible sin que nada avise.
        /// </para>
        /// <para>
        /// <b>Esto no se detecta con física ni con un rayo.</b> En esta escena el arte llega sin
        /// colliders, así que un <c>Raycast</c> contesta «despejado» y miente. Y un solo rayo al
        /// centro del objeto tampoco vale: medido, la línea de visión pasaba a 4 cm de la
        /// bufanda y daba por buena una estación que en la captura no se veía. Se mide contra la
        /// caja de cada renderer, que es lo que de verdad ocupa sitio.
        /// </para>
        /// <para>
        /// Se aparta, no se apaga: es decorado del equipo de arte, no basura. Van a una fila
        /// contra la pared de al lado, fuera del encuadre de las tres estaciones.
        /// </para>
        /// </summary>
        private static void ClearCounterFrontage(float eppMinX, float eppMaxX, float counterZ)
        {
            // La franja a dejar libre: el ancho de los EPP y lo que hay por delante hasta donde
            // se planta el aprendiz. Empieza por delante del vuelo de la tapa para no coger el
            // propio mueble, que tambien esta "en la franja".
            const float sideMargin = .30f, overhang = .35f, frontDepth = 2.60f;
            // Solo estorba lo que cae en la banda que el ojo recorre. Asi las luces del techo
            // -que tambien estan por delante del mostrador- se quedan donde estan.
            const float eyeBand = 1.75f;

            // A que lado se aparta y hasta donde se busca sitio.
            float sideX = eppMinX - 2.00f;
            Debug.Log($"[Module01] despeje: EPP en x {eppMinX:F2}..{eppMaxX:F2}; franja a dejar libre " +
                $"z {counterZ + overhang:F2}..{counterZ + frontDepth:F2}; se aparta a x={sideX:F2}.");

            Renderer[] todos = Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            List<Renderer> estorban = todos.Where(r =>
                r.GetComponentInParent<WorldObjectTaskEntity>() == null
                && r.GetComponentInParent<Canvas>() == null
                && r.bounds.size.magnitude <= 8f
                && r.bounds.min.y <= eyeBand
                && r.bounds.min.z > counterZ + overhang
                && r.bounds.max.z < counterZ + frontDepth
                && r.bounds.max.x > eppMinX - sideMargin
                && r.bounds.min.x < eppMaxX + sideMargin).ToList();

            if (estorban.Count == 0)
                return;

            // Contra lo que hay que no chocar: todo lo demas que ocupa sitio. Las mallas enormes
            // -suelo, muros, techo- se quedan fuera porque cualquier caja las toca siempre.
            List<Renderer> ocupado = todos.Where(r =>
                !estorban.Contains(r) && r.bounds.size.magnitude <= 8f
                && r.GetComponentInParent<Canvas>() == null).ToList();

            List<Bounds> puestos = new List<Bounds>();
            int moved = 0;

            foreach (Renderer r in estorban)
            {
                Bounds b = r.bounds;
                if (!TryFindSpot(b, sideX, counterZ, ocupado, puestos, out Vector3 destino))
                {
                    Debug.LogWarning(
                        $"[Module01] '{r.name}' esta delante del mostrador y tapa los EPP, pero no he " +
                        $"encontrado sitio libre en la pared de x={sideX:F2}. Se queda donde esta: " +
                        "hay que moverlo a mano o liberar esa pared.");
                    continue;
                }

                Undo.RecordObject(r.transform, "Apartar del mostrador");
                r.transform.position += destino - b.center;
                puestos.Add(new Bounds(destino, b.size));
                moved++;

                Debug.LogWarning(
                    $"[Module01] '{r.name}' estaba delante del mostrador (x {b.min.x:F2}..{b.max.x:F2}, " +
                    $"z {b.min.z:F2}..{b.max.z:F2}) y tapaba los EPP. Apartado a " +
                    $"x={destino.x:F2}, z={destino.z:F2}. " +
                    "Conviene que el equipo de arte no lo coloque ahi en la proxima entrega.");
            }

            if (moved > 0)
                Debug.Log($"[Module01] {moved} objetos de decorado apartados del frente del mostrador.");
        }

        /// <summary>
        /// Busca un hueco libre en la pared de al lado, recorriendola en profundidad.
        /// <para>
        /// Hace falta buscar y no calcular: la primera version dejaba las prendas en una fila a
        /// intervalos fijos y cuatro de las seis acabaron metidas dentro de la estanteria. Un
        /// sitio «libre» hay que comprobarlo contra lo que ya hay y contra lo que acabamos de
        /// poner, o se apilan entre ellas.
        /// </para>
        /// </summary>
        private static bool TryFindSpot(Bounds piece, float sideX, float counterZ,
            List<Renderer> ocupado, List<Bounds> puestos, out Vector3 spot)
        {
            const float step = .25f, margin = .08f;
            float from = counterZ + .60f, to = counterZ + 9.00f;

            for (float z = from; z <= to; z += step)
            {
                spot = new Vector3(sideX, piece.center.y, z);
                Bounds probe = new Bounds(spot, piece.size + Vector3.one * margin);

                if (ocupado.Any(r => r.bounds.Intersects(probe))) continue;
                if (puestos.Any(b => b.Intersects(probe))) continue;
                return true;
            }

            spot = default;
            return false;
        }

        private static bool SameZoneNext(
            List<(string Zone, WorldObjectTaskEntity Entity, Bounds Box)> plan, WorldObjectTaskEntity current)
        {
            int i = plan.FindIndex(p => p.Entity == current);
            return i >= 0 && i + 1 < plan.Count && plan[i].Zone == plan[i + 1].Zone;
        }

        /// <summary>
        /// Altura de la tapa: la mediana de las bases. Robusta al objeto hundido, que a la
        /// media se la llevaría por delante.
        /// </summary>
        private static float WorktopHeight(IEnumerable<float> bases) => Median(bases.ToList());

        private static float Median(List<float> values)
        {
            values.Sort();
            int n = values.Count;
            return n % 2 == 1 ? values[n / 2] : (values[n / 2 - 1] + values[n / 2]) * .5f;
        }

        private static bool TryBounds(Component c, out Bounds box)
        {
            Renderer[] renderers = c.GetComponentsInChildren<Renderer>(true);
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
