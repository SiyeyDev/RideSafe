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
            _inspector.Presenting += HideMarkersWhileInspecting;
            _inspector.Dismissed += ShowMarkersAgain;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _inspector == null)
                return;
            _inspector.Accepted -= HandleAccepted;
            _inspector.Declined -= HandleDeclined;
            _inspector.Presenting -= HideMarkersWhileInspecting;
            _inspector.Dismissed -= ShowMarkersAgain;
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
                {
                    entity.SelectionConfirmed -= HandleEntityConfirmed;
                    Mark(entity, highlighted: false);
                }
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
                // Solo laten y se encienden en naranja los puntos de la zona en curso: si
                // latieran todos, ninguno diria "este".
                Mark(entity, highlighted: true);
            }
        }

        private void HideMarkersWhileInspecting(SafetyItemSO _) => ObscureMarkers(true);

        private void ShowMarkersAgain() => ObscureMarkers(false);

        /// <summary>
        /// Aparta los signos de la zona mientras hay un objeto en el pedestal. El popup de
        /// confirmación ocupa el centro y los marcadores viven en la capa de overlay, así que
        /// sin esto se le cruzan por encima: medido, uno se montaba sobre la pregunta.
        /// </summary>
        private void ObscureMarkers(bool value)
        {
            foreach (WorldObjectTaskEntity entity in _entities)
            {
                if (entity == null)
                    continue;
                foreach (IInspectionCue cue in entity.GetComponents<IInspectionCue>())
                    cue.SetObscured(value);
            }
        }

        /// <summary>
        /// Pone el punto en su estado, avisando por igual al signo de exclamación y al contorno
        /// de la pieza: cuál de los dos hay depende del vehículo, y eso no es cosa del
        /// recorrido. Silencioso cuando no hay ninguno: los elementos de protección personal no
        /// llevan aviso, y no es un fallo.
        /// </summary>
        private static void Mark(WorldObjectTaskEntity entity, bool highlighted)
        {
            if (entity == null)
                return;
            foreach (IInspectionCue cue in entity.GetComponents<IInspectionCue>())
            {
                // Se limpia el "resuelto" tanto al entrar como al salir de la zona: un punto
                // marcado en verde de una zona anterior se queda visible y, al girar el
                // vehiculo, acaba cruzandose con el punto que toca revisar ahora. El aviso
                // habla de la zona en curso y de ninguna otra.
                cue.SetHighlighted(highlighted);
                cue.SetResolved(false);
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
                    // Con la pieza real cuando la hay: es lo que el pedestal ensena en vez de
                    // la esfera blanca que se fabricaba para los puntos sin modelo.
                    _inspector.Present(item, world.GetComponent<InspectionHighlight>()?.Part);
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

            foreach (WorldObjectTaskEntity entity in _entities)
            {
                if (entity == null || entity.ItemId != item.ItemId)
                    continue;
                foreach (IInspectionCue cue in entity.GetComponents<IInspectionCue>())
                    cue.SetResolved(true);
            }

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
