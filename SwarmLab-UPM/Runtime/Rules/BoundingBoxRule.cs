using System.Collections.Generic;
using SwarmLab;
using UnityEngine;

namespace Runtime.Rules
{
    [System.Serializable]
    public class BoundingBoxRule : SteeringRule
    {
        [Header("Container Settings")]
        public Vector3 center = Vector3.zero;
        public Vector3 size = new Vector3(20, 20, 20);
        
        [Tooltip("How far from the edge does the force start kicking in?")]
        public float edgeThreshold = 5f;

        [Tooltip("Max force to push them back in")]
        public float maxForce = 10f;

        public override Vector3 CalculateForce(Entity entity, List<Entity> neighbors)
        {
            Vector3 position = entity.Position;
            Vector3 desired = Vector3.zero;

            // Check X axis
            if (position.x < center.x - size.x / 2 + edgeThreshold)
                desired.x = 1; // Want to go Right
            else if (position.x > center.x + size.x / 2 - edgeThreshold)
                desired.x = -1; // Want to go Left

            // Check Y axis
            if (position.y < center.y - size.y / 2 + edgeThreshold)
                desired.y = 1;
            else if (position.y > center.y + size.y / 2 - edgeThreshold)
                desired.y = -1;

            // Check Z axis
            if (position.z < center.z - size.z / 2 + edgeThreshold)
                desired.z = 1;
            else if (position.z > center.z + size.z / 2 - edgeThreshold)
                desired.z = -1;

            // If we are happily inside the safe zone, do nothing
            if (desired == Vector3.zero) return Vector3.zero;

            // Scale desired velocity to max speed
            desired.Normalize();
            desired *= entity.Species.maxSpeed;

            // Reynolds Steering: Desired - Velocity
            Vector3 steer = desired - entity.Velocity;
            steer = Vector3.ClampMagnitude(steer, maxForce);

            // Note: We typically don't multiply this by Species Weight 
            // because the wall doesn't care what species you are. 
            // But we DO multiply by the Rule Weight (Global).
            
            return steer;
        }
    }
}