using System;
using System.Collections.Generic;
using UnityEngine;

namespace RideSafe.Module01
{
    /// <summary>
    /// Encadena el módulo completo: lleva al aprendiz por las zonas de protección
    /// personal, cierra esa sección con su reporte, pasa a las zonas del vehículo y
    /// cierra la segunda.
    /// <para>
    /// Las zonas del vehículo salen del <see cref="VehicleProfileSO"/> elegido. El
    /// director tiene un perfil por vehículo y <see cref="SelectVehicle"/> decide cuál,
    /// porque con un solo perfil serializado elegir patinete recorría las zonas de la
    /// bici. Quien elige es <c>Module01Experience</c>, que es quien tiene el contexto.
    /// </para>
    /// <para>
    /// El libro se reinicia entre secciones porque son dos entregas independientes: lo
    /// que el aprendiz tomó del estante no puede contar como un componente inspeccionado.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/Module 01/Module 01 Director")]
    public class Module01Director : MonoBehaviour
    {
        [SerializeField] private Module01CatalogSO _catalog;

        [Tooltip("Un perfil por vehículo elegible. El módulo 0 decide cuál con SelectVehicle.")]
        [SerializeField] private List<VehicleProfileSO> _vehicles = new List<VehicleProfileSO>();

        [Tooltip("Opcional: enciende la malla del vehículo elegido y apaga las demás.")]
        [SerializeField] private VehicleStage _stage;

        [SerializeField] private GuidedTour _tour;
        [SerializeField] private ZoneRunner _runner;
        [SerializeField] private ReportPresenter _report;

        [Tooltip("Arranca solo al entrar en Play. Apagalo si lo dispara el modulo 0.")]
        [SerializeField] private bool _beginOnStart;
        [SerializeField] private bool _manualProgress;
        private bool _awaitingReview;
        public bool InVehicleSection => _inVehicleSection;
        public bool IsLastZone => _index == ActiveList.Count - 1;
        public event Action<ZoneSO> ZoneEntered;
        public SelectionReport PersonalReport { get; private set; }
        public SelectionReport VehicleReport { get; private set; }

        private readonly List<ZoneSO> _personalZones = new List<ZoneSO>();
        private readonly List<ZoneSO> _vehicleZones = new List<ZoneSO>();
        private int _index = -1;
        private bool _inVehicleSection;
        private bool _subscribed;
        private VehicleProfileSO _selected;

        public event Action<SelectionReport> PersonalSectionCompleted;
        public event Action<SelectionReport> VehicleSectionCompleted;
        public event Action Finished;

        public ZoneSO CurrentZone { get; private set; }
        public bool IsFinished { get; private set; }

        private void Start()
        {
            if (_beginOnStart)
                Begin();
        }

        private void OnDisable() => Unsubscribe();

        public void Begin()
        {
            if (_catalog == null || _runner == null)
            {
                Debug.LogError("[Module01] El director necesita catalogo y ZoneRunner.", this);
                return;
            }

            _personalZones.Clear();
            foreach (ZoneSO zone in _catalog.Zones)
                if (zone != null)
                    _personalZones.Add(zone);

            _vehicleZones.Clear();
            VehicleProfileSO vehicle = SelectedVehicle;
            if (vehicle != null)
            {
                foreach (ZoneSO zone in vehicle.InspectionZones)
                    if (zone != null)
                        _vehicleZones.Add(zone);
            }

            IsFinished = false;
            _awaitingReview = false;
            PersonalReport = VehicleReport = null;
            _inVehicleSection = false;
            _index = -1;
            _runner.Ledger.Clear();

            Subscribe();
            Advance();
        }

        private void Subscribe()
        {
            if (_subscribed)
                return;
            _runner.ZoneCompleted += HandleZoneCompleted;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _runner == null)
                return;
            _runner.ZoneCompleted -= HandleZoneCompleted;
            _subscribed = false;
        }

        private List<ZoneSO> ActiveList => _inVehicleSection ? _vehicleZones : _personalZones;

        private void HandleZoneCompleted(ZoneSO zone) { if (!_manualProgress) Advance(); }

        public void AdvanceZone()
        {
            if (_manualProgress && !_awaitingReview && !IsFinished) Advance();
        }

        public void PreviousZone()
        {
            if (_manualProgress && !_awaitingReview && _index > 0) { _index--; EnterZone(ActiveList[_index]); }
        }

        public void ContinueAfterReview()
        {
            if (!_awaitingReview) return;
            _awaitingReview = false;
            if (!_inVehicleSection)
            {
                _inVehicleSection = true;
                _index = -1;
                _runner.Ledger.Clear();
                Advance();
            }
            else { CurrentZone = null; IsFinished = true; Unsubscribe(); Finished?.Invoke(); }
        }

