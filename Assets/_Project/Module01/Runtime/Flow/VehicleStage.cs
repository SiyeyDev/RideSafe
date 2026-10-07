using System;
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

        /// <summary>
        /// Enciende la malla del vehículo pedido y apaga las demás. Si no hay malla para
        /// ese valor avisa y deja el garaje como estaba: apagarlo todo dejaría al
        /// aprendiz inspeccionando el aire.
        /// </summary>
        public void Show(string vehicleContextValue)
        {
            string wanted = Normalize(vehicleContextValue);
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
                    $"[Module01] El escenario no tiene malla para el vehiculo '{vehicleContextValue}'.", this);
                return;
            }

            foreach (Entry entry in _vehicles)
                if (entry?.Model != null)
                    entry.Model.SetActive(entry == match);
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
    }
}
