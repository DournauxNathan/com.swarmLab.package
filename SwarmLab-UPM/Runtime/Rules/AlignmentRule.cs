using System.Collections.Generic;
using SwarmLab;
using UnityEngine;

namespace Runtime.Rules
{
    [System.Serializable]
    public class AlignmentRule : SteeringRule
    {
        [Tooltip("Distance to see neighbors (neighbor_radius)")]
        public float neighborRadius = 100f;

        [Tooltip("Maximum steering force")]
        public float maxForce = 2f;

        public override Vector3 CalculateForce(Entity entity, List<Entity> neighbors)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;

            foreach (var neighbor in neighbors)
            {
                float distance = Vector3.Distance(entity.Position, neighbor.Position);

                // if 0 < distance < neighbor_radius:
                if (distance > 0 && distance < neighborRadius)
                {
                    float weight = GetWeightFor(neighbor.Species);
                    if (weight <= 0.001f) continue;

                    sum += neighbor.Velocity * weight;
                    count++;
                }
            }

            if (count > 0)
            {
                sum /= count;
                
                // -- Reynolds Steering Implementation --
                
                // 1. Normalize desired velocity
                sum.Normalize();
                
                // 2. Scale to max speed
                sum *= entity.Species.maxSpeed;
                
                // 3. Calculate steering force
                Vector3 steer = sum - entity.Velocity;
                
                // 4. Limit the force
                steer = Vector3.ClampMagnitude(steer, maxForce);
                
                return steer;
            }

            return Vector3.zero;
        }
    }
}