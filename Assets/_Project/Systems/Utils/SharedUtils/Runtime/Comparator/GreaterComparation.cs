using System;
using UnityEngine;

namespace Comparation
{
    [CreateAssetMenu(fileName = "SO_GreaterComparation", menuName = "System/Comparation/Greater", order = 51)]
    public class GreaterComparation : EqualsComparation
    {
        [SerializeField] bool _allowEqual;
        public override bool IsValid(object value1, object value2)
        {
            if (_allowEqual && base.IsValid(value1, value2))
                return true;
            if (value1.GetType() != value2.GetType())
            {
                Debug.LogWarning($"Value1: {value1} and Value2: {value2} are different types and can't be compared.");
                return false;
            }

            if (value1 is IComparable comparable1)
            {
                int result = comparable1.CompareTo(value2);
                return result > 0;
            }
            Debug.LogWarning($"Type {value1.GetType()} is not comparable.");
            return false;
        }
    }
}