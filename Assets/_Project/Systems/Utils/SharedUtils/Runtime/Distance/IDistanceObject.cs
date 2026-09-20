
using UnityEngine;

public interface IDistanceObject
{
    public bool IsActive { get; }
    public void Select(Transform origin, Vector3 offset);
    public void Deselect();
}
