using Sirenix.OdinInspector;
using UnityEngine;

namespace Cachacos
{
    public class FollowRotation : BaseFollowRotation
    {
        private void Update() => Rotate();
    }
}