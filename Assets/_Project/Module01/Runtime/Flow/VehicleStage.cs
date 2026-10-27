using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RideSafe.Module01
{
    /// <summary>
    /// Enciende el vehículo elegido y apaga los demás.
    /// </summary>
    [AddComponentMenu("RideSafe/Module 01/Vehicle Stage")]
    public class VehicleStage : MonoBehaviour
    {
        [Serializable]
        private class Entry
        {
            [Tooltip("Valor de la clave 'vehicle' que enciende esta malla.")]
            public string VehicleContextValue;
            public GameObject Model;
        }

        [SerializeField] private List<Entry> _vehicles = new List<Entry>();

        [Tooltip("Puntos de inspección. Se cuelgan del vehículo elegido al encenderlo.")]
        [SerializeField] private List<WorldObjectTaskEntity> _inspectionPoints = new List<WorldObjectTaskEntity>();

        [Tooltip("Segundos que tarda el vehículo en girar de una zona a otra.")]
        [SerializeField, Min(0f)] private float _turnSeconds = .8f;

        private Entry _active;
        private Coroutine _turning;

        /// <summary>Último giro pedido, en grados. Lo que se está mostrando o se va a mostrar.</summary>
        public float TargetYaw { get; private set; }

        /// <summary>
        /// ¿Está el vehículo girando ahora mismo?
        /// <para>
        /// Lo consulta el flujo para no dar la zona por lista mientras la malla se mueve. Sin
        /// esto, el aprendiz puede pulsar el punto de las llantas cuando el vehículo todavía
        /// está de frente: el marcador aún no ha llegado a su sitio —medido, se queda a un 5 %
        /// por debajo del borde inferior de la pantalla— y el clic cae en el aire.
        /// </para>
        /// </summary>
        public bool IsTurning => _turning != null;

        /// <summary>La malla encendida ahora mismo, o null si no hay ninguna.</summary>
        public Transform ActiveModel => _active?.Model == null ? null : _active.Model.transform;

        /// <summary>
        /// Enciende la malla del vehículo del perfil, apaga las demás y delinea las piezas que el
        /// perfil declara. Si no hay malla para ese vehículo avisa y deja el garaje como estaba:
        /// apagarlo todo dejaría al aprendiz inspeccionando el aire.
        /// </summary>
        public void Show(VehicleProfileSO profile)
        {
            if (profile == null)
            {
                Debug.LogError("[Module01] Se pidio encender un vehiculo sin perfil.", this);
                return;
            }

            string wanted = Normalize(profile.VehicleContextValue);
            Entry match = null;
            foreach (Entry entry in _vehicles)
            {
                if (entry != null && Normalize(entry.VehicleContextValue) == wanted)
                {
                    match = entry;
                    break;
                }
            }

            if (match == null)
            {
                Debug.LogError(
                    $"[Module01] El escenario no tiene malla para el vehiculo '{profile.VehicleContextValue}'.", this);
                return;
            }

            foreach (Entry entry in _vehicles)
                if (entry?.Model != null)
                    entry.Model.SetActive(entry == match);

            _active = match;
            AttachInspectionPoints(match, profile);
            BindParts(profile);
            FaceYaw(TargetYaw, immediate: true);
        }

        /// <summary>
        /// Cuelga el contorno de la pieza real de cada punto que tenga una. El signo de
        /// exclamación se queda en todos, también en los delineados (Cindy, 2026-10-21: solo con
        /// el contorno, encontrar todas las piezas con la vista costaba): se pulsa el signo o la
        /// pieza, y al revisarlo los dos se ponen en verde.
        /// <para>
        /// Se llama en <b>cada</b> <c>Show</c>, porque lo primero que tiene que pasar al cambiar de
        /// vehículo es soltar el contorno del anterior: cuelga de la malla que se acaba de apagar.
        /// </para>
        /// </summary>
        private void BindParts(VehicleProfileSO profile)
        {
            Transform model = ActiveModel;

            foreach (WorldObjectTaskEntity point in _inspectionPoints)
            {
                if (point == null || string.IsNullOrEmpty(point.ItemId))
                    continue;

                InspectionHighlight highlight = point.GetComponent<InspectionHighlight>();
                InspectionMarker marker = point.GetComponent<InspectionMarker>();

                if (highlight != null)
                {
                    // Todas las piezas del punto: la e-bike de 2026-10-21 trae frenos, ruedas y
                    // luces en dos (Cindy: delinear las dos). La esfera va a la primera.
                    List<Transform> parts = new List<Transform>();
                    if (model != null)
                        foreach (string partName in profile.PartNamesFor(point.ItemId))
                        {
                            Transform part = FindPart(model, partName);
                            if (part != null)
                                parts.Add(part);
                        }

                    if (parts.Count == 0)
                        highlight.Release();
                    else if (highlight.BindParts(parts))
                        // La esfera va a la pieza y el signo con ella: así el signo queda sobre la
                        // pieza correcta en los dos vehículos, aunque estén en espejo. Que en la
                        // cabina no se amontonen lo deciden los mounts (Module01VehicleStaging).
                        MoveToPart(point, highlight.Part);
                }

                marker?.SetSuppressed(false);
            }
        }

        /// <summary>
        /// Lleva la esfera clicable del punto encima de la pieza que se delinea, y la cuelga de
        /// ella para que viaje con el vehículo al girar.
        /// <para>
        /// <b>Esto sustituye a un collider sobre la propia pieza, que es lo primero que se
        /// probó y hubo que descartar.</b> Un <c>MeshCollider</c> en <c>manubrio</c> parece la
        /// solución exacta —el blanco es la silueta que brilla— pero esa pieza no es el
        /// manillar: es todo el tren delantero, 0,66 × 1,11 × 0,76, y al hacerse opaca al
        /// raycast entierra las esferas de <c>point_brakes</c> y <c>point_electrics</c>, que
        /// cuelgan por detrás de ella. Lo cazó el recorrido de PlayMode con un «clic bloqueado
        /// por manubrio».
        /// </para>
        /// <para>
        /// Así no aparece ningún bloqueador nuevo: sigue habiendo un collider por punto, como
        /// siempre, solo que ahora el del punto delineado está sobre su pieza.
        /// </para>
        /// <para>
        /// <b>Y se mueve lo mínimo: al punto de la pieza más cercano a donde ya estaba.</b> No
        /// al centro de la pieza, que fue el segundo intento y también lo tumbó PlayMode: la
        /// rueda se iba a su centro, que en la vista de llantas cae <i>detrás del checklist</i>,
        /// y el clic chocaba con la fila «Frenos» en vez de llegar a la rueda. Donde están hoy
        /// los mounts no es casual —se ajustaron a ojo contra capturas, y la rueda subió de .13
        /// a .18 justo para no salirse por abajo del encuadre—, así que ese ajuste se respeta:
        /// si el mount ya cae dentro de la pieza, la esfera no se mueve ni un milímetro.
        /// </para>
        /// </summary>
        private static void MoveToPart(WorldObjectTaskEntity point, Transform part)
        {
            // Donde lo dejo AttachInspectionPoints: el sitio ajustado a mano.
            Vector3 tuned = point.transform.position;

            // worldPositionStays para no heredar la escala de la pieza: el FBX no trae las
            // cuatro a la misma, y con false la esfera cambiaria de tamaño segun la pieza.
            point.transform.SetParent(part, worldPositionStays: true);

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
                point.transform.position = renderer.bounds.ClosestPoint(tuned);
        }

        /// <summary>
        /// Busca la pieza por nombre en toda la jerarquía del vehículo, no solo entre sus hijos
        /// directos: un FBX puede reagrupar sus piezas bajo un nulo intermedio al reexportarlo,
        /// y comparar solo el primer nivel dejaría de encontrarlas sin decir por qué.
        /// </summary>
        private static Transform FindPart(Transform model, string partName)
        {
            foreach (Transform candidate in model.GetComponentsInChildren<Transform>(true))
                if (candidate != model && candidate.name == partName)
                    return candidate;

            Debug.LogWarning(
                $"[Module01] '{model.name}' no tiene la pieza '{partName}' que declara su " +
                "perfil; ese punto se queda con el signo de exclamacion.", model);
            return null;
        }


        /// <summary>
        /// Cuelga cada punto de inspección del mount correspondiente del vehículo encendido.
        /// <para>
        /// Hace falta en runtime y no basta con dejarlo hecho en la escena, porque hay
        /// <b>una sola entidad por punto</b> para los dos vehículos: sus ids no llevan el
        /// vehículo —<c>module01.cockpit.point_brakes</c>—, que es justo lo que permite que los
        /// dos perfiles compartan las tres zonas y que no haga falta ni un término nuevo de I2.
        /// Con los dos vehículos presentes en la escena, dejarlo cableado en el editor repartía
        /// los cinco puntos entre las dos mallas a capricho de en cuál cayera primero la
        /// búsqueda por nombre.
        /// </para>
        /// </summary>
        private void AttachInspectionPoints(Entry entry, VehicleProfileSO profile)
        {
            if (entry?.Model == null)
                return;

            foreach (WorldObjectTaskEntity point in _inspectionPoints)
            {
                if (point == null || string.IsNullOrEmpty(point.ItemId))
                    continue;

                // Un punto que este vehiculo no revisa —la plataforma con la bici— se apaga:
                // dejarlo colgado del otro vehiculo lo dejaria a la vista y clicable.
                bool own = profile.Inspects(point.ItemId);
                point.gameObject.SetActive(own);
                if (!own)
                    continue;

                Transform mount = entry.Model.transform.Find("mount_" + point.ItemId);
                if (mount == null)
                {
                    Debug.LogWarning(
                        $"[Module01] '{entry.Model.name}' no tiene 'mount_{point.ItemId}'; " +
                        "ese punto se queda donde estaba.", this);
                    continue;
                }

                point.transform.SetParent(mount, false);
                point.transform.localPosition = Vector3.zero;
            }
        }

        /// <summary>
        /// Gira el vehículo para presentar una zona. El aprendiz se queda quieto: lo que se
        /// mueve es el vehículo, y los marcadores que cuelgan de su malla viajan con él.
        /// <para>
        /// El cero no es "de frente" por definición: depende de cómo venga orientada la malla
        /// del FBX. Por eso el ángulo vive en <see cref="VehicleProfileSO"/> como dato.
        /// </para>
        /// </summary>
        public void FaceYaw(float yaw, bool immediate = false)
        {
            TargetYaw = yaw;
            Transform model = ActiveModel;
            if (model == null)
                return;

            if (_turning != null)
            {
                StopCoroutine(_turning);
                _turning = null;
            }

            if (immediate || _turnSeconds <= 0f || !Application.isPlaying || !isActiveAndEnabled)
            {
                model.localRotation = Quaternion.Euler(0f, yaw, 0f);
                return;
            }

            _turning = StartCoroutine(Turn(model, yaw));
        }

        private IEnumerator Turn(Transform model, float yaw)
        {
            Quaternion from = model.localRotation;
            Quaternion to = Quaternion.Euler(0f, yaw, 0f);
            float elapsed = 0f;
            while (elapsed < _turnSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _turnSeconds);
                model.localRotation = Quaternion.Slerp(from, to, t);
                yield return null;
            }
            model.localRotation = to;
            _turning = null;
        }

        private static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();

        /// <summary>Solo para tests: llena la tabla sin pasar por el inspector.</summary>
        public void ConfigureForTests(params (string VehicleContextValue, GameObject Model)[] entries)
        {
            _vehicles = new List<Entry>();
            foreach ((string value, GameObject model) in entries)
                _vehicles.Add(new Entry { VehicleContextValue = value, Model = model });
        }

        /// <summary>Solo para tests y para el montador de escena.</summary>
        public void ConfigurePointsForTests(params WorldObjectTaskEntity[] points)
        {
            _inspectionPoints = new List<WorldObjectTaskEntity>(points);
        }
    }
}
