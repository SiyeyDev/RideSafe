using System.Collections.Generic;
using UnityEngine;

namespace RideSafe.Module02
{
    /// <summary>Práctica: feedback + volver al checkpoint. Prueba: sin ayudas, solo aprobado / no aprobado.</summary>
    public enum RunMode { Practice, Test }

    /// <summary>
    /// Último checkpoint alcanzado y zonas falladas. Sin escena, para poder probarlo en EditMode.
    /// </summary>
    public class CheckpointTracker
    {
        private readonly HashSet<string> _failed = new HashSet<string>();

        public RunMode Mode { get; }

        /// <summary>Null mientras no se ha alcanzado ninguno: se vuelve al inicio de la ruta.</summary>
        public string LastId { get; private set; }

        public Pose LastPose { get; private set; }

        /// <summary>Zonas falladas al menos una vez.</summary>
        public IReadOnlyCollection<string> FailedZones => _failed;

        public bool Passed => _failed.Count == 0;

        public CheckpointTracker(RunMode mode, Pose routeStart)
        {
            Mode = mode;
            LastPose = routeStart;
        }

        public void Reach(string checkpointId, Pose pose)
        {
            LastId = checkpointId;
            LastPose = pose;
        }

        /// <summary>Apunta el fallo. Devuelve true si toca presentarlo y reaparecer (solo en práctica).</summary>
        public bool Fail(string zoneId)
        {
            _failed.Add(zoneId);
            return Mode == RunMode.Practice;
        }
    }
}
