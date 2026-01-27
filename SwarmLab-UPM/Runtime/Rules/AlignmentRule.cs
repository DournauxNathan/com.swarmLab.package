using System.Collections.Generic;
using PlasticGui.WorkspaceWindow.PendingChanges;
using SwarmLab;
using UnityEditor.TerrainTools;
using UnityEngine;

namespace Runtime.Rules
{
    [System.Serializable]
    public class AlignmentRule : SteeringRule
    {
        public override Vector3 CalculateForce(Entity entity, List<Entity> neighbors)
        {
            if (neighbors == null || neighbors.Count == 0)
                return Vector3.zero;
            
            Vector3 steering = Vector3.zero;
            float totalWeight = 0f;
            int  count = 0;

            foreach (var neighbor in neighbors)
            {
                if (neighbor == entity) continue;
                
                float weight = GetWeightFor(neighbor.Species);
                if (weight > 0f)
                {
                    steering += neighbor.Velocity * weight;
                    totalWeight += weight;
                    count++;
                }

                if (count > 0 && totalWeight > 0f)
                {
                    steering /= count;
                    Vector3 direction = steering - entity.Position;
                    return direction.normalized;
                }
            }
            
            return Vector3.zero;
        }
    }
}