using System;
using System.Collections.Generic;
using UnityEngine;

namespace RideSafe.Module02
{
    /// <summary>
    /// Checkpoints de la ruta. El id de cada uno es el nombre de su Transform; van unos segundos
    /// de recorrido antes de cada zona de conflicto (reunión 2026-11-11).
    /// <para>
    /// Práctica: <see cref="Fail"/> lanza <see cref="Failed"/> (congelar antes del choque, vídeo) y
    /// al terminar quien presenta llama a <see cref="Respawn"/>. Prueba: solo se apunta el fallo.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/Module 02/Route Checkpoints")]
    public class RouteCheckpoints : MonoBehaviour
    {
        [SerializeField] private RunMode mode = RunMode.Practice;
        [Tooltip("Vacío = este mismo Transform.")]
        [SerializeField] private Transform routeStart;
        [SerializeField] private Transform[] checkpoints = new Transform[0];

        /// <summary>Zona fallada en práctica: congelar, vídeo y luego <see cref="Respawn"/>.</summary>
        public event Action<string> Failed;

        public event Action<Pose> RespawnRequested;

        private CheckpointTracker _tracker;

        // Perezoso para que funcione también sin Awake (EditMode).
        private CheckpointTracker Tracker =>
            _tracker ??= new CheckpointTracker(mode, ToPose(routeStart != null ? routeStart : transform));

        public RunMode Mode => mode;
        public bool Passed => Tracker.Passed;
        public IReadOnlyCollection<string> FailedZones => Tracker.FailedZones;

        public void Reach(string checkpointId)
        {
            var t = Array.Find(checkpoints, c => c != null && c.name == checkpointId);
            if (t == null)
            {
                Debug.LogWarning($"[RouteCheckpoints] No hay checkpoint «{checkpointId}».", this);
                return;
            }
            Tracker.Reach(checkpointId, ToPose(t));
        }

        public void Fail(string zoneId)
        {
            if (Tracker.Fail(zoneId)) Failed?.Invoke(zoneId);
        }

        /// <summary>Pide volver al último checkpoint (o al inicio si no se alcanzó ninguno).</summary>
        public void Respawn() => RespawnRequested?.Invoke(Tracker.LastPose);

        private static Pose ToPose(Transform t) => new Pose(t.position, t.rotation);
    }
}
