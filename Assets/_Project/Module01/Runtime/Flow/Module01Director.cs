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
    /// Las zonas del vehículo salen del <see cref="VehicleProfileSO"/>, no de una lista
    /// fija: cambiar de e-bike a scooter es cambiar el perfil, sin tocar esto.
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
        [SerializeField] private VehicleProfileSO _vehicle;
        [SerializeField] private GuidedTour _tour;
        [SerializeField] private ZoneRunner _runner;
        [SerializeField] private ReportPresenter _report;

        [Tooltip("Arranca solo al entrar en Play. Apagalo si lo dispara el modulo 0.")]
        [SerializeField] private bool _beginOnStart;

        private readonly List<ZoneSO> _personalZones = new List<ZoneSO>();
        private readonly List<ZoneSO> _vehicleZones = new List<ZoneSO>();
        private int _index = -1;
        private bool _inVehicleSection;
        private bool _subscribed;

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
            if (_vehicle != null)
            {
                foreach (ZoneSO zone in _vehicle.InspectionZones)
                    if (zone != null)
                        _vehicleZones.Add(zone);
            }

            IsFinished = false;
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

        private void HandleZoneCompleted(ZoneSO zone) => Advance();

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

        private void EnterZone(ZoneSO zone)
        {
            CurrentZone = zone;
            _tour?.TryGoTo(zone.ZoneId);
            _runner.Begin(zone);
        }

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

        /// <summary>Solo para tests: inyecta las dependencias sin pasar por el inspector.</summary>
        public void ConfigureForTests(Module01CatalogSO catalog, VehicleProfileSO vehicle,
                                      GuidedTour tour, ZoneRunner runner, ReportPresenter report)
        {
            _catalog = catalog;
            _vehicle = vehicle;
            _tour = tour;
            _runner = runner;
            _report = report;
        }
    }
}
