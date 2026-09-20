using UnityEngine;

namespace Comparation
{
    public abstract class BaseComparation : ScriptableObject
    {
        public abstract bool IsValid(object value1, object value2);
    }
}
