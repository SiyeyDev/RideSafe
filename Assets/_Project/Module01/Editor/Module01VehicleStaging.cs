using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Monta la sección del vehículo como la pidió la reunión: el aprendiz se queda quieto en
    /// un único punto, el vehículo elegido aparece en el centro del garaje y <b>gira</b> para
    /// enseñarle cada componente, con los puntos de inspección colgando de su malla.
    /// <para>
    /// Hasta ahora los cinco puntos eran esferas en fila a 1.30 de alto y z=0.80, colgando de
    /// <c>Module01_Spawned</c> y <b>sin tocar ningún vehículo</b>: el aprendiz "inspeccionaba
    /// los frenos" mirando una bola flotando a dos metros de la bici. Y el giro ya estaba
    /// programado pero dormido, tras una guarda que pedía que existiera el ancla
    /// <c>anchor_vehicle</c>. Esto crea el ancla y cuelga los puntos, que es lo que faltaba.
    /// </para>
    /// <para>
    /// <b>Los sitios de los puntos salen de fracciones de la caja del vehículo, y se respetan
    /// una vez puestos.</b> Vienen de cuando las mallas eran una pieza única sin hijos y no
    /// había "el manubrio" que agarrar; se ajustaron a ojo contra capturas, así que este
    /// comando solo los crea si faltan y nunca los mueve. Desde que los vehículos llegan
    /// despiezados, el punto que tiene pieza se muda además encima de ella en runtime, al sitio
    /// de la pieza <b>más cercano a donde ya estaba</b> —ver <c>VehicleStage.MoveToPart</c>—,
    /// que es lo que conserva ese ajuste.
    /// </para>
    /// <para>
    /// Lo que distingue a un vehículo de otro —zonas y giros, piezas, puntos de anclaje— vive
    /// en <see cref="VehicleProfiles"/>; aquí solo se aplica a la escena abierta.
    /// </para>
    /// </summary>
    public static class Module01VehicleStaging
    {
        private const string AnchorsRoot = "Module01_Anchors";
        public const string MarkerFontPath = "Assets/_Project/UI/Fonts/Manrope-Bold SDF.asset";
        private const string VehicleAnchor = "anchor_" + Module01Director.VehicleAnchorId;

        /// <summary>Altura de los ojos del aprendiz de pie. La misma que usan las zonas personales.</summary>
        private const float EyeHeight = 1.45f;

        /// <summary>
        /// Dónde se planta el aprendiz respecto al vehículo. Suficiente para que una bici de
        /// casi dos metros quepa de costado sin que los marcadores se salgan por los lados.
        /// </summary>
        private const float ViewDistance = 2.7f;

        [MenuItem("RideSafe/Módulo 1/Montar el punto de exhibición del vehículo")]
        public static void StageOpenScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Module01] Sal de Play antes de montar el vehículo.");
                return;
            }

            if (!TryDisplayPoint(out Vector3 display))
                return;

            VehicleProfileSO[] profiles = VehicleProfiles.LoadAll().ToArray();

            float tallest = 0f;
            foreach (VehicleProfileSO profile in profiles)
            {
                if (profile == null || profile.Model == null)
                {
                    Debug.LogWarning("[Module01] Un perfil de vehículo no tiene modelo; se salta.");
                    continue;
                }

                GameObject model = Find(profile.Model.name);
                if (model == null)
                {
                    Debug.LogWarning($"[Module01] La escena no tiene la malla '{profile.Model.name}'.");
                    continue;
                }

                MoveToDisplay(model, display);
                if (TryBounds(model, out Bounds box))
                    tallest = Mathf.Max(tallest, box.size.y);
                CreateMounts(model, profile);
                ApplyYaws(profile);
            }

            PlaceAnchor(display, tallest);

            // En el editor los dos vehiculos estan encendidos a la vez y, ahora que comparten
            // el punto de exhibicion, se interpenetran. Se deja visible el primero, que es el
            // que el escenario enciende tambien en runtime mientras nadie elija.
            ShowOnlyFirst(profiles);
            AttachPointsToMounts();
            ParkPointsTheShownVehicleDoesNotInspect(profiles);
            WireOutlines(profiles);

            // Y el escenario se queda con la lista de puntos, para recolgarlos del vehiculo que
            // el aprendiz elija.
            Module01Director director = Object.FindAnyObjectByType<Module01Director>(FindObjectsInactive.Include);
            if (director != null)
                Module01VehicleWiring.Apply(director);
            else
                Debug.LogWarning("[Module01] Sin Module01Director en la escena: el escenario se queda sin la lista de puntos.");

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log(
                $"[Module01] Vehículos en el punto de exhibición {display:F2}, ancla a {ViewDistance} m. " +
                "Los sitios de los puntos son aproximados: conviene repasarlos a ojo en la escena.");
        }

        private const string OutlineMaterialPath =
            "Assets/_Project/Module01/Art/M_InspectionOutline.mat";

        /// <summary>
        /// Le pone a cada punto su <see cref="InspectionHighlight"/> con el material del
        /// contorno, y escribe en cada perfil qué pieza le toca. Idempotente.
        /// </summary>
        private static void WireOutlines(VehicleProfileSO[] profiles)
        {
            Material outline = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
            if (outline == null)
            {
                Debug.LogError(
                    $"[Module01] Falta el material del contorno en {OutlineMaterialPath}; " +
                    "los puntos se quedan solo con el signo.");
                return;
            }

            foreach (VehicleProfileSO profile in profiles)
            {
                if (profile == null)
                    continue;
                (string Item, string Part)[] table = VehicleProfiles.TryGet(profile.VehicleContextValue, out VehicleProfiles.Definition definition)
                    ? definition.Parts.ToArray()
                    : new (string Item, string Part)[0];
                Undo.RecordObject(profile, "Piezas del vehiculo");
                profile.ConfigurePartBindings(table);
                EditorUtility.SetDirty(profile);
                Debug.Log($"[Module01] {profile.name}: {table.Length} pieza(s) a delinear"
                          + (table.Length == 0 ? " (sin piezas declaradas: todo al signo)." : ": "
                              + string.Join(", ", table.Select(p => $"{p.Item}->{p.Part}")) + "."));
            }

            int wired = 0;
            foreach (WorldObjectTaskEntity point in Object
                         .FindObjectsByType<WorldObjectTaskEntity>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .Where(e => e.ItemId.StartsWith("point_")))
            {
                InspectionHighlight highlight =
                    Module01EditorComponents.Ensure<InspectionHighlight>(point.gameObject);
                Undo.RecordObject(highlight, "Contorno del punto");
                // Los mismos colores que el signo: naranja lo que toca, verde lo resuelto.
                highlight.ConfigureForTests(
                    outline, new Color(1f, .42f, .05f, 1f), new Color(.46f, .83f, .54f, .9f));
                EditorUtility.SetDirty(highlight);
                wired++;
            }
            Debug.Log($"[Module01] {wired} puntos con contorno disponible. Cuál se usa de verdad " +
                      "lo decide el escenario al encender el vehículo.");
        }

        /// <summary>Deja encendida solo la malla del primer perfil.</summary>
        /// <summary>
        /// Aparta y apaga los puntos que no revisa el vehículo que queda a la vista —la
        /// plataforma, con la bici—, igual que hace <c>VehicleStage</c> en runtime. Sin esto el
        /// punto se quedaba encendido colgando de la bici: se veía y respondía al clic en el
        /// módulo 0, y la copia de la vitrina se lo llevaba dentro.
        /// </summary>
        private static void ParkPointsTheShownVehicleDoesNotInspect(VehicleProfileSO[] profiles)
        {
            VehicleProfileSO shown = profiles.FirstOrDefault(p => p != null && p.Model != null && Find(p.Model.name) != null);
            if (shown == null)
                return;

            GameObject parking = Find("Module01_Spawned") ?? new GameObject("Module01_Spawned");
            foreach (WorldObjectTaskEntity point in Object
                         .FindObjectsByType<WorldObjectTaskEntity>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .Where(e => e.ItemId.StartsWith("point_") && !shown.Inspects(e.ItemId)))
            {
                Undo.SetTransformParent(point.transform, parking.transform, "Apartar punto de otro vehiculo");
                Undo.RecordObject(point.gameObject, "Apartar punto de otro vehiculo");
                point.gameObject.SetActive(false);
                Debug.Log($"[Module01] '{point.ItemId}' no es de '{shown.name}': apagado en {parking.name} hasta que se elija su vehículo.");
            }
        }

        private static void ShowOnlyFirst(VehicleProfileSO[] profiles)
        {
            bool first = true;
            foreach (VehicleProfileSO profile in profiles)
            {
                if (profile == null || profile.Model == null)
                    continue;
                GameObject model = Find(profile.Model.name);
                if (model == null)
                    continue;
                Undo.RecordObject(model, "Mostrar un solo vehiculo");
                model.SetActive(first);
                first = false;
            }
        }

        /// <summary>Centro del suelo del garaje, sobre su cara superior.</summary>
        private static bool TryDisplayPoint(out Vector3 point)
        {
            point = default;
            GameObject floor = Find("piso");
            if (floor == null)
            {
                Debug.LogError("[Module01] No encuentro el suelo 'piso' para situar el punto de exhibición.");
                return false;
            }

            Renderer[] renderers = floor.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogError("[Module01] El suelo 'piso' no tiene renderer.");
                return false;
            }

            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers)
                b.Encapsulate(r.bounds);

            point = new Vector3(b.center.x, b.max.y, b.center.z);
            return true;
        }

        /// <summary>
        /// Crea o recoloca <c>anchor_vehicle</c>. Mientras no exista, el director sigue
        /// llevando al aprendiz a un ancla por zona y el vehículo no gira: la guarda de
        /// <c>Module01Director.EnterZone</c> lo comprueba justo por eso.
        /// </summary>
        private static void PlaceAnchor(Vector3 display, float vehicleHeight)
        {
            GameObject root = Find(AnchorsRoot);
            if (root == null)
            {
                Debug.LogError($"[Module01] La escena no tiene '{AnchorsRoot}'.");
                return;
            }

            Transform anchor = root.transform.Find(VehicleAnchor);
            if (anchor == null)
            {
                anchor = new GameObject(VehicleAnchor).transform;
                anchor.SetParent(root.transform, false);
                Undo.RegisterCreatedObjectUndo(anchor.gameObject, "Ancla del vehiculo");
            }

            // El aprendiz se coloca hacia +Z y mira al vehiculo, EN HORIZONTAL.
            //
            // Se probó a inclinar el ancla hacia el centro del vehículo para encuadrarlo mejor
            // y fue peor por dos motivos. Uno: en VR el cabeceo lo pone el visor, no el rig, así
            // que inclinar el ancla no es algo que el aprendiz vaya a ver. Y dos: el pedestal
            // del inspector cuelga de la cámara, y al inclinarse, su caja envolvente alineada
            // con los ejes del mundo crece y pasa a comerse el techo del popup de confirmación
            // —lo caza AssertPedestalClearsPopup—.
            //
            // El encuadre se resuelve con la distancia, no con el ángulo: a 2.7 m y 60° de
            // campo vertical, la mitad inferior alcanza 1.56 m, de sobra para el marcador más
            // bajo, que cuelga a unos 1.37 por debajo de la línea de ojos.
            Vector3 eye = new Vector3(display.x, EyeHeight, display.z + ViewDistance);
            anchor.SetPositionAndRotation(eye, Quaternion.LookRotation(new Vector3(0f, 0f, -1f), Vector3.up));
            Debug.Log($"[Module01] {VehicleAnchor} en {eye:F2}, en horizontal. " +
                      $"Vehículo más alto: {vehicleHeight:F2} m.");
        }

        /// <summary>Apoya el vehículo en el suelo y lo centra en el punto de exhibición.</summary>
        private static void MoveToDisplay(GameObject model, Vector3 display)
        {
            if (!TryBounds(model, out Bounds b))
                return;

            Undo.RecordObject(model.transform, "Mover vehiculo");
            Vector3 target = new Vector3(display.x, display.y + b.size.y * .5f, display.z);
            model.transform.position += target - b.center;
        }

        /// <summary>
        /// Cuelga de la malla un punto de anclaje por cada componente, derivado de su caja.
        /// Idempotente: si el mount ya existe no se mueve, para no deshacer un ajuste a mano.
        /// </summary>
        private static void CreateMounts(GameObject model, VehicleProfileSO profile)
        {
            if (!TryLocalBounds(model, out Bounds local))
                return;

            foreach (KeyValuePair<string, Vector3> entry in VehicleProfiles.Mounts)
            {
                // Solo los puntos que revisa este vehiculo: la bici no tiene plataforma.
                if (!profile.Inspects(entry.Key))
                    continue;

                string name = "mount_" + entry.Key;
                Transform mount = model.transform.Find(name);
                if (mount != null)
                    continue;

                mount = new GameObject(name).transform;
                mount.SetParent(model.transform, false);
                mount.localPosition = MountPosition(local, entry.Value);
                mount.localRotation = Quaternion.identity;
                Undo.RegisterCreatedObjectUndo(mount.gameObject, "Mount del vehiculo");
            }
        }

        /// <summary>
        /// Dónde cae una fracción <c>(u, v, w)</c> de la caja local: <c>u</c> a lo largo,
        /// <c>v</c> en altura y <c>w</c> a lo ancho. El largo es el eje horizontal <b>mayor</b>
        /// de la caja, X o Z: las fracciones se escribieron cuando los dos FBX lo traían en X, y
        /// el scooter de 2026-10-26 lo trae en Z.
        /// </summary>
        public static Vector3 MountPosition(Bounds local, Vector3 uvw)
        {
            Vector3 fraction = local.size.z > local.size.x
                ? new Vector3(uvw.z, uvw.y, uvw.x)
                : uvw;
            return local.min + Vector3.Scale(local.size, fraction);
        }

        /// <summary>
        /// Lleva cada punto de inspección a su mount en el vehículo <b>de la e-bike</b>.
        /// <para>
        /// Una sola entidad por punto sirve a los dos vehículos: los ids no llevan el vehículo
        /// (<c>module01.cockpit.point_brakes</c>), que es lo que permite que los dos perfiles
        /// compartan las tres zonas. Se cuelga del que esté encendido; al cambiar de vehículo
        /// hay que volver a correr esto, igual que el cableado del escenario.
        /// </para>
        /// </summary>
        private static void AttachPointsToMounts()
        {
            WorldObjectTaskEntity[] points = Object
                .FindObjectsByType<WorldObjectTaskEntity>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(e => e.ItemId.StartsWith("point_"))
                .ToArray();

            int attached = 0;
            foreach (WorldObjectTaskEntity point in points)
            {
                Transform mount = Object
                    .FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(t => t.name == "mount_" + point.ItemId && t.gameObject.activeInHierarchy);

                if (mount == null)
                {
                    Debug.LogWarning($"[Module01] Sin mount para '{point.ItemId}'; se queda donde estaba.");
                    continue;
                }

                Undo.RecordObject(point.transform, "Colgar punto del vehiculo");
                point.transform.SetParent(mount, false);
                point.transform.localPosition = Vector3.zero;
                attached++;
            }

            Debug.Log($"[Module01] Puntos de inspección colgados del vehículo: {attached} de {points.Length}.");

            foreach (WorldObjectTaskEntity point in points)
                BuildMarker(point);
        }

        /// <summary>
        /// Cambia la esfera gris del punto por un signo de exclamación flotante.
        /// <para>
        /// La malla de la esfera se <b>apaga</b>, no se borra, y el collider se queda: es lo que
        /// recibe el clic. El signo es texto, no un sprite del atlas, porque este comando vive
        /// en el asmdef del módulo 1 y el kit de UI está en <c>Assembly-CSharp-Editor</c>, fuera
        /// de su alcance.
        /// </para>
        /// </summary>
        /// <summary>
        /// Monta el signo «!» sobre un punto clicable: apaga su malla, le cuelga el glifo en
        /// capa UI y le pone el <see cref="InspectionMarker"/>.
        /// <para>
        /// Es público porque <c>Module01SceneEntities</c> lo usa para los EPP que todavía no
        /// tienen 3D. El cálculo de escala y de altura se compensa con la escala del punto, así
        /// que el signo mide lo mismo cuelgue de la malla de un vehículo o de una esfera sobre
        /// una mesa: no hace falta una segunda versión para los EPP, y dos versiones acabarían
        /// divergiendo.
        /// </para>
        /// </summary>
        public static void BuildMarker(WorldObjectTaskEntity point)
        {
            // La esfera deja de verse; su collider es lo unico que sigue haciendo falta.
            foreach (MeshRenderer renderer in point.GetComponents<MeshRenderer>())
                renderer.enabled = false;

            if (point.transform.Find("Marker") != null)
                return;

            GameObject marker = new GameObject("Marker", typeof(RectTransform), typeof(Canvas));
            marker.transform.SetParent(point.transform, false);
            // El punto esta escalado a 0.32, asi que el marcador compensa para medir lo mismo
            // cuelgue de donde cuelgue.
            float compensate = point.transform.lossyScale.x <= 0f ? 1f : 1f / point.transform.lossyScale.x;
            marker.transform.localPosition = new Vector3(0f, .26f * compensate, 0f);
            // 0.0018 y no 0.009. La cuenta: el rect mide 112 unidades de alto, asi que con
            // 0.009 de escala en mundo el signo medía UN METRO. Por eso se pisaban entre ellos
            // y con el HUD: no era que estuvieran mal colocados, es que eran enormes. A 0.0018
            // mide unos 20 cm, que desde los 2.7 m del punto de exhibicion ocupa un 6 % de la
            // altura de pantalla: se ve de sobra y no tapa nada.
            marker.transform.localScale = Vector3.one * .0018f * compensate;

            RectTransform rect = (RectTransform)marker.transform;
            // 70x112 y no 60x90: medido, una '!' a 86 pt pide 96.1 de alto, asi que en 90 se
            // recortaba por abajo. No se ve de un vistazo; lo caza AssertTextIsLegible.
            rect.sizeDelta = new Vector2(70f, 112f);

            Canvas canvas = marker.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            GameObject label = new GameObject("Glyph", typeof(RectTransform));
            label.transform.SetParent(marker.transform, false);
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI glyph = label.AddComponent<TextMeshProUGUI>();
            // Brand font, not TMP's default (AutoHand's AmmoText in this project).
            TMP_FontAsset brand = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MarkerFontPath);
            if (brand != null) glyph.font = brand;
            glyph.text = "!";
            glyph.fontSize = 82f; // Manrope's '!' at 86 asks 117.5 of height in a 112 box
            glyph.fontStyle = FontStyles.Bold;
            glyph.alignment = TextAlignmentOptions.Center;
            glyph.raycastTarget = false;
            // Contorno oscuro: el naranja sobre el techo blanco del garaje se pierde.
            glyph.outlineColor = new Color32(10, 20, 35, 255);
            glyph.outlineWidth = .22f;

            // El marcador va a la capa UI, que dibuja la camara de overlay del stack de URP.
            // Esa camara limpia profundidad, asi que el signo se ve SIEMPRE, tambien cuando
            // esta dentro del cuadro de la bici: sin esto solo se veia el del manubrio y los
            // otros dos quedaban enterrados en la malla, que es justo lo contrario de lo que
            // hace un marcador de mision.
            //
            // El collider NO se mueve de capa: el PhysicsRaycaster de la camara principal
            // intersecta su mascara con la de la camara, y la UI esta excluida de ella, asi
            // que un collider en capa UI dejaria de recibir clics.
            int ui = LayerMask.NameToLayer("UI");
            if (ui >= 0)
                foreach (Transform t in marker.GetComponentsInChildren<Transform>(true))
                    t.gameObject.layer = ui;

            InspectionMarker component = Module01EditorComponents.Ensure<InspectionMarker>(point.gameObject);
            component.ConfigureForTests(glyph, marker.transform);

            Undo.RegisterCreatedObjectUndo(marker, "Marcador de inspeccion");
        }

        private static void ApplyYaws(VehicleProfileSO profile)
        {
            Dictionary<string, float> table = new Dictionary<string, float>();
            if (VehicleProfiles.TryGet(profile.VehicleContextValue, out VehicleProfiles.Definition definition))
                foreach ((string id, float yaw) in definition.Zones)
                    table[id] = yaw;
            else
                Debug.LogWarning(
                    $"[Module01] '{profile.VehicleContextValue}' no está en VehicleProfiles, asi que " +
                    "se queda a 0° en todas las zonas: el vehiculo no girara y las piezas que se " +
                    "delinean pueden quedar de espaldas.");

            List<(ZoneSO Zone, float Yaw)> views = new List<(ZoneSO, float)>();
            foreach (ZoneSO zone in profile.InspectionZones)
            {
                if (zone == null)
                    continue;
                views.Add((zone, table.TryGetValue(zone.ZoneId, out float yaw) ? yaw : 0f));
            }
            profile.ConfigureZoneViews(views);
            EditorUtility.SetDirty(profile);
            Debug.Log($"[Module01] Giros de {profile.name}: "
                      + string.Join(", ", views.Select(v => $"{v.Zone.ZoneId}={v.Yaw:F0}°")));
        }

        private static bool TryBounds(GameObject go, out Bounds box)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            box = default;
            if (renderers.Length == 0)
                return false;
            box = renderers[0].bounds;
            foreach (Renderer r in renderers)
                box.Encapsulate(r.bounds);
            return true;
        }

        /// <summary>La caja del objeto en SU espacio local, que es donde viven los mounts.</summary>
        private static bool TryLocalBounds(GameObject go, out Bounds local)
        {
            local = default;
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return false;

            bool first = true;
            foreach (Renderer r in renderers)
            {
                Bounds b = r.bounds;
                for (int k = 0; k < 8; k++)
                {
                    Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3(
                        (k & 1) == 0 ? -1 : 1, (k & 2) == 0 ? -1 : 1, (k & 4) == 0 ? -1 : 1));
                    Vector3 p = go.transform.InverseTransformPoint(corner);
                    if (first) { local = new Bounds(p, Vector3.zero); first = false; }
                    else local.Encapsulate(p);
                }
            }
            return !first;
        }

        private static GameObject Find(string name)
        {
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (go.name == name)
                    return go;
            return null;
        }
    }
}
