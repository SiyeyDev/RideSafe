using Sirenix.OdinInspector;
using UnityEngine;

namespace Cachacos
{
    public class FollowRotationFixed : BaseFollowRotation
    {
        private void FixedUpdate() => Rotate();
    }
}