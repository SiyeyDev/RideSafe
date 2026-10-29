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
    /// <b>Esto SÍ mueve los EPP.</b> El comentario de aquí decía lo contrario —«no se mueve
    /// nada suyo»— y llevaba obsoleto desde que entró <see cref="Arrange"/>, que recoloca
    /// todo lo de la zona. Esa mentira costó cara el 2026-10-29: la PO había repartido los
    /// EPP a mano y se perdió tres veces seguidas.
    /// </para>
    /// <para>
    /// Lo único que NO toca es lo que una persona haya movido: <see cref="Module01LayoutStamp"/>
    /// apunta dónde dejó el layout cada cosa y, si al volver no está ahí, la respeta.
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

        /// <summary>
        /// Lo mínimo que se deja entre dos vecinos de una fila. Por debajo de esto se tocan y la
        /// mesa deja de leerse como un expositor.
        /// </summary>
        private const float MinimoEntreVecinos = 0.04f;

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
            ClearDecorFromTops(zones);
            foreach (ZoneSO zone in zones)
                Arrange(zone, Mine(zone, byId));
            Module01LayoutStamp.Guardar();
            Physics.SyncTransforms();

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

        /// <summary>
        /// Los elementos de la zona que este reparto puede mover: los de su catálogo <b>menos</b>
        /// los que alguien haya colocado a mano desde la última corrida.
        /// <para>
        /// Antes devolvía la zona entera sin filtro y por eso el reparto se llevaba por delante
        /// cualquier colocación manual (PO, 2026-10-29). Quien lo mueva en el editor lo protege
        /// sin tener que marcar nada.
        /// </para>
        /// </summary>
        /// <summary>
        /// Mira las mesas y dice qué está mal, <b>sin mover ni un milímetro</b>.
        /// <para>
        /// Es la herramienta para después de colocar a mano: el reparto automático responde a
        /// reglas que no siempre son las que quiere la PO, así que lo que hace falta no es
        /// recolocar, es saber si lo colocado se sostiene. Comprueba cuatro cosas, que son las
        /// que se han roto de verdad: piezas que se tapan entre sí, piezas que flotan o se
        /// hunden respecto al nivel del mueble, piezas fuera del mueble de su parada, y cuánto
        /// queda libre de cada nivel.
        /// </para>
        /// </summary>
        [MenuItem("RideSafe/Módulo 1/Revisar las mesas de EPP (no mueve nada)")]
        public static void CheckOpenScene()
        {
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

            int problemas = 0;
            foreach (ZoneSO zone in catalog.Zones.Where(z => z != null))
            {
                var tops = Tops(zone);
                if (tops.Count == 0)
                    continue;

                // --- lo que hay en esta parada, con su caja de clic ---------------------
                var puestos = new List<(WorldObjectTaskEntity E, Bounds Caja)>();
                foreach (SafetyItemSO item in zone.Items)
                {
                    if (item == null || !byId.TryGetValue(item.ItemId, out WorldObjectTaskEntity e))
                        continue;
                    if (!e.gameObject.activeInHierarchy)
                        continue;
                    if (!TryBounds(e.gameObject, out Bounds visible))
                        continue;
                    puestos.Add((e, Clickable(e, visible)));
                }
                if (puestos.Count == 0)
                    continue;

                // --- 1. piezas que se pisan entre si ------------------------------------
                for (int a = 0; a < puestos.Count; a++)
                for (int b = a + 1; b < puestos.Count; b++)
                {
                    if (!puestos[a].Caja.Intersects(puestos[b].Caja))
                        continue;
                    Bounds ca = puestos[a].Caja, cb = puestos[b].Caja;
                    float solapeX = Mathf.Min(ca.max.x, cb.max.x) - Mathf.Max(ca.min.x, cb.min.x);
                    float solapeZ = Mathf.Min(ca.max.z, cb.max.z) - Mathf.Max(ca.min.z, cb.min.z);
                    Debug.LogWarning($"[Revisión] {zone.ZoneId}: '{puestos[a].E.ItemId}' y "
                                   + $"'{puestos[b].E.ItemId}' se pisan ({solapeX:F2} × {solapeZ:F2} m).",
                                     puestos[a].E.gameObject);
                    problemas++;
                }

                // --- 2. flotando o hundido respecto al nivel mas cercano ----------------
                foreach (var pz in puestos)
                {
                    float mejor = float.MaxValue;
                    string nivel = null;
                    foreach (var t in tops)
                    {
                        // solo cuenta el nivel bajo cuya planta cae la pieza
                        Vector3 c = pz.Caja.center;
                        if (c.x < t.Box.min.x - .05f || c.x > t.Box.max.x + .05f) continue;
                        if (c.z < t.Box.min.z - .05f || c.z > t.Box.max.z + .05f) continue;
                        float d = pz.Caja.min.y - t.Surface.WorktopHeight;
                        if (Mathf.Abs(d) >= Mathf.Abs(mejor)) continue;
                        mejor = d;
                        nivel = t.Surface.Name + " @" + t.Surface.WorktopHeight.ToString("0.00");
                    }
                    if (nivel == null)
                    {
                        Debug.LogWarning($"[Revisión] {zone.ZoneId}: '{pz.E.ItemId}' no cae sobre ningún "
                                       + "nivel declarado de su parada.", pz.E.gameObject);
                        problemas++;
                    }
                    else if (Mathf.Abs(mejor) > .02f)
                    {
                        Debug.LogWarning($"[Revisión] {zone.ZoneId}: '{pz.E.ItemId}' "
                                       + (mejor > 0f ? "flota " : "se hunde ")
                                       + $"{Mathf.Abs(mejor):F2} m sobre {nivel}.", pz.E.gameObject);
                        problemas++;
                    }
                }

                // --- 3. cuanto se usa de cada nivel -------------------------------------
                foreach (var t in tops)
                {
                    bool depthAlongX = Mathf.Abs(Outward(t.Box, GarageFloor()).x) > .5f;
                    float largo = Largo(t.Box, depthAlongX) - 2f * StationLayout.EdgeInset;
                    float ocupado = 0f;
                    int cuantos = 0;
                    foreach (var pz in puestos)
                    {
                        if (Mathf.Abs(pz.Caja.min.y - t.Surface.WorktopHeight) > .05f) continue;
                        Vector3 c = pz.Caja.center;
                        if (c.x < t.Box.min.x - .05f || c.x > t.Box.max.x + .05f) continue;
                        if (c.z < t.Box.min.z - .05f || c.z > t.Box.max.z + .05f) continue;
                        ocupado += depthAlongX ? pz.Caja.size.z : pz.Caja.size.x;
                        cuantos++;
                    }
                    float uso = largo > 0f ? 100f * ocupado / largo : 0f;
                    Debug.Log($"[Revisión] {zone.ZoneId} · {t.Surface.Name} @"
                            + $"{t.Surface.WorktopHeight:F2}: {cuantos} elementos, {ocupado:F2} m "
                            + $"de {largo:F2} ({uso:F0} % del largo).");
                    if (uso > 100f)
                        problemas++;
                }
            }

            Debug.Log(problemas == 0
                ? "[Revisión] Las mesas están bien: nada se pisa, nada flota y nada se sale."
                : $"[Revisión] {problemas} cosas que mirar. Ninguna se ha tocado.");
        }

        private static List<WorldObjectTaskEntity> Mine(ZoneSO zone, Dictionary<string, WorldObjectTaskEntity> byId)
        {
            var mios = new List<WorldObjectTaskEntity>();
            var respetados = new List<string>();
            foreach (SafetyItemSO item in zone.Items)
            {
                if (item == null || !byId.TryGetValue(item.ItemId, out WorldObjectTaskEntity e))
                    continue;
                if (Module01LayoutStamp.LoMovioUnaPersona(item.ItemId, e.transform.position))
                {
                    respetados.Add(item.ItemId);
                    continue;
                }
                mios.Add(e);
            }

            if (respetados.Count > 0)
                Debug.Log($"[Module01] En '{zone.ZoneId}' se respeta lo colocado a mano: " +
                          string.Join(", ", respetados) + ".");
            return mios;
        }

        /// <summary>
        /// Apaga el decorado que descansa sobre la tapa de una parada: sobre un mueble que el
        /// aprendiz recorre solo queda lo que puede elegir (PO, 2026-10-29).
        /// <para>
        /// No es solo estética. Cada pieza de adorno sobre la tapa es un obstáculo para el rayo
        /// del clic: el aprendiz apunta a un EPP, la bomba de aire se cruza y el clic se pierde.
        /// En el banco había <b>diecinueve</b> piezas de un kit de parches y en la mesa del centro
        /// cinco herramientas, todas a la altura de lo que hay que pulsar.
        /// </para>
        /// <para>
        /// Es una regla, no una lista: lo que no lleva <c>WorldObjectTaskEntity</c> y está apoyado
        /// en una tapa de parada se apaga. Así una entrega nueva del artista no vuelve a llenar
        /// las mesas sin que nadie se entere. Lo que cuelga del panel o está sobre un mueble que
        /// no es parada no se toca: el garaje tiene que seguir pareciendo un garaje.
        /// </para>
        /// </summary>
        private static void ClearDecorFromTops(List<ZoneSO> zones)
        {
            var apagados = new List<string>();
            foreach (ZoneSO zone in zones)
            {
                if (!Module01Stations.TryGet(zone.ZoneId, out Station station))
                    continue;
                foreach (Surface s in station.Surfaces)
                {
                    if (!TryTop(zone.ZoneId, s, out GameObject mueble, out Bounds tapa))
                        continue;
                    foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    {
                        if (r == null || !r.enabled || r.transform.IsChildOf(mueble.transform))
                            continue;
                        if (r.GetComponentInParent<Canvas>() != null)
                            continue;
                        if (r.GetComponentInParent<WorldObjectTaskEntity>() != null)
                            continue;   // es un EPP: se queda
                        Bounds b = r.bounds;
                        if (b.size.magnitude > PropSizeLimit)
                            continue;
                        if (b.center.x < tapa.min.x || b.center.x > tapa.max.x) continue;
                        if (b.center.z < tapa.min.z || b.center.z > tapa.max.z) continue;
                        // Apoyado en la tapa, no colgado del panel ni guardado debajo.
                        if (b.min.y < s.WorktopHeight - .06f || b.min.y > s.WorktopHeight + .25f)
                            continue;

                        GameObject quitar = Entero(r.gameObject, tapa);
                        if (!quitar.activeSelf)
                            continue;
                        Undo.RecordObject(quitar, "Despejar la tapa");
                        quitar.SetActive(false);
                        apagados.Add($"{quitar.name} ({s.Name})");
                    }
                }
            }
            if (apagados.Count > 0)
                Debug.Log($"[Module01] Decorado apagado sobre las tapas ({apagados.Count}): " +
                          string.Join(", ", apagados.Distinct()) + ".");
        }

        /// <summary>
        /// El objeto entero al que pertenece una pieza, sin pasarse: sube por los padres mientras
        /// el conjunto siga cabiendo sobre la tapa. Apagar el renderer suelto dejaba media bomba
        /// de aire a la vista; subir sin freno apagaría el garaje entero.
        /// </summary>
        private static GameObject Entero(GameObject pieza, Bounds tapa)
        {
            GameObject actual = pieza;
            for (Transform p = pieza.transform.parent; p != null; p = p.parent)
            {
                if (!TryBounds(p.gameObject, out Bounds b))
                    break;
                if (b.min.x < tapa.min.x - .25f || b.max.x > tapa.max.x + .25f) break;
                if (b.min.z < tapa.min.z - .25f || b.max.z > tapa.max.z + .25f) break;
                actual = p.gameObject;
            }
            return actual;
        }

        /// <summary>
        /// Coloca <b>todos</b> los EPP de una parada sobre sus muebles como un expositor: dos
        /// bandas de profundidad —la del fondo y la de delante—, y dentro de cada una, a
        /// intervalos iguales a lo largo del mueble.
        /// <para>
        /// <b>Por qué así y no buscando huecos.</b> El buscador colocaba uno a uno en «el primer
        /// hueco libre más cercano a lo que ya hay»: amontonaba en un extremo, dejaba medio
        /// mueble vacío, y lo que no cabía se quedaba donde estuviera —en el suelo o en el aire—.
        /// Repartir no es buscar: con el largo del mueble y la suma de anchos, el hueco entre
        /// vecinos es una resta.
        /// </para>
        /// <para>
        /// <b>Por qué ya no se solapan.</b> Las dos bandas ocupan tramos de profundidad
        /// <i>disjuntos</i>, y una pieza solo entra en la del fondo si su fondo cabe en ella.
        /// Dentro de cada banda van en fila, uno tras otro. El intento anterior repartía por
        /// altura sin mirar el fondo, y por eso puso la bufanda encima de los zapatos y las botas
        /// dentro del chaleco (PO, 2026-10-29).
        /// </para>
        /// <para>
        /// <b>El alto no es un número inventado</b>: es el hueco libre medido sobre cada banda.
        /// Sobre la mesa del centro, al fondo, solo hay 0,26 m porque tiene alacena encima; en
        /// las demás no hay techo, así que un pantalón de pie cabría. Ir a ojo con un tope fijo
        /// fue lo que dejó la alforja y el poncho fuera de toda mesa.
        /// </para>
        /// </summary>
        private static void Arrange(ZoneSO zone, List<WorldObjectTaskEntity> mine)
        {
            var tops = Tops(zone);
            if (tops.Count == 0 || mine.Count == 0)
                return;

            Bounds whole = tops[0].Box;
            foreach (var t in tops)
                whole.Encapsulate(t.Box);
            Vector3 outward = Outward(whole, GarageFloor());
            bool depthAlongX = Mathf.Abs(outward.x) > .5f;

            var items = new List<Pieza>();
            foreach (WorldObjectTaskEntity e in mine)
                if (Medir(e, depthAlongX, out Pieza pz))
                    items.Add(pz);
            if (items.Count == 0)
                return;

            // Reparto entre muebles proporcional al largo, para que un mostrador de dos cuerpos
            // se llene entero y no solo el primero.
            float largoTotal = tops.Sum(t => Largo(t.Box, depthAlongX) - 2f * StationLayout.EdgeInset);
            if (largoTotal <= 0f)
                return;

            // El reparto entre niveles va por ANCHO ACUMULADO, no por número: repartir «mitad y
            // mitad de los elementos» mandaba cinco prendas anchas a un cuerpo del mostrador y
            // cinco gafas al otro, y el primero reventaba (PO, 2026-10-29). Cada nivel se lleva
            // la porción de ancho que le toca por su largo, y como cada uno acomoda dos bandas,
            // puede con el doble de lo que mide.
            items.Sort((a, b) => b.W.CompareTo(a.W));   // lo ancho primero: necesita tramo seguido
            float anchoTotal = items.Sum(p => p.W);

            // El nivel más hondo va al final a propósito: es el que recoge lo que no cabe de
            // fondo en ningún otro, así que tiene que ser el último en elegir.
            var niveles = tops
                .Select((t, idx) => (Top: t, Idx: idx, Fondo: Fondo(t.Box, depthAlongX)))
                .OrderBy(n => n.Fondo)
                .ToList();

            var pendientes = new List<Pieza>(items);
            var sinSitio = new List<string>();
            for (int t = 0; t < niveles.Count && pendientes.Count > 0; t++)
            {
                var nivel = niveles[t];
                float largo = Largo(nivel.Top.Box, depthAlongX) - 2f * StationLayout.EdgeInset;
                float fondoUtil = nivel.Fondo - 2f * StationLayout.EdgeInset;
                bool ultimo = t == niveles.Count - 1;
                float cupo = anchoTotal * largo / largoTotal;

                var toca = new List<Pieza>();
                float llevo = 0f;
                for (int k = 0; k < pendientes.Count; k++)
                {
                    Pieza pz = pendientes[k];

                    // Lo que no cabe de fondo en este nivel se queda para otro. Sin esto, el
                    // estante de 0,38 m de la mesa de la entrada se llevaba mochilas de 0,30
                    // que le colgaban por delante.
                    if (!ultimo && pz.D > fondoUtil)
                        continue;

                    // `llevo + ancho <= cupo`, no `llevo < cupo`: mirar el cupo ANTES de sumar
                    // la pieza metía siempre una de más. En el mostrador de la ropa el cupo era
                    // 2,73 m, las dos chaquetas sumaban 2,68 y entraba el chaleco de 0,45 -3,13
                    // en un tramo de 2,59-, que es como acabaron tres prendas una encima de otra
                    // (PO, 2026-10-29). El `toca.Count == 0` evita el otro extremo: un nivel
                    // vacío porque la primera pieza ya no cabía en su cupo.
                    if (!ultimo && toca.Count > 0 && llevo + pz.W > cupo)
                        continue;

                    if (ultimo && pz.D > fondoUtil)
                        sinSitio.Add($"{pz.E.ItemId} ({pz.D:F2} m de fondo)");

                    toca.Add(pz);
                    llevo += pz.W;
                    pendientes.RemoveAt(k);
                    k--;
                }
                Mueble(nivel.Top, toca, outward, depthAlongX, zone.ZoneId);
            }

            if (sinSitio.Count > 0)
                Debug.LogWarning($"[Module01] En '{zone.ZoneId}' no hay ningún nivel bastante hondo para: " +
                                 string.Join(", ", sinSitio) + ". Sobresalen del borde.");
            foreach (Pieza pz in pendientes)
                Debug.LogWarning($"[Module01] En '{zone.ZoneId}' se quedó sin colocar '{pz.E.ItemId}'.");
        }

        /// <summary>Lo que mide un mueble hacia el fondo, que es su lado corto.</summary>
        private static float Fondo(Bounds box, bool depthAlongX) =>
            depthAlongX ? box.size.x : box.size.z;

        /// <summary>Lo que hace falta saber de un EPP para repartirlo.</summary>
        private readonly struct Pieza
        {
            public Pieza(WorldObjectTaskEntity e, float w, float d, float h, float baseY, Vector3 centro)
            { E = e; W = w; D = d; H = h; BaseY = baseY; Centro = centro; }

            public WorldObjectTaskEntity E { get; }

            /// <summary>Lo que mide a lo largo del mueble.</summary>
            public float W { get; }

            /// <summary>Lo que mide hacia el fondo.</summary>
            public float D { get; }

            public float H { get; }

            /// <summary>Altura de su base: con ella se posa sin flotar ni hundirse.</summary>
            public float BaseY { get; }

            /// <summary>Centro en planta de su caja de clic, que es lo que hay que mover.</summary>
            public Vector3 Centro { get; }
        }

        /// <summary>
        /// Mide un EPP <b>encendiéndolo</b>: <c>Renderer.bounds</c> de un objeto apagado devuelve
        /// lo que hubiera la última vez que se dibujó, o cero, y aquí llegan apagados. Medir sin
        /// encender fue lo que dejó las gafas deportivas a 1,69 m (PO, 2026-10-29).
        /// </summary>
        private static bool Medir(WorldObjectTaskEntity e, bool depthAlongX, out Pieza pz)
        {
            pz = default;
            bool estaba = e.gameObject.activeSelf;
            if (!estaba)
                e.gameObject.SetActive(true);
            bool ok = TryBounds(e.gameObject, out Bounds visible);
            Bounds clic = ok ? Clickable(e, visible) : default;
            if (!estaba)
                e.gameObject.SetActive(false);
            if (!ok || visible.size.x <= 0.0001f)
                return false;

            pz = new Pieza(e,
                depthAlongX ? clic.size.z : clic.size.x,
                depthAlongX ? clic.size.x : clic.size.z,
                clic.size.y, visible.min.y,
                new Vector3(clic.center.x, 0f, clic.center.z));
            return true;
        }

        private static float Largo(Bounds b, bool depthAlongX) => depthAlongX ? b.size.z : b.size.x;

        /// <summary>Reparte un puñado de EPP sobre un mueble, en dos bandas disjuntas.</summary>
        private static void Mueble((Surface Surface, GameObject Go, Bounds Box) top, List<Pieza> piezas,
            Vector3 outward, bool depthAlongX, string zoneId)
        {
            if (piezas.Count == 0)
                return;

            float fondoUtil = (depthAlongX ? top.Box.size.x : top.Box.size.z) - 2f * StationLayout.EdgeInset;

            // Las dos bandas no son iguales: la del fondo se lleva el 55 % porque ahí va lo más
            // hondo —un par de zapatos ocupa 30 cm— y delante basta con lo plano. Partir por la
            // mitad exacta mandaba a la fila larga cualquier cosa de 30 cm y esa fila se come el
            // mueble entero: con los pares completos el reparto se desbordó (PO, 2026-10-29).
            float bandaFondo = fondoUtil * .55f;
            float bandaFrente = fondoUtil - bandaFondo;
            float mediaBanda = bandaFondo;
            float techoFondo = Headroom(top, outward, depthAlongX, true);

            // Tres grupos, y el primero es el que faltaba: lo que es MÁS HONDO que una banda no
            // cabe en ninguna sin invadir la otra. El candado de guaya mide 0,63 de fondo y la
            // banda 0,29: puesto delante se metía en la de atrás y se comía al que hubiera
            // detrás —así acabó el paraguas dentro del candado (PO, 2026-10-29)—. Esos van en
            // una fila propia a todo el fondo, en su propio tramo del mueble, que queda fuera
            // del alcance de las otras dos.
            var hondos = new List<Pieza>();
            var fondo = new List<Pieza>();
            var frente = new List<Pieza>();
            foreach (Pieza pz in piezas.OrderByDescending(x => x.H))
            {
                if (pz.D > bandaFondo)
                {
                    hondos.Add(pz);        // no cabe ni en la banda más honda: fila propia
                    continue;
                }
                // Lo que no cabe delante tiene que ir detrás, quepa o no por altura: si no, se
                // sale del mueble. Y a igualdad, lo alto detrás, para que no tape a lo bajo.
                bool soloCabeDetras = pz.D > bandaFrente;
                bool fondoVaLleno = fondo.Sum(x => x.W) > frente.Sum(x => x.W);
                if (soloCabeDetras || (pz.H <= techoFondo && !fondoVaLleno))
                    fondo.Add(pz);
                else
                    frente.Add(pz);
            }

            float signo = depthAlongX ? outward.x : outward.z;
            float centroMueble = depthAlongX ? top.Box.center.x : top.Box.center.z;
            // Cada banda centrada en su propio tramo, que no se solapan por construcción.
            float centroFondo = centroMueble - signo * (fondoUtil - bandaFondo) * .5f;
            float centroFrente = centroMueble + signo * (fondoUtil - bandaFrente) * .5f;

            // El mueble se parte a lo largo: un tramo para los hondos y el resto para las dos
            // bandas, que comparten su tramo porque no se pisan entre ellas.
            float inicio = (depthAlongX ? top.Box.min.z : top.Box.min.x) + StationLayout.EdgeInset;
            float largo = Largo(top.Box, depthAlongX) - 2f * StationLayout.EdgeInset;
            float pideHondos = hondos.Sum(p => p.W);
            float pideBandas = Mathf.Max(fondo.Sum(p => p.W), frente.Sum(p => p.W));
            float tramoHondos = pideHondos + pideBandas <= 0.0001f
                ? 0f
                : largo * pideHondos / (pideHondos + pideBandas);

            Fila(top, hondos, depthAlongX, centroMueble, inicio, tramoHondos, zoneId);
            Fila(top, fondo, depthAlongX, centroFondo, inicio + tramoHondos, largo - tramoHondos, zoneId);
            Fila(top, frente, depthAlongX, centroFrente, inicio + tramoHondos, largo - tramoHondos, zoneId);
        }

        /// <summary>
        /// El hueco libre en alto sobre una banda de la tapa: la cota más baja de lo que haya
        /// encima —alacena, panel—. Sin nada encima, sin límite.
        /// </summary>
        private static float Headroom((Surface Surface, GameObject Go, Bounds Box) top,
            Vector3 outward, bool depthAlongX, bool atras)
        {
            float libre = float.PositiveInfinity;
            float signo = depthAlongX ? outward.x : outward.z;
            float centro = depthAlongX ? top.Box.center.x : top.Box.center.z;
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!r.enabled || r.transform.IsChildOf(top.Go.transform))
                    continue;
                if (r.GetComponentInParent<Canvas>() != null)
                    continue;
                Bounds b = r.bounds;
                if (b.size.magnitude > 20f)
                    continue;
                if (b.min.y < top.Surface.WorktopHeight + .25f)
                    continue;   // a ras de tapa no es techo, es lo que ya hay encima
                float lateral = depthAlongX ? b.center.z : b.center.x;
                float lmin = depthAlongX ? top.Box.min.z : top.Box.min.x;
                float lmax = depthAlongX ? top.Box.max.z : top.Box.max.x;
                if (lateral < lmin || lateral > lmax)
                    continue;
                float prof = depthAlongX ? b.center.x : b.center.z;
                if (((prof - centro) * signo < 0f) != atras)
                    continue;
                libre = Mathf.Min(libre, b.min.y - top.Surface.WorktopHeight);
            }
            return libre;
        }

        /// <summary>
        /// Una banda: los EPP en fila a intervalos iguales, con la base posada en la tapa. Lo
        /// ancho al centro y lo estrecho a los lados, que queda compuesto en vez de parecer una
        /// lista. Si no caben holgados se juntan hasta un mínimo, y si ni así, se avisa con el
        /// número en vez de dejarlos encima unos de otros.
        /// </summary>
        private static void Fila((Surface Surface, GameObject Go, Bounds Box) top, List<Pieza> fila,
            bool depthAlongX, float profundidad, float inicio, float largo, string zoneId)
        {
            if (fila.Count == 0 || largo <= 0f)
                return;

            float suma = fila.Sum(p => p.W);
            float hueco = (largo - suma) / (fila.Count + 1);
            if (hueco < MinimoEntreVecinos)
            {
                // No cabe holgado. Se reparte lo que haya, nunca negativo: con hueco negativo el
                // cursor se salía del mueble y el último quedaba en el aire, fuera de la tapa.
                hueco = Mathf.Max(0f, (largo - suma) / (fila.Count + 1));
                Debug.LogWarning($"[Module01] En '{zoneId}', sobre '{top.Surface.Name}': {fila.Count} elementos " +
                                 $"piden {suma:F2} m y el tramo da {largo:F2}. Van pegados" +
                                 (suma > largo ? " Y SE SALEN: sobran " + (suma - largo).ToString("F2") + " m." : "."));
            }

            var ordenada = new List<Pieza>();
            foreach (Pieza pz in fila.OrderByDescending(p => p.W))
                if (ordenada.Count % 2 == 0)
                    ordenada.Add(pz);
                else
                    ordenada.Insert(0, pz);

            float cursor = inicio + hueco;
            foreach (Pieza pz in ordenada)
            {
                float a = cursor + pz.W * .5f;
                Vector3 destino = depthAlongX
                    ? new Vector3(profundidad, 0f, a)
                    : new Vector3(a, 0f, profundidad);

                Undo.RecordObject(pz.E.transform, "Repartir en la mesa");
                // En planta manda la caja de clic —es lo que no se debe solapar—; en vertical, la
                // base de lo que se ve, para que se pose sin flotar ni hundirse.
                pz.E.transform.position += destino - pz.Centro;
                pz.E.transform.position += Vector3.up * (top.Surface.WorktopHeight - pz.BaseY);

                // Queda apuntado dónde lo dejamos: si la próxima vez no está aquí es que lo
                // movió una persona, y entonces no se toca.
                Module01LayoutStamp.Apuntar(pz.E.ItemId, pz.E.transform.position);

                cursor = a + pz.W * .5f + hueco;
            }
        }


        /// <summary>
        /// Lleva al mueble de su parada lo que descansa sobre el mueble de otra.
        /// <para>
        /// Hace falta desde que el reparto dejó de ser el del artista (PO, 2026-10-29): él agrupó
        /// por tipo y eso ponía los cinco esenciales en la mesa de la entrada. Antes esto se hacía
        /// a mano, elemento por elemento, en <c>Module01ArtEpp.Moves</c>; con veinticinco mudanzas
        /// eso eran veinticinco coordenadas escritas a ojo.
        /// </para>
        /// <para>
        /// La prueba es «¿sobre qué mueble está?», no «¿a qué distancia de su parada?»: la mesa
        /// del centro y la del fondo se tocan, así que por distancia un elemento de una pasaría
        /// por bueno en la otra. Y lo que no descansa sobre ningún mueble —la mochila, la alforja,
        /// la bolsa, las Crocs y el poncho, que van en el suelo delante de su mesa— no cumple la
        /// condición y <b>no se toca</b>, que es justo lo que queremos.
        /// </para>
        /// </summary>
        private static int Relocate(List<ZoneSO> zones, Dictionary<string, WorldObjectTaskEntity> byId)
        {
            // Todos los muebles declarados, con la parada a la que pertenecen.
            var furniture = new List<(string ZoneId, GameObject Go, Bounds Box)>();
            foreach (Station station in Module01Stations.All)
                foreach (Surface s in station.Surfaces)
                    if (TryTop(station.ZoneId, s, out GameObject go, out Bounds box))
                        furniture.Add((station.ZoneId, go, box));

            int moved = 0;
            foreach (ZoneSO zone in zones)
            {
                var tops = Tops(zone);
                if (tops.Count == 0)
                    continue;
                List<WorldObjectTaskEntity> mine = Mine(zone, byId);
                List<WorldObjectTaskEntity> strangers = mine
                    .Where(e => !IsLoose(e) && TryBounds(e.gameObject, out Bounds b) && OnFurnitureOf(b, furniture, zone.ZoneId))
                    .ToList();
                if (strangers.Count == 0)
                    continue;

                // Lo que ya estaba bien puesto en esta parada marca dónde buscar hueco, y lo que
                // ocupa sitio se recalcula con los forasteros fuera: si no, se estorban a sí mismos.
                List<WorldObjectTaskEntity> settled = mine.Except(strangers).ToList();
                Vector3 near = settled.Count > 0 && TryBounds(settled.Select(e => e.gameObject), out Bounds group)
                    ? group.center
                    : tops[0].Box.center;
                List<Bounds> occupied = Occupied(tops, strangers);

                Bounds whole = tops[0].Box;
                foreach (var t in tops)
                    whole.Encapsulate(t.Box);
                Vector3 outward = Outward(whole, GarageFloor());
                // Lo que ya está puesto cuenta para la regla de altura: la segunda fila se mide
                // contra lo del artista, no solo contra lo que mudamos nosotros.
                List<Bounds> placed = settled.Where(e => TryBounds(e.gameObject, out Bounds _))
                    .Select(e => { TryBounds(e.gameObject, out Bounds b); return Clickable(e, b); }).ToList();

                // Lo alto primero: si lo bajo entra antes, se queda delante y cierra el fondo.
                foreach (WorldObjectTaskEntity entity in strangers
                             .OrderByDescending(e => TryBounds(e.gameObject, out Bounds b) ? b.size.y : 0f))
                {
                    if (!TryBounds(entity.gameObject, out Bounds box))
                        continue;
                    Vector3 size = Clickable(entity, box).size;
                    if (!TryFirstSpot(tops, size, occupied, near, c => Taller(c, size, placed, outward),
                                      out Vector3 spot, out Bounds used, outward))
                    {
                        Debug.LogWarning($"[Module01] '{entity.ItemId}' pasa a la parada '{zone.ZoneId}', " +
                                         "pero no queda hueco libre en su mueble; se queda donde está.");
                        continue;
                    }
                    Move(entity, spot, occupied, used, outward, placed);
                    moved++;
                }
            }

            if (moved > 0)
                Debug.Log($"[Module01] {moved} elementos mudados al mueble de su parada.");
            return moved;
        }

        /// <summary>
        /// Si la caja descansa sobre un mueble que no es de <paramref name="zoneId"/>. «Sobre»
        /// es: su centro en planta cae dentro del mueble y su base no está por debajo de la de él
        /// —lo que cuelga delante, como el poncho, queda fuera—.
        /// </summary>
        private static bool OnFurnitureOf(Bounds box, List<(string ZoneId, GameObject Go, Bounds Box)> furniture,
            string zoneId) =>
            furniture.Any(f => f.ZoneId != zoneId
                               && box.center.x >= f.Box.min.x && box.center.x <= f.Box.max.x
                               && box.center.z >= f.Box.min.z && box.center.z <= f.Box.max.z
                               && box.min.y >= f.Box.min.y);

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

            List<WorldObjectTaskEntity> settled = mine.Except(loose).ToList();
            Vector3 near = settled.Count > 0 && TryBounds(settled.Select(e => e.gameObject), out Bounds group)
                ? group.center
                : tops[0].Box.center;
            List<Bounds> occupied = Occupied(tops, loose);

            Bounds furniture = tops[0].Box;
            foreach (var t in tops)
                furniture.Encapsulate(t.Box);
            Vector3 outward = Outward(furniture, GarageFloor());
            List<Bounds> placed = settled
                .Select(e => { TryBounds(e.gameObject, out Bounds b); return Clickable(e, b); }).ToList();

            foreach (WorldObjectTaskEntity entity in loose
                         .OrderByDescending(e => TryBounds(e.gameObject, out Bounds b) ? b.size.y : 0f))
            {
                if (!TryBounds(entity.gameObject, out Bounds box))
                    continue;
                Vector3 size = Clickable(entity, box).size;
                if (!TryFirstSpot(tops, size, occupied, near, c => Taller(c, size, placed, outward),
                                  out Vector3 spot, out Bounds used, outward))
                {
                    Debug.LogWarning($"[Module01] No queda hueco libre en los muebles de '{zone.ZoneId}' para " +
                                     $"'{entity.ItemId}'; se queda donde está.");
                    continue;
                }
                Move(entity, spot, occupied, used, outward, placed);
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
                Bounds furniture = tops[0].Box;
                foreach (var t in tops)
                    furniture.Encapsulate(t.Box);
                Vector3 outward = Outward(furniture, GarageFloor());
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

                    if (TryFirstSpot(tops, box.size, occupied, box.center, Shows, out Vector3 spot, out Bounds used, outward))
                        box = Move(entity, spot, occupied, used, outward);
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
            List<Bounds> occupied, Vector3 near, System.Func<Vector3, bool> accept, out Vector3 spot, out Bounds used,
            Vector3 outward = default)
        {
            foreach ((Surface s, GameObject _, Bounds top) in tops)
            {
                IEnumerable<Vector3> candidates = StationLayout.FreeCandidates(top, s.WorktopHeight, size, occupied, near);
                // Dos criterios, por este orden:
                //
                // 1. Al fondo primero. `FreeCandidates` ordena por cercanía a `near`, que cae a
                //    media profundidad: con eso el primer elemento se planta en mitad de la tapa
                //    y deja un resto demasiado estrecho para una segunda fila. Rellenando desde
                //    el fondo caben las dos, y como lo alto entra antes, lo de detrás sobresale.
                //    En tramos de 10 cm, para que a igual fila mande el criterio siguiente.
                // 2. A igual fila, el más cercano a lo que ya hay.
                //
                // PENDIENTE (PO, 2026-10-29): esto amontona en un extremo y deja el otro vacío.
                // Probé a invertirlo —elegir el punto más lejano de lo ocupado— y salió peor: los
                // objetos se van a las dos esquinas y el agujero queda en el medio. Lo que hace
                // falta es repartir a intervalos iguales a lo largo de la tapa, que es otro
                // algoritmo, no otro orden. Mientras tanto se queda el comportamiento conocido.
                if (outward != default)
                    candidates = candidates
                        .OrderByDescending(c => Mathf.Floor((Mathf.Abs(outward.x) > .5f
                            ? (top.center.x - c.x) * outward.x
                            : (top.center.z - c.z) * outward.z) / .10f))
                        .ThenBy(c => (Flat(c) - Flat(near)).sqrMagnitude);
                foreach (Vector3 c in candidates)
                    if (accept(c))
                    {
                        spot = c;
                        used = top;
                        return true;
                    }
            }
            spot = default;
            used = default;
            return false;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        /// <summary>Lleva el signo al hueco y deja ocupado su tramo del mueble.</summary>
        private static Bounds Move(WorldObjectTaskEntity entity, Vector3 spot, List<Bounds> occupied, Bounds top,
            Vector3 outward, List<Bounds> placed = null)
        {
            TryBounds(entity.gameObject, out Bounds box);
            box = Clickable(entity, box);
            Undo.RecordObject(entity.transform, "Colocar signo");
            entity.transform.position += spot - box.center;
            occupied.Add(FrontBlock(top, spot, box.size, outward));
            Bounds at = new Bounds(spot, box.size);
            placed?.Add(at);
            return at;
        }

        /// <summary>
        /// Lo que un elemento deja ocupado: <b>su propia caja</b>, nada más.
        /// <para>
        /// Hasta el 2026-10-29 esto bloqueaba el fondo <b>entero</b>, así que cada mueble era una
        /// fila y no una superficie. Con eso los 61 elementos pedían 24,3 m de mesa y el garaje
        /// tiene 15,7: no cabían, y el cliente no quiere nada en el suelo salvo los vehículos.
        /// </para>
        /// <para>
        /// Bloquear hacia delante tampoco servía: la primera fila que entraba por el fondo
        /// cerraba toda la franja delantera de su tramo y la mesa se quedaba al 65 % vacía
        /// (medido en la mesa de herramientas el 2026-10-29). Quien impide que algo quede tapado
        /// es <see cref="Taller"/>, que compara alturas en los dos sentidos; esta caja solo evita
        /// que dos objetos se pisen.
        /// </para>
        /// </summary>
        private static Bounds FrontBlock(Bounds top, Vector3 spot, Vector3 size, Vector3 outward) =>
            new Bounds(spot, size);

        /// <summary>
        /// Hacia dónde se mira el mueble: el lado que da al centro del garaje. Misma regla que
        /// usa <see cref="StationLayout.TryFrontView"/> para plantar el ancla, para que «delante»
        /// signifique lo mismo en los dos sitios.
        /// </summary>
        private static Vector3 Outward(Bounds furniture, Bounds floor) =>
            furniture.size.x < furniture.size.z
                ? new Vector3(Mathf.Sign(floor.center.x - furniture.center.x), 0f, 0f)
                : new Vector3(0f, 0f, Mathf.Sign(floor.center.z - furniture.center.z));

        /// <summary>
        /// Si una caja puesta en <paramref name="spot"/> se vería por encima de lo que ya hay
        /// delante de ella. Lo que está al lado no estorba: solo cuenta lo que se cruza en su
        /// tramo. Dos centímetros de margen, que es lo que hace falta para que se lea el borde.
        /// </summary>
        private static bool Taller(Vector3 spot, Vector3 size, IEnumerable<Bounds> placed, Vector3 outward)
        {
            bool depthAlongX = Mathf.Abs(outward.x) > .5f;
            float top = spot.y + size.y * .5f;
            foreach (Bounds p in placed)
            {
                float lateral = depthAlongX ? Mathf.Abs(p.center.z - spot.z) : Mathf.Abs(p.center.x - spot.x);
                float halfs = depthAlongX ? (p.size.z + size.z) * .5f : (p.size.x + size.x) * .5f;
                if (lateral >= halfs)
                    continue;   // está al lado: no se cruzan en la línea de visión

                // Se cruzan. El de atrás tiene que sobresalir por encima del de delante, mire
                // quién mire: si el nuevo va detrás, tiene que ser él; si va delante, el otro.
                float ahead = depthAlongX ? (p.center.x - spot.x) * outward.x : (p.center.z - spot.z) * outward.z;
                if (ahead > 0f ? top <= p.max.y + .02f : p.max.y <= top + .02f)
                    return false;
            }
            return true;
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
