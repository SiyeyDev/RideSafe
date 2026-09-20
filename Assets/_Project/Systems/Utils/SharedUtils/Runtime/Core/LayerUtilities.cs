
using UnityEngine;

public static class LayerUtilities
{
    private static bool ContainsLayer(this LayerMask layerMask, int layer) => (1 << layer & layerMask) != 0;
    public static bool IsSameLayer(this LayerMask layerMask, int intLayer) => layerMask.ContainsLayer(intLayer);
}

