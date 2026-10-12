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
    /// <b>Los sitios de los puntos son aproximados y necesitan ojo humano.</b> Salen de
    /// fracciones de la caja de cada vehículo, no de huesos ni de sub-mallas, porque las mallas
    /// que entregó el equipo de arte son una pieza única sin hijos: no hay "el manubrio" ni "la
    /// rueda delantera" que agarrar. Quedan donde se ven razonables; moverlos a mano en la
    /// escena es rápido y no lo pisa nada, porque este comando solo los crea si faltan.
    /// </para>
    /// </summary>
    public static class Module01VehicleStaging
    {
        private const string AnchorsRoot = "Module01_Anchors";
        private const string VehicleAnchor = "anchor_" + Module01Director.VehicleAnchorId;
        private const string DataFolder = "Assets/_Project/Module01/Data";

        /// <summary>Altura de los ojos del aprendiz de pie. La misma que usan las zonas personales.</summary>
        private const float EyeHeight = 1.45f;

        /// <summary>
        /// Dónde se planta el aprendiz respecto al vehículo. Suficiente para que una bici de
        /// casi dos metros quepa de costado sin que los marcadores se salgan por los lados.
        /// </summary>
        private const float ViewDistance = 2.4f;

        /// <summary>
        /// Dónde cuelga cada punto, en fracciones de la caja del vehículo:
        /// <c>u</c> a lo largo, <c>v</c> en altura, <c>w</c> en anchura.
        /// <para>
        /// El eje largo es la <b>X local</b> en los dos vehículos: la bici mide 1.97 en Z de
        /// mundo pero está girada 90°, y el patinete 1.27 girado 270°. Medido, no supuesto.
        /// </para>
        /// </summary>
        private static readonly Dictionary<string, Vector3> Mounts = new Dictionary<string, Vector3>
        {
            { "point_steering",  new Vector3(.86f, .94f, .50f) }, // manubrio
            { "point_brakes",    new Vector3(.79f, .78f, .50f) }, // maneta de freno
            { "point_lights",    new Vector3(.95f, .62f, .50f) }, // faro delantero
            { "point_wheels",    new Vector3(.10f, .13f, .50f) }, // rueda
            { "point_electrics", new Vector3(.45f, .36f, .50f) }, // bateria
        };

        /// <summary>
        /// Giro del vehículo en cada zona, en grados y <b>absolutos</b>: <c>VehicleStage</c>
        /// asigna la rotación local, no la suma.
        /// <para>
        /// Con el aprendiz mirando hacia −Z: a 0° el eje largo del vehículo queda a lo ancho de
        /// la vista, que es el costado; a ±90° queda de frente o de espaldas. Cuál de los dos
        /// extremos es el morro <b>no se puede saber desde el código</b> —la malla no tiene ni
        /// hijos ni nombres—, así que esto es una primera aproximación: si el vehículo sale de
        /// culo en la zona de manubrio, se intercambian los 90 por −90 en este diccionario.
        /// </para>
        /// </summary>
        private static readonly Dictionary<string, float> Yaws = new Dictionary<string, float>
        {
            { "cockpit",    90f },   // de frente: manubrio, frenos, electrico
            { "wheels",      0f },   // de costado: se ven las dos ruedas
            { "visibility", -90f },  // por detras: luces y reflectivos
        };

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

            VehicleProfileSO[] profiles =
            {
                AssetDatabase.LoadAssetAtPath<VehicleProfileSO>($"{DataFolder}/SO_VehicleProfile_Ebike.asset"),
                AssetDatabase.LoadAssetAtPath<VehicleProfileSO>($"{DataFolder}/SO_VehicleProfile_Escooter.asset"),
            };

            PlaceAnchor(display);

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
                CreateMounts(model);
                ApplyYaws(profile);
            }

            // En el editor los dos vehiculos estan encendidos a la vez y, ahora que comparten
            // el punto de exhibicion, se interpenetran. Se deja visible el primero, que es el
            // que el escenario enciende tambien en runtime mientras nadie elija.
            ShowOnlyFirst(profiles);
            AttachPointsToMounts();

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

        /// <summary>Deja encendida solo la malla del primer perfil.</summary>
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
        private static void PlaceAnchor(Vector3 display)
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

            // El aprendiz se coloca hacia +Z y mira hacia el vehiculo, o sea hacia -Z.
            Vector3 eye = new Vector3(display.x, EyeHeight, display.z + ViewDistance);
            anchor.SetPositionAndRotation(eye, Quaternion.LookRotation(
                new Vector3(0f, 0f, -1f), Vector3.up));
            Debug.Log($"[Module01] {VehicleAnchor} en {eye:F2}, mirando al punto de exhibición.");
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
        private static void CreateMounts(GameObject model)
        {
            if (!TryLocalBounds(model, out Bounds local))
                return;

            foreach (KeyValuePair<string, Vector3> entry in Mounts)
            {
                string name = "mount_" + entry.Key;
                Transform mount = model.transform.Find(name);
                if (mount != null)
                    continue;

                mount = new GameObject(name).transform;
                mount.SetParent(model.transform, false);
                mount.localPosition = local.min + Vector3.Scale(local.size, entry.Value);
                mount.localRotation = Quaternion.identity;
                Undo.RegisterCreatedObjectUndo(mount.gameObject, "Mount del vehiculo");
            }
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
        private static void BuildMarker(WorldObjectTaskEntity point)
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
            marker.transform.localScale = Vector3.one * .009f * compensate;

            RectTransform rect = (RectTransform)marker.transform;
            rect.sizeDelta = new Vector2(60f, 90f);

            Canvas canvas = marker.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            GameObject label = new GameObject("Glyph", typeof(RectTransform));
            label.transform.SetParent(marker.transform, false);
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI glyph = label.AddComponent<TextMeshProUGUI>();
            glyph.text = "!";
            glyph.fontSize = 86f;
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
            List<(ZoneSO Zone, float Yaw)> views = new List<(ZoneSO, float)>();
            foreach (ZoneSO zone in profile.InspectionZones)
            {
                if (zone == null)
                    continue;
                views.Add((zone, Yaws.TryGetValue(zone.ZoneId, out float yaw) ? yaw : 0f));
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
