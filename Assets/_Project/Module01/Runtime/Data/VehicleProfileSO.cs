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

        /// <summary>
        /// Qué pieza de la malla delinea cada punto de inspección, por nombre.
        /// <para>
        /// Es dato y por vehículo porque cada uno despieza su FBX a su manera, y no coinciden
        /// ni en los nombres ni en el corte: la e-bike trae <c>manubrio</c>, <c>marco</c>,
        /// <c>pedales</c> y <c>rueda_trasera</c>; el patinete, <c>control_de_direccion</c> con
        /// <c>freno</c>, <c>guardabarros</c> y <c>rueda_frontal</c> dentro, y <c>cuerpo</c> con
        /// <c>rueda_tracera</c> y <c>soporte</c>. Y es un <b>nombre</b>, no una referencia a la
        /// escena, porque hay una sola entidad por punto para los dos vehículos y la pieza se
        /// busca en el que esté encendido.
        /// </para>
        /// <para>
        /// Lo que no esté aquí no es un fallo: la batería va dentro del chasis en los dos y el
        /// faro no existe como pieza en ninguno. Esos puntos caen al signo de exclamación de
        /// <see cref="InspectionMarker"/>, que es lo que había antes de esto.
        /// </para>
        /// </summary>
        [Serializable]
        private class PartBinding
        {
            [Tooltip("Id del punto, p. ej. point_steering.")]
            public string ItemId;

            [Tooltip("Nombre de la pieza dentro de la malla, p. ej. manubrio.")]
            public string PartName;
        }

        [Tooltip("Pieza que delinea cada punto. Vacío = ese punto se queda con el signo.")]
        [SerializeField] private List<PartBinding> _partBindings = new List<PartBinding>();

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

        /// <summary>
        /// Nombre de la pieza que delinea ese punto, o cadena vacía si no tiene ninguna.
        /// Vacío es la respuesta normal, no un error: ver <see cref="PartBinding"/>.
        /// </summary>
        public string PartNameFor(string itemId)
        {
            string wanted = Normalize(itemId);
            if (wanted.Length == 0)
                return string.Empty;

            foreach (PartBinding binding in _partBindings)
            {
                if (binding == null || Normalize(binding.ItemId) != wanted)
                    continue;
                return string.IsNullOrWhiteSpace(binding.PartName)
                    ? string.Empty
                    : binding.PartName.Trim();
            }
            return string.Empty;
        }

        /// <summary>Usado por el montador de escena y por los tests.</summary>
        public void ConfigurePartBindings(IEnumerable<(string ItemId, string PartName)> bindings)
        {
            _partBindings = new List<PartBinding>();
            foreach ((string itemId, string partName) in bindings)
                _partBindings.Add(new PartBinding { ItemId = itemId, PartName = partName });
        }

        private static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();

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
