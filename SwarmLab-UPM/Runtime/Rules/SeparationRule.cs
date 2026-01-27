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
            if (neighbors.Count == 0) return Vector3.zero;

            Vector3 separationForce = Vector3.zero;

            foreach (var neighbor in neighbors)
            {
                float weight = GetWeightFor(neighbor.Species);
                if (weight <= 0.001f) continue;

                Vector3 direction = entity.Position - neighbor.Position;
                float distanceSq = direction.sqrMagnitude;

                // Avoid division by zero
                if (distanceSq > 0.0001f)
                {
                    // Weight the repulsion by species importance
                    // Inverse square law is common for separation
                    separationForce += (direction.normalized / distanceSq) * weight;
                }
            }

            return separationForce.normalized;
        }
    }
}