        private void Advance()
        {
            _index++;

            if (_index < ActiveList.Count)
            {
                EnterZone(ActiveList[_index]);
                return;
            }

            // Se acabó la lista en curso: cerrar la sección.
            SelectionReport report = Grade();
            if (_manualProgress)
            {
                _awaitingReview = true;
                _runner.Begin(null);
                if (_inVehicleSection) { VehicleReport = report; VehicleSectionCompleted?.Invoke(report); }
                else { PersonalReport = report; PersonalSectionCompleted?.Invoke(report); }
                return;
            }

            if (!_inVehicleSection)
            {
                PersonalSectionCompleted?.Invoke(report);
                _report?.Show(report);

                _inVehicleSection = true;
                _index = -1;
                _runner.Ledger.Clear();
                Advance();
                return;
            }

            VehicleSectionCompleted?.Invoke(report);
            _report?.Show(report);

            CurrentZone = null;
            IsFinished = true;
            Unsubscribe();
            Finished?.Invoke();
        }

        /// <summary>
        /// En las zonas personales el aprendiz viaja entre estaciones. En las del vehículo
        /// no: se queda en el punto de exhibición y es el vehículo el que gira para enseñar
        /// cada zona. Por eso solo se llama al recorrido cuando el ancla cambia de verdad —
        /// si no, cada zona del vehículo metería un fundido a negro que taparía el giro,
        /// que es justo lo que hay que ver.
        /// </summary>
        private void EnterZone(ZoneSO zone)
        {
            CurrentZone = zone;

            // El punto unico de exhibicion solo manda si la escena ya tiene su ancla. Mientras
            // no exista, cada zona del vehiculo sigue teniendo la suya y el recorrido es el de
            // siempre: asi el codigo nuevo no rompe una escena que aun no se ha remontado.
            bool singleViewpoint = _inVehicleSection && _tour != null && _tour.HasAnchor(VehicleAnchorId);
            string anchorId = singleViewpoint ? VehicleAnchorId : zone.ZoneId;
            if (_tour != null && _tour.CurrentZoneId != anchorId)
                _tour.TryGoTo(anchorId);

            if (_inVehicleSection && _stage != null)
                _stage.FaceYaw(SelectedVehicle != null ? SelectedVehicle.YawFor(zone) : 0f);

            _runner.Begin(zone);
            ZoneEntered?.Invoke(zone);
        }

        /// <summary>Ancla única desde la que se mira el vehículo, sea cual sea la zona.</summary>
        public const string VehicleAnchorId = "vehicle";

        /// <summary>
        /// Gradúa contra un catálogo que contiene solo las zonas de la sección que
        /// cierra, para que el reporte del vehículo no liste elementos personales.
        /// </summary>
        private SelectionReport Grade()
        {
            Module01CatalogSO scope = ScriptableObject.CreateInstance<Module01CatalogSO>();
            scope.ConfigureForTests(ActiveList);
            SelectionReport report = SelectionGrader.Grade(scope, _runner.Ledger);

            if (Application.isPlaying)
                Destroy(scope);
            else
                DestroyImmediate(scope);

            return report;
        }

        /// <summary>
        /// El perfil en uso: el elegido, y mientras nadie elija, el primero de la lista.
        /// Nunca queda en null teniendo perfiles, porque un vehículo sin zonas salta la
        /// sección entera sin que nadie se entere.
        /// </summary>
        public VehicleProfileSO SelectedVehicle
        {
            get
            {
                if (_selected != null)
                    return _selected;
                foreach (VehicleProfileSO profile in _vehicles)
                    if (profile != null)
                        return profile;
                return null;
            }
        }

        /// <summary>
        /// Lo que el aprendiz eligió en el panel Vehicle. Llamar antes de
        /// <see cref="Begin"/>, que es donde se arma el recorrido.
        /// </summary>
        public void SelectVehicle(string vehicleContextValue)
        {
            string wanted = Normalize(vehicleContextValue);
            foreach (VehicleProfileSO profile in _vehicles)
            {
                if (profile == null || profile.VehicleContextValue != wanted)
                    continue;

                _selected = profile;
                if (_stage != null)
                    _stage.Show(profile.VehicleContextValue);
                return;
            }

            Debug.LogError(
                $"[Module01] No hay perfil de vehiculo para '{vehicleContextValue}'. Sigue el anterior.", this);
        }

        private static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();

        /// <summary>Solo para tests: inyecta las dependencias sin pasar por el inspector.</summary>
        public void ConfigureForTests(Module01CatalogSO catalog, VehicleProfileSO vehicle,
                                      GuidedTour tour, ZoneRunner runner, ReportPresenter report)
            => ConfigureForTests(catalog, new[] { vehicle }, tour, runner, report);

        /// <summary>Solo para tests: varios perfiles, como los tendrá la escena.</summary>
        public void ConfigureForTests(Module01CatalogSO catalog, IEnumerable<VehicleProfileSO> vehicles,
                                      GuidedTour tour, ZoneRunner runner, ReportPresenter report)
        {
            _catalog = catalog;
            _vehicles = new List<VehicleProfileSO>(vehicles);
            _selected = null;
            _tour = tour;
            _runner = runner;
            _report = report;
        }

        /// <summary>Solo para tests: el escenario que enciende y apaga las mallas.</summary>
        public void ConfigureStageForTests(VehicleStage stage) => _stage = stage;
    }
}

