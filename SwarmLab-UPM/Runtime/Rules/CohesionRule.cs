using System.Collections.Generic;
using SwarmLab;
using UnityEngine;

namespace Runtime.Rules
{
    [System.Serializable]
    public class CohesionRule : SteeringRule
    {
        [Tooltip("Distance maximum pour voir les voisins (neighbor_radius)")]
        public float visionRadius = 100f;

        [Tooltip("Force maximum de virage (max_force). Plus c'est bas, plus les virages sont larges.")]
        public float maxForce = 2f; // Corresponds to boid.max_force


        public override Vector3 CalculateForce(Entity entity, List<Entity> neighbors)
        {
            Vector3 centerOfMass = Vector3.zero;
            float totalWeight = 0f;
            int count = 0;

            foreach (var neighbor in neighbors)
            {
                // -- LOGIC: Finding Neighbors --
                if (neighbor == entity) continue;

                float distance = Vector3.Distance(entity.Position, neighbor.Position);
                
                // if 0 < distance < neighbor_radius:
                if (distance > 0 && distance < visionRadius)
                {
                    float weight = GetWeightFor(neighbor.Species);
                    if (weight <= 0.001f) continue;

                    centerOfMass += neighbor.Position * weight;
                    totalWeight += weight;
                    count++;
                }
            }

            if (count == 0) return Vector3.zero;

            centerOfMass /= totalWeight;

            // -- LOGIC: Reynolds Steering --
            
            // 1. Desired = (Target - Position).normalized * max_speed
            // Se diriger vers le centre
            Vector3 desired = centerOfMass - entity.Position;
            desired = desired.normalized * entity.Species.maxSpeed;

            // 2. Steer = Desired - Velocity
            // Force de pilotage
            Vector3 steer = desired - entity.Velocity;

            // 3. Steer.limit(max_force)
            // C'est ce qui rend le mouvement fluide (smooth)
            steer = Vector3.ClampMagnitude(steer, maxForce);

            return steer;
        }
    }
}