using System.Collections.Generic;
using SwarmLab;
using UnityEngine;

namespace Runtime.Rules
{
    [System.Serializable]
    public class SeparationRule : SteeringRule
    {
        [Tooltip("Distance at which repulsion starts (min_distance)")]
        public float minDistance = 2.5f; 

        [Tooltip("Maximum force applied for separation")]
        public float maxForce = 5f;

        public override Vector3 CalculateForce(Entity entity, List<Entity> neighbors)
        {
            Vector3 steer = Vector3.zero;
            int count = 0;

            foreach (var neighbor in neighbors)
            {
                float distance = Vector3.Distance(entity.Position, neighbor.Position);

                // if 0 < distance < min_distance:
                if (distance > 0 && distance < minDistance)
                {
                    float weight = GetWeightFor(neighbor.Species);
                    if (weight <= 0.001f) continue;

                    // Vector pointing away from neighbor
                    Vector3 diff = entity.Position - neighbor.Position;
                    diff.Normalize();

                    // The closer the neighbor, the stronger the push
                    diff /= distance;

                    steer += diff * weight;
                    count++;
                }
            }

            if (count > 0)
            {
                // steer = steer / count (Average)
                steer /= count;
            }

            // Reynolds Steering Logic (Optional but recommended for smoothness)
            // If you want EXACT Python behavior, you might just return 'steer' here.
            // But usually for Boids, we apply the steering formula:
            if (steer.sqrMagnitude > 0)
            {
                // Implement Reynolds: Steering = Desired - Velocity
                steer.Normalize();
                steer *= entity.Species.maxSpeed;
                steer -= entity.Velocity;
                steer = Vector3.ClampMagnitude(steer, maxForce);
            }

            return steer;
        }
    }
}