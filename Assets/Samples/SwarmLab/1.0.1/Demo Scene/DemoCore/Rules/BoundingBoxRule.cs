using System.Collections.Generic;
using SwarmLab;
using UnityEngine;

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

    // Since this rule doesn't have per-species settings, 
    // we just leave this method empty.
    public override void SyncSpeciesList(List<SpeciesDefinition> allSpecies)
    {
    }

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

        return steer;
    }

    public override void DrawGizmos()
    {
        Gizmos.color = Color.yellow; // Yellow cage
        Gizmos.DrawWireCube(center, size);

        // Optional: Draw the "Soft" threshold inside
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Vector3 thresholdSize = new Vector3(
            size.x - edgeThreshold * 2,
            size.y - edgeThreshold * 2,
            size.z - edgeThreshold * 2
        );
        Gizmos.DrawWireCube(center, thresholdSize);
    }
}
