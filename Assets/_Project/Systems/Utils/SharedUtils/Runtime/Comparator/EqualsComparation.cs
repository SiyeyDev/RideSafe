using UnityEngine;

namespace Comparation
{
    [CreateAssetMenu(fileName = "SO_EqualComparation", menuName = "System/Comparation/Equal", order = 51)]
    public class EqualsComparation : BaseComparation
    {
        public override bool IsValid(object value1, object value2)
        {
            if (value1 == null && value2 == null)
                return true;
            if (value1 == null || value2 == null)
                return false;
            return value1.Equals(value2);
        }
    }
}
