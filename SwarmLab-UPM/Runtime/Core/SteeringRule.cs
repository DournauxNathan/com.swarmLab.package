using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace SwarmLab
{
    // A simple struct to map Species -> Value in the Inspector
    [System.Serializable]
    public struct SpeciesWeight
    {
        public SpeciesDefinition species;
        public float weight;
    }

    [System.Serializable]
    public abstract class SteeringRule
    {
        [Header("Species Interactions")]
        [Tooltip("Define specific weights for specific species here.")]
        public List<SpeciesWeight> speciesWeights = new List<SpeciesWeight>();

        // Cache for runtime lookup (Dictionaries are faster than iterating lists every frame)
        protected Dictionary<SpeciesDefinition, float> WeightMap = new Dictionary<SpeciesDefinition, float>();
        private bool _isInitialized = false;

        public abstract Vector3 CalculateForce(Entity entity, List<Entity> neighbors);
        
        protected virtual void InitializeMap()
        {
            WeightMap.Clear();
            foreach (var sw in speciesWeights)
            {
                if (sw.species != null && !WeightMap.ContainsKey(sw.species))
                {
                    WeightMap.Add(sw.species, sw.weight);
                }
            }
            _isInitialized = true;
        }

        // Helper to get weight
        protected float GetWeightFor(SpeciesDefinition species)
        {
            if (!_isInitialized) InitializeMap();
            
            if (species != null && WeightMap.TryGetValue(species, out float val))
            {
                return val;
            }
            return 0f; // Default weight if not found
        }

        // protected virtual void OnValidate()
        // {
        //     if (speciesWeights == null) return;
        //
        //     // Check if we have duplicate definitions for the same species
        //     var duplicates = speciesWeights
        //         .Where(sw => sw.species != null)
        //         .GroupBy(sw => sw.species)
        //         .Where(g => g.Count() > 1)
        //         .Select(g => g.Key.name)
        //         .ToList();
        //
        //     if (duplicates.Count > 0)
        //     {
        //         Debug.LogWarning($"[SwarmLab] Rule '{this.name}' has duplicate entries for species: {string.Join(", ", duplicates)}. Removing duplicates.");
        //
        //         // Keep only the first occurrence of each species
        //         var distinctWeights = new List<SpeciesWeight>();
        //         var seenSpecies = new HashSet<SpeciesDefinition>();
        //
        //         foreach (var sw in speciesWeights)
        //         {
        //             if (sw.species != null && !seenSpecies.Contains(sw.species))
        //             {
        //                 distinctWeights.Add(sw);
        //                 seenSpecies.Add(sw.species);
        //             }
        //         }
        //         speciesWeights = distinctWeights;
        //     }
        //
        //     // Force cache rebuild in editor if values change
        //     _isInitialized = false; 
        // }
    }
}