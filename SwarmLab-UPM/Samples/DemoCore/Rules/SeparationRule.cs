using System.Collections.Generic;
using SwarmLab;
using UnityEngine;

[System.Serializable]
public class SeparationRule : SteeringRule
{
    [System.Serializable]
    public class Params : SpeciesParams
    {
        public float weight = 1f;       // Custom Field 1
        public float minDistance = 10f; // Custom Field 2
    }

    // Params per Species
    public List<Params> speciesParams = new List<Params>();

    // Cache for fast lookup
    private Dictionary<SpeciesDefinition, Params> _cache;

    [Tooltip("Maximum force applied for separation")]
    public float maxForce = 2f;

    // --- C. Sync Logic (The Base Class Contract) ---
    public override void SyncSpeciesList(List<SpeciesDefinition> allSpecies)
    {
        // 1. Add missing species
        foreach (var def in allSpecies)
        {
            if (!speciesParams.Exists(p => p.species == def))
            {
                speciesParams.Add(new Params { species = def });
            }
        }
        // 2. Remove deleted species
        speciesParams.RemoveAll(p => p.species == null || !allSpecies.Contains(p.species));
        _cache = null; // Force rebuild
    }

    public override Vector3 CalculateForce(Entity entity, List<Entity> neighbors)
    {
        // Build Cache if needed
        if (_cache == null)
        {
            _cache = new Dictionary<SpeciesDefinition, Params>();
            foreach (var p in speciesParams) if (p.species != null) _cache[p.species] = p;
        }
        
        Vector3 steer = Vector3.zero;
        int count = 0;
        float totalWeight = 0f;

        foreach (var neighbor in neighbors)
        {
            if (neighbor == entity) continue;
            // We check if we have params for this neighbor's species
            if (_cache.TryGetValue(neighbor.Species, out Params p))
            {
                if (p.weight <= 0) continue;
                
                float distance = Vector3.Distance(entity.Position, neighbor.Position);

                if (distance > 0 && distance < p.minDistance)
                {
                    Vector3 diff = entity.Position - neighbor.Position;
                    diff.Normalize();
                    diff /= distance; // Weight by distance (closer = stronger)

                    steer += diff; // Add the vector directly
                    totalWeight += p.weight;
                    count++;
                }
            }
        }

        if (count > 0)
        {
            steer /= count; // Average direction
            
            float averageWeight = totalWeight / count;

            if (steer.sqrMagnitude > 0)
            {
                // Reynolds Steering
                steer.Normalize();
                steer *= entity.Species.maxSpeed;
                steer -= entity.Velocity;
                steer = Vector3.ClampMagnitude(steer, maxForce);
                
                // APPLY WEIGHT
                return steer * averageWeight;
            }
        }

        return Vector3.zero;
    }
}
