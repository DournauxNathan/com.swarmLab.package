using System.Collections.Generic;
using SwarmLab;
using UnityEngine;

namespace Runtime.Rules
{
    [System.Serializable]
    public class AlignmentRule : SteeringRule
    {
        public override Vector3 CalculateForce(Entity entity, List<Entity> neighbors)
        {
            if (neighbors.Count == 0) return Vector3.zero;

            Vector3 averageVelocity = Vector3.zero;
            float totalWeight = 0f;

            foreach (var neighbor in neighbors)
            {
                float weight = GetWeightFor(neighbor.Species);
                if (weight <= 0.001f) continue;

                averageVelocity += neighbor.Velocity * weight;
                totalWeight += weight;
            }

            if (totalWeight <= 0.001f) return Vector3.zero;

            averageVelocity /= totalWeight;
            return (averageVelocity - entity.Velocity).normalized;
        }
    }
}
