using System;
using System.Collections.Generic;
using Cachacos;
using UnityEngine;
using RideSafe.TaskSequence;
using EntityId = RideSafe.TaskSequence.EntityId;

namespace RideSafe.Module01
{
    /// <summary>
    /// Conecta una zona con la mecánica: cada entidad de la zona, al confirmarse,
    /// abre el inspector; aceptar registra en el libro y añade a la lista lateral.
    /// <para>
    /// La zona termina cuando el aprendiz ha resuelto todos sus elementos, no cuando
    /// acierta: aquí nada juzga. El juicio ocurre al enviar la sección.
    /// </para>
    /// <para>
    /// El libro acumula entre zonas porque la sección entera es una sola entrega;
    /// lo que se limpia al cambiar de zona es qué elementos quedan por resolver.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/Module 01/Zone Runner")]
    public class ZoneRunner : MonoBehaviour
    {
        [SerializeField] private ItemInspector _inspector;
        [SerializeField] private Module01Binding _binding;

        private readonly List<WorldObjectTaskEntity> _entities = new List<WorldObjectTaskEntity>();
        private readonly HashSet<string> _resolved = new HashSet<string>();
        private ZoneSO _zone;
        private ILocalizationProvider _localization;
        private bool _subscribed;
        private bool _listSubscribed;

        public SelectionLedger Ledger { get; } = new SelectionLedger();
        public ZoneSO CurrentZone => _zone;

        public event Action<ZoneSO> ZoneCompleted;

        private void Awake()
        {
            _localization = ServiceLocator.Instance.RequestService<ILocalizationProvider>();
            Subscribe();
        }

        private void OnEnable() => Subscribe();

        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed || _inspector == null)
                return;
            _inspector.Accepted += HandleAccepted;
            _inspector.Declined += HandleDeclined;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _inspector == null)
                return;
            _inspector.Accepted -= HandleAccepted;
            _inspector.Declined -= HandleDeclined;
            _subscribed = false;
        }

        public void Begin(ZoneSO zone)
        {
            _zone = zone;
            _resolved.Clear();
            Subscribe();
            Rebind();
            if (!_listSubscribed && _binding != null && _binding.PreparationList != null)
            {
                _binding.PreparationList.onItemRemoved.AddListener(RemoveSelection);
                _listSubscribed = true;
            }
        }

        private void RemoveSelection(string id) => Ledger.Remove(id);
        private void OnDestroy()
        {
            if (_listSubscribed && _binding != null && _binding.PreparationList != null)
                _binding.PreparationList.onItemRemoved.RemoveListener(RemoveSelection);
        }

        private void Rebind()
        {
            foreach (WorldObjectTaskEntity entity in _entities)
            {
                if (entity != null)
                    entity.SelectionConfirmed -= HandleEntityConfirmed;
            }
            _entities.Clear();

            if (_zone == null)
                return;

            TaskContextService registry = ServiceLocator.Instance.RequestService<TaskContextService>();
            if (registry == null)
                return;

            foreach (SafetyItemSO item in _zone.Items)
            {
                EntityId id = new EntityId($"module01.{_zone.ZoneId}.{item.ItemId}");
                if (!registry.TryGetEntity(id, out WorldObjectTaskEntity entity))
                    continue;
                entity.SelectionConfirmed += HandleEntityConfirmed;
                _entities.Add(entity);
            }
        }

        private void HandleEntityConfirmed(ITaskEntity entity)
        {
            if (_inspector == null || _inspector.IsPresenting || _zone == null)
                return;
            if (!(entity is WorldObjectTaskEntity world))
                return;

            foreach (SafetyItemSO item in _zone.Items)
            {
                if (item.ItemId == world.ItemId)
                {
                    _inspector.Present(item);
                    return;
                }
            }
        }

        private void HandleAccepted(SafetyItemSO item)
        {
            Ledger.Add(item.ItemId);

            if (_binding != null && _binding.PreparationList != null)
            {
                string label = _localization != null ? _localization.GetTranslation(item.NameKey) : item.NameKey;
                _binding.PreparationList.AddItem(item.ItemId, label);
            }

            MarkResolved(item);
        }

        private void HandleDeclined(SafetyItemSO item) => MarkResolved(item);

        private void MarkResolved(SafetyItemSO item)
        {
            if (_zone == null)
                return;
            if (!_resolved.Add(item.ItemId))
                return;
            if (_resolved.Count >= _zone.Items.Count)
                ZoneCompleted?.Invoke(_zone);
        }

        /// <summary>Solo para tests: inyecta las dependencias sin pasar por el inspector.</summary>
        public void ConfigureForTests(ItemInspector inspector, Module01Binding binding)
        {
            Unsubscribe();
            _inspector = inspector;
            _binding = binding;
            Subscribe();
        }
    }
}
