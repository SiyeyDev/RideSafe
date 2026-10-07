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

        [Tooltip("Segundos que tarda el vehículo en girar de una zona a otra.")]
        [SerializeField, Min(0f)] private float _turnSeconds = .8f;

        private Entry _active;
        private Coroutine _turning;

        /// <summary>Último giro pedido, en grados. Lo que se está mostrando o se va a mostrar.</summary>
        public float TargetYaw { get; private set; }

        /// <summary>La malla encendida ahora mismo, o null si no hay ninguna.</summary>
        public Transform ActiveModel => _active?.Model == null ? null : _active.Model.transform;

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

            _active = match;
            FaceYaw(TargetYaw, immediate: true);
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
    }
}
