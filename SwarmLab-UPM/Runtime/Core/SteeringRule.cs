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

        public virtual void OnValidate()
        {
            _isInitialized = false; 
        }

        // helper method to check for errors without deleting data.
        public string GetValidationWarning()
        {
            if (speciesWeights == null) return null;

            // Check if we have duplicate definitions
            var duplicates = speciesWeights
                .Where(sw => sw.species != null)
                .GroupBy(sw => sw.species)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key.name)
                .ToList();

            if (duplicates.Count > 0)
            {
                return $"Duplicate species found: {string.Join(", ", duplicates)}";
            }

            return null; // No warning
        }
    }
}