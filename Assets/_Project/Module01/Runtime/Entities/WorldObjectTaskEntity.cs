using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using RideSafe.TaskSequence;
// Unity 6 introduce UnityEngine.EntityId, que choca con el de TaskSequence.
using EntityId = RideSafe.TaskSequence.EntityId;

namespace RideSafe.Module01
{
    /// <summary>
    /// Entidad de tarea sobre un objeto 3D con collider: props de protección personal
    /// y puntos de inspección del vehículo.
    /// <para>
    /// No hace raycast propio. El EventSystem ya lo hace: en modo PC a través del
    /// <c>PhysicsRaycaster</c> de la cámara, y en VR a través del puntero de AutoHand.
    /// Este componente solo traduce esos eventos al vocabulario del tutorial, igual
    /// que <c>AutoHandUITaskEntity</c> hace para uGUI.
    /// </para>
    /// <para>
    /// La selección no la decide el click: la decide el inspector tras la confirmación.
    /// Por eso <see cref="SetSelected"/> es público y el click solo confirma.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/Module 01/World Object Task Entity")]
    [RequireComponent(typeof(Collider))]
    public class WorldObjectTaskEntity : TaskEntity,
        IFocusableTaskEntity, ISelectableTaskEntity, IConfirmableTaskEntity,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Tooltip("Id del elemento en Module01CatalogSO. Vacío para puntos sin catálogo.")]
        [SerializeField] private string _itemId;

        // Un puntero por mano en VR; uno solo con el mouse. El foco vive mientras
        // quede alguno dentro, así que salir con una mano no apaga el de la otra.
        private readonly HashSet<PointerEventData> _pointersInside = new HashSet<PointerEventData>();
        private bool _selected;

        public event Action<ITaskEntity, bool> FocusChanged;
        public event Action<ITaskEntity, bool> SelectionChanged;
        public event Action<ITaskEntity> SelectionConfirmed;

        public string ItemId => string.IsNullOrWhiteSpace(_itemId) ? string.Empty : _itemId.Trim();
        public bool IsFocused => _pointersInside.Count > 0;
        public bool IsSelected => _selected;

        /// <summary>
        /// Configuración en runtime, para que el greybox y el modelo real se cableen
        /// desde el perfil del vehículo y nunca como override de escena.
        /// Llamar con el objeto inactivo y luego <see cref="TaskEntity.EnsureRegistered"/>.
        /// </summary>
        public void Configure(EntityId id, string itemId)
        {
            SetEntityId(id);
            _itemId = itemId;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            bool was = IsFocused;
            _pointersInside.Add(eventData);
            if (!was && IsFocused)
                FocusChanged?.Invoke(this, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            bool was = IsFocused;
            _pointersInside.Remove(eventData);
            if (was && !IsFocused)
                FocusChanged?.Invoke(this, false);
        }

        /// <summary>Apuntar y hacer click pide inspeccionar; no selecciona por sí solo.</summary>
        public void OnPointerClick(PointerEventData eventData) => Confirm();

        public void SetSelected(bool value)
        {
            if (_selected == value)
                return;
            _selected = value;
            SelectionChanged?.Invoke(this, _selected);
        }

        public void Confirm() => SelectionConfirmed?.Invoke(this);

        protected override void OnDestroy()
        {
            _pointersInside.Clear();
            base.OnDestroy();
        }
    }
}
