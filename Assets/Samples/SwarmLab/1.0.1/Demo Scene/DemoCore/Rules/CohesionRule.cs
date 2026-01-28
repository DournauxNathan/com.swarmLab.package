using System.Collections.Generic;
using SwarmLab;
using UnityEngine;

[System.Serializable]
public class CohesionRule : SteeringRule
{
    [System.Serializable]
    public class Params : SpeciesParams
    {
        public float weight = 1f;       // Custom Field 1
        public float visionRadius = 10f; // Custom Field 2
    }

    [Tooltip("Maximum steering force")]
    public float maxForce = 2f;
    
    // Params per Species
    public List<Params> speciesParams = new List<Params>();

    // Cache for fast lookup
    private Dictionary<SpeciesDefinition, Params> _cache;

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

        Vector3 center = Vector3.zero;
        float totalWeight = 0f;

        foreach (var n in neighbors)
        {
            if (n == entity) continue;

            // We check if we have params for this neighbor's species
            if (_cache.TryGetValue(n.Species, out Params p))
            {
                if (p.weight <= 0) continue; 
                
                float dist = Vector3.Distance(entity.Position, n.Position);
                
                // We can check specific radius for THIS species!
                if (dist < p.visionRadius) 
                {
                    center += n.Position * p.weight;
                    totalWeight += p.weight;
                }
            }
        }
        
        if (totalWeight > 0)
        {
            center /= totalWeight;
            Vector3 desired = (center - entity.Position).normalized * entity.Species.maxSpeed;
            Vector3 steer = Vector3.ClampMagnitude(desired - entity.Velocity, maxForce); 
            return steer;
        }
        
        return Vector3.zero;
    }
}
