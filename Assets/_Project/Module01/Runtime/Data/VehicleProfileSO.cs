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

        public string VehicleContextValue =>
            string.IsNullOrWhiteSpace(_vehicleContextValue)
                ? string.Empty
                : _vehicleContextValue.Trim().ToLowerInvariant();

        public GameObject Model => _model;
        public IReadOnlyList<ZoneSO> InspectionZones => _inspectionZones;

        /// <summary>Usado por el generador de la primera pasada y por los tests.</summary>
        public void Configure(string vehicleContextValue, GameObject model, IEnumerable<ZoneSO> zones)
        {
            _vehicleContextValue = vehicleContextValue;
            _model = model;
            _inspectionZones = new List<ZoneSO>(zones);
        }
    }
}
