using System;
using UnityEngine;

namespace RideSafe.TaskSequence
{
    /// <summary>
    /// A scene object a sequence can address by <see cref="EntityId"/>.
    /// <para>
    /// Implementations describe a TYPE of interaction, never a specific piece of
    /// content: there is no "HelmetEntity". Content identity lives in the id.
    /// </para>
    /// </summary>
    public interface ITaskEntity
    {
        EntityId Id { get; }

        /// <summary>Anchor used by presentation (highlights, hint lines). Never mutated by the core.</summary>
        Transform Transform { get; }

        /// <summary>False while the entity exists but cannot take part (hidden, disabled, locked).</summary>
        bool IsAvailable { get; }
    }

    /// <summary>Entity that reports pointer/gaze focus. Backs EntityFocusedValidator.</summary>
    public interface IFocusableTaskEntity : ITaskEntity
    {
        bool IsFocused { get; }
        event Action<ITaskEntity, bool> FocusChanged;
    }

    /// <summary>Entity that can be selected and deselected. Backs Entity(De)SelectedValidator.</summary>
    public interface ISelectableTaskEntity : ITaskEntity
    {
        bool IsSelected { get; }
        event Action<ITaskEntity, bool> SelectionChanged;
    }

    /// <summary>Entity that can commit its current selection. Backs SelectionConfirmedValidator.</summary>
    public interface IConfirmableTaskEntity : ITaskEntity
    {
        event Action<ITaskEntity> SelectionConfirmed;
    }
}
