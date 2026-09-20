using System;
using UnityEngine;

/// <summary>
/// A base class for creating event channels that facilitate event-driven communication between components.
/// To create a new event channel, derive a class from <see cref="BaseEventChannelSO{T}"/>.
/// </summary>
/// <typeparam name="T">The type of data transmitted by this event channel.</typeparam>
/// /// <note>
/// The <see cref="BaseEventChannelSOEditor"/> class is used to draw the `BaseEventChannelSO` class in the Inspector using a custom window.
/// </note>
public class BaseEventChannelSO<T> : ScriptableObject
{

    [Tooltip("The action to perform; Listeners subscribe to this UnityAction")]
    public Action<T> OnEventRaised;

    /// <summary>
    /// Raises the event and notifies all subscribed listeners.
    /// </summary>
    /// <param name="parameter">The data associated with this event.</param>
    public void RaiseEvent(T parameter)
    {
        OnEventRaised?.Invoke(parameter);
    }
}
