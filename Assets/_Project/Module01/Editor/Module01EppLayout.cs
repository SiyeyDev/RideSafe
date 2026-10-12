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
        /// Aparta de delante del mostrador los maniquíes del paquete de arte.
        /// <para>
        /// El garaje v6 trae <c>Ch20_nonPBR (2)</c> plantado entre el aprendiz y la tapa,
        /// ocupando x de −1.47 a 0.35: tapa las gafas y el calzado. No se detecta con un rayo
        /// —en esta escena <b>ni los maniquíes ni los armarios tienen collider</b>, así que
        /// toda comprobación de oclusión por física sale «despejado» y miente—; se vio en una
        /// captura.
        /// </para>
        /// <para>
        /// Se aparta en vez de apagarse porque es decorado del equipo de arte, no basura. Y se
        /// hace aquí, y no a mano en la escena, para que vuelva a apartarse solo cuando llegue
        /// el garaje v7 y lo coloque otra vez en medio.
        /// </para>
        /// </summary>
        private static void ClearCounterFrontage(float eppMinX, float eppMaxX, float counterZ)
        {
            // Franja que hay que dejar libre: el ancho de los EPP y el metro y medio por
            // delante, que es donde se pone el aprendiz a mirarlos.
            const float sideMargin = .30f;
            const float frontDepth = 1.8f;

            foreach (GameObject go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!go.name.StartsWith("Ch20_nonPBR") || go.transform.parent != null)
                    continue;

                Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                    continue;
                Bounds b = renderers[0].bounds;
                foreach (Renderer r in renderers)
                    b.Encapsulate(r.bounds);

                bool inX = b.max.x > eppMinX - sideMargin && b.min.x < eppMaxX + sideMargin;
                bool inZ = b.max.z > counterZ - .4f && b.min.z < counterZ + frontDepth;
                if (!inX || !inZ)
                    continue;

                // Al extremo libre del mostrador, por el lado que quede mas lejos del grupo.
                float half = b.size.x * .5f;
                float targetX = eppMinX - sideMargin - half - .5f;
                Undo.RecordObject(go.transform, "Apartar maniqui");
                go.transform.position += new Vector3(targetX - b.center.x, 0f, 0f);

                Debug.LogWarning(
                    $"[Module01] '{go.name}' estaba delante del mostrador (x {b.min.x:F2}..{b.max.x:F2}) " +
                    $"y tapaba los EPP. Apartado a x={targetX:F2}. " +
                    "Conviene que el equipo de arte no lo coloque ahi en la proxima entrega.");
            }
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
