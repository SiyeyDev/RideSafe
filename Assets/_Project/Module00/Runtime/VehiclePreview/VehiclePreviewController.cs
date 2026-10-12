using System;
using System.Collections.Generic;
using UnityEngine;

namespace RideSafe.Module00
{
    /// <summary>
    /// Presents the vehicle the learner is choosing (screen 04): the chosen one eases into a
    /// slightly lifted pose on its platform and turns slowly; the one left behind stops at
    /// once and eases back to the exact pose it had when the scene loaded.
    /// <para>
    /// Knows vehicle ids only. Who decides the selection (the Vehicle panel, a test, a later
    /// 3D selection) calls <see cref="Select"/>; this class never talks to the tutorial.
    /// Vehicles are never deactivated, so nothing pops. One Update drives every vehicle.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/Module 00/Vehicle Preview Controller")]
    public class VehiclePreviewController : MonoBehaviour
    {
        [Serializable]
        public class Vehicle
        {
            [Tooltip("Id used by the Vehicle panel, e.g. ebike or escooter.")]
            [SerializeField] private string _id;
            [SerializeField] private Transform _model;
            [Tooltip("Optional platform effect under this vehicle, shown while it is selected.")]
            [SerializeField] private GameObject _platform;

            public Vehicle() { }

            public Vehicle(string id, Transform model, GameObject platform)
            {
                _id = id;
                _model = model;
                _platform = platform;
            }

            public string Id => _id;
            public Transform Model => _model;
            public GameObject Platform => _platform;
        }

        private enum Phase
        {
            Resting,
            Entering,
            Presenting,
            Leaving
        }

        /// <summary>Runtime state of one vehicle. Lives here, never in an asset.</summary>
        private sealed class State
        {
            public Vehicle Config;
            public Vector3 RestPosition;
            public Quaternion RestRotation;
            public Vector3 RestScale;
            public Vector3 FromPosition;
            public Quaternion FromRotation;
            public Vector3 FromScale;
            public ParticleSystem[] Particles;
            public bool PlatformShown;
            public Phase Phase;
            public float Time;
            public float Yaw;
        }

        [SerializeField] private List<Vehicle> _vehicles = new List<Vehicle>();

        [Header("Motion")]
        [Tooltip("Seconds to ease into and out of the presentation pose.")]
        [SerializeField, Min(0.05f)] private float _blendSeconds = 0.8f;
        [Tooltip("Turntable speed of the selected vehicle.")]
        [SerializeField] private float _degreesPerSecond = 20f;
        [Tooltip("Metres the selected vehicle rises above its rest pose.")]
        [SerializeField, Min(0f)] private float _presentLift = 0.04f;
        [Tooltip("Scale of the selected vehicle relative to its rest scale.")]
        [SerializeField, Min(0.1f)] private float _presentScale = 1f;

        private readonly List<State> _states = new List<State>();

        public string SelectedId { get; private set; }

        /// <summary>Captures every rest pose and turns the platforms off. Runs once, on Awake.</summary>
        public void Initialize()
        {
            if (_states.Count > 0)
                return;

            foreach (Vehicle vehicle in _vehicles)
            {
                if (vehicle == null || vehicle.Model == null || string.IsNullOrEmpty(vehicle.Id))
                {
                    Debug.LogWarning("[Module00] Vehicle preview entry without id or model is ignored.", this);
                    continue;
                }

                Transform model = vehicle.Model;
                State state = new State
                {
                    Config = vehicle,
                    RestPosition = model.localPosition,
                    RestRotation = model.localRotation,
                    RestScale = model.localScale,
                    Particles = vehicle.Platform != null
                        ? vehicle.Platform.GetComponentsInChildren<ParticleSystem>(true)
                        : new ParticleSystem[0],
                    Phase = Phase.Resting
                };
                if (vehicle.Platform != null)
                    vehicle.Platform.SetActive(false);
                _states.Add(state);
            }
        }

        /// <summary>Presents <paramref name="vehicleId"/> and sends the previous one back.</summary>
        public void Select(string vehicleId)
        {
            Initialize();
            State next = Find(vehicleId);
            if (next == null)
            {
                Debug.LogWarning("[Module00] Vehicle preview has no vehicle '" + vehicleId + "'.", this);
                return;
            }
            if (vehicleId == SelectedId)
                return;

            State previous = Find(SelectedId);
            if (previous != null)
                BeginLeave(previous);

            SelectedId = vehicleId;
            BeginEnter(next);
        }

