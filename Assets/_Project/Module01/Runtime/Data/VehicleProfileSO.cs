using System;
using System.Collections.Generic;
using UnityEngine;

namespace RideSafe.Module01
{
    /// <summary>
    /// Un vehículo y sus puntos de inspección.
    /// <para>
    /// Añadir el e-scooter es crear otro de estos con <see cref="VehicleContextValue"/>
    /// distinto. Ningún código pregunta por el vehículo: lo resuelve
    /// <c>ContextRequirement</c> sobre la clave <c>vehicle</c>.
    /// </para>
    /// </summary>
    [CreateAssetMenu(fileName = "SO_VehicleProfile", menuName = "RideSafe/Module 01/Vehicle Profile")]
    public class VehicleProfileSO : ScriptableObject
    {
        [Tooltip("Valor que debe reportar la clave 'vehicle', p. ej. ebike.")]
        [SerializeField] private string _vehicleContextValue = "ebike";

        [Tooltip("Modelo que se instancia. Greybox mientras 3D entrega el definitivo.")]
        [SerializeField] private GameObject _model;

        [Tooltip("Zonas de inspección del vehículo, en orden.")]
        [SerializeField] private List<ZoneSO> _inspectionZones = new List<ZoneSO>();

        /// <summary>
        /// Cuánto gira el vehículo para enseñar cada zona. El aprendiz no se mueve: el
        /// vehículo se presenta solo, de costado para las ruedas y de frente para las luces.
        /// <para>
        /// Es dato y no código porque el giro que "se ve bien" depende de cómo esté
        /// orientada cada malla, y eso solo se acierta mirándolo en la escena.
        /// </para>
        /// </summary>
        [Serializable]
        private class ZoneView
        {
            public ZoneSO Zone;
            [Range(-180f, 180f)] public float Yaw;
        }

        [Tooltip("Giro del vehículo en cada zona, en grados. Ajustar a ojo contra la malla.")]
        [SerializeField] private List<ZoneView> _zoneViews = new List<ZoneView>();

        public string VehicleContextValue =>
            string.IsNullOrWhiteSpace(_vehicleContextValue)
                ? string.Empty
                : _vehicleContextValue.Trim().ToLowerInvariant();

        public GameObject Model => _model;
        public IReadOnlyList<ZoneSO> InspectionZones => _inspectionZones;

        /// <summary>Grados que debe girar el vehículo en esa zona. Sin declarar, no gira.</summary>
        public float YawFor(ZoneSO zone)
        {
            if (zone == null)
                return 0f;
            foreach (ZoneView view in _zoneViews)
                if (view != null && view.Zone == zone)
                    return view.Yaw;
            return 0f;
        }

        /// <summary>Usado por el generador de escena y por los tests.</summary>
        public void ConfigureZoneViews(IEnumerable<(ZoneSO Zone, float Yaw)> views)
        {
            _zoneViews = new List<ZoneView>();
            foreach ((ZoneSO zone, float yaw) in views)
                _zoneViews.Add(new ZoneView { Zone = zone, Yaw = yaw });
        }

        /// <summary>Usado por el generador de la primera pasada y por los tests.</summary>
        public void Configure(string vehicleContextValue, GameObject model, IEnumerable<ZoneSO> zones)
        {
            _vehicleContextValue = vehicleContextValue;
            _model = model;
            _inspectionZones = new List<ZoneSO>(zones);
        }
    }
}
