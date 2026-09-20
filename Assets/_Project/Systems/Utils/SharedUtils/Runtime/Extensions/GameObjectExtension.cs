using UnityEngine;

public static class GameObjectExtension 
{
    public static T GetOrAddComponent<T>(this Component monoBehaviour) where T : Component
    { 
        if(monoBehaviour.TryGetComponent(out T compoment))
            return compoment;
        return monoBehaviour.gameObject.AddComponent<T>();
    }
}