        /// <summary>Every vehicle back to rest, platforms fading out. E.g. when leaving the Vehicle screen.</summary>
        public void ClearSelection()
        {
            Initialize();
            foreach (State state in _states)
            {
                if (state.Phase == Phase.Entering || state.Phase == Phase.Presenting)
                    BeginLeave(state);
            }
            SelectedId = null;
        }

        public bool IsRotating(string vehicleId)
        {
            State state = Find(vehicleId);
            return state != null && (state.Phase == Phase.Entering || state.Phase == Phase.Presenting);
        }

        public bool IsPlatformShown(string vehicleId)
        {
            State state = Find(vehicleId);
            return state != null && state.PlatformShown;
        }

        /// <summary>Advances every vehicle. Driven by Update; public so it can be stepped deterministically.</summary>
        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _states.Count; i++)
            {
                State state = _states[i];
                switch (state.Phase)
                {
                    case Phase.Entering:
                    case Phase.Presenting:
                        TickPresenting(state, deltaTime);
                        break;
                    case Phase.Leaving:
                        TickLeaving(state, deltaTime);
                        break;
                }
            }
        }

        protected virtual void Awake() => Initialize();

        protected virtual void Update() => Tick(Time.deltaTime);

        private void BeginEnter(State state)
        {
            Snapshot(state);
            state.Phase = Phase.Entering;
            state.Time = 0f;
            state.Yaw = 0f;
            ShowPlatform(state, true);
        }

        private void BeginLeave(State state)
        {
            Snapshot(state);
            state.Phase = Phase.Leaving;
            state.Time = 0f;
            ShowPlatform(state, false);
        }

        private void TickPresenting(State state, float deltaTime)
        {
            state.Time += deltaTime;
            float weight = Ease(state.Time / _blendSeconds);
            if (state.Phase == Phase.Entering && weight >= 1f)
                state.Phase = Phase.Presenting;

            // The turntable spins up with the blend, so the start has no jolt either.
            state.Yaw = Mathf.Repeat(state.Yaw + _degreesPerSecond * weight * deltaTime, 360f);

            Transform model = state.Config.Model;
            Vector3 presentedPosition = state.RestPosition + Vector3.up * _presentLift;
            Quaternion presentedRotation = Quaternion.AngleAxis(state.Yaw, Vector3.up) * state.RestRotation;
            Vector3 presentedScale = state.RestScale * _presentScale;
            model.localPosition = Vector3.Lerp(state.FromPosition, presentedPosition, weight);
            model.localRotation = Quaternion.Slerp(state.FromRotation, presentedRotation, weight);
            model.localScale = Vector3.Lerp(state.FromScale, presentedScale, weight);
        }

        private void TickLeaving(State state, float deltaTime)
        {
            state.Time += deltaTime;
            float weight = Ease(state.Time / _blendSeconds);

            Transform model = state.Config.Model;
            model.localPosition = Vector3.Lerp(state.FromPosition, state.RestPosition, weight);
            model.localRotation = Quaternion.Slerp(state.FromRotation, state.RestRotation, weight);
            model.localScale = Vector3.Lerp(state.FromScale, state.RestScale, weight);

            if (weight >= 1f)
            {
                // Exact rest pose, no drift left from interpolation.
                model.localPosition = state.RestPosition;
                model.localRotation = state.RestRotation;
                model.localScale = state.RestScale;
                state.Phase = Phase.Resting;
            }
        }

        private static void Snapshot(State state)
        {
            Transform model = state.Config.Model;
            state.FromPosition = model.localPosition;
            state.FromRotation = model.localRotation;
            state.FromScale = model.localScale;
        }

        /// <summary>
        /// Shown: active and emitting. Hidden: emission stops and live particles finish their
        /// life, so the effect fades instead of vanishing.
        /// </summary>
        private static void ShowPlatform(State state, bool shown)
        {
            state.PlatformShown = shown;
            GameObject platform = state.Config.Platform;
            if (platform == null)
                return;

            if (shown)
            {
                platform.SetActive(true);
                for (int i = 0; i < state.Particles.Length; i++)
                    state.Particles[i].Play(false);
            }
            else
            {
                for (int i = 0; i < state.Particles.Length; i++)
                    state.Particles[i].Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private State Find(string vehicleId)
        {
            if (string.IsNullOrEmpty(vehicleId))
                return null;
            for (int i = 0; i < _states.Count; i++)
            {
                if (_states[i].Config.Id == vehicleId)
                    return _states[i];
            }
            return null;
        }

        private static float Ease(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
    }
}
