using System.Collections.Generic;
using SwarmLab;
using UnityEngine;

[System.Serializable]
public class AlignmentRule : SteeringRule
{
    [System.Serializable]
    public class Params : SpeciesParams
    {
        public float weight = 1f;       // Custom weight
        public float visionRadius = 10f; // Custom vision radius
    }
    
    [Tooltip("Maximum steering force")]
    public float maxForce = 2f;
    
    public List<Params> speciesParams = new List<Params>();

    // Cache for fast lookup
    private Dictionary<SpeciesDefinition, Params> _cache;

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
        
        Vector3 sum = Vector3.zero;
        float totalWeight = 0f;
        int count = 0;

        foreach (var neighbor in neighbors)
        {
            
            if (neighbor == entity) continue;

            // We check if we have params for this neighbor's species
            if (_cache.TryGetValue(neighbor.Species, out Params p))
            {
                if (p.weight <= 0) continue; 
                
                float distance = Vector3.Distance(entity.Position, neighbor.Position);

                if (distance > 0 && distance < p.visionRadius)
                {
                    sum += neighbor.Velocity * p.weight;
                    totalWeight += p.weight;
                    count++;
                }
            }
        }

        if (count > 0)
        {
            // Average velocity
            sum /= totalWeight; // Weighted average direction
            
            float averageWeight = totalWeight / count;

            // Reynolds Steering
            sum.Normalize();
            sum *= entity.Species.maxSpeed;
            
            Vector3 steer = sum - entity.Velocity;
            steer = Vector3.ClampMagnitude(steer, maxForce);
            
            // APPLY WEIGHT
            return steer * averageWeight;
        }

        return Vector3.zero;
    }
}
