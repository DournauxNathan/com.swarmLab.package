using System.Collections.Generic;
using SwarmLab;
using UnityEngine;

namespace Runtime.Rules
{
    [System.Serializable]
    public class SeparationRule : SteeringRule
    {
        public override Vector3 CalculateForce(Entity entity, List<Entity> neighbors)
        {
            return Vector3.zero;
        }
    }
}