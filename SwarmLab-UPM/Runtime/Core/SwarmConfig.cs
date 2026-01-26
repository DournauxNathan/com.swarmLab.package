using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SwarmLab
{
    // The Configuration for a single species in this specific swarm
    [System.Serializable]
    public struct SpeciesConfig
    {
        [Tooltip("The visual identity (Prefab, Name, Mesh)")]
        public SpeciesDefinition speciesDefinition;

        [Tooltip("The Logic: List of behavior rules this species follows in this simulation")]
        [SerializeReference] public List<SteeringRule> steeringRules;

        [Header("Spawn Settings")]
        public int count;
        public float spawnRadius;
    }

    // The Main Config Asset
    [CreateAssetMenu(fileName = "NewSwarmConfig", menuName = "SwarmLab/Swarm Config")]
    public class SwarmConfig : ScriptableObject
    {
        [Header("Population")]
        public List<SpeciesConfig> speciesConfigs;
        
        // private void OnValidate()
        // {
        //     if (speciesConfigs == null) return;
        //
        //     for (int i = 0; i < speciesConfigs.Count; i++)
        //     {
        //         var config = speciesConfigs[i];
        //         if (config.steeringRules == null) continue;
        //
        //         // 1. Manually Validate internal lists (since they aren't Objects)
        //         // foreach(var rule in config.steeringRules)
        //         // {
        //         //     if(rule != null) rule.Validate();
        //         // }
        //
        //         // 2. Smart De-duplication
        //         // We must allow NULLs (newly added slots) to exist, otherwise the "+" button won't work.
        //         // We only check for duplicates among the instantiated rules.
        //         var activeRules = config.steeringRules.Where(r => r != null).ToList();
        //
        //         var duplicates = activeRules
        //             .GroupBy(r => r.GetType())
        //             .Where(g => g.Count() > 1)
        //             .Select(g => g.Key.Name)
        //             .ToList();
        //
        //         if (duplicates.Count > 0)
        //         {
        //             Debug.LogWarning($"[SwarmLab] Species {i} has duplicate rules of type: {string.Join(", ", duplicates)}. Auto-cleaning.");
        //             
        //             var cleanList = new List<SteeringRule>();
        //             var seenTypes = new HashSet<System.Type>();
        //
        //             foreach (var rule in config.steeringRules)
        //             {
        //                 // Always keep empty slots (nulls) so the user can edit them
        //                 if (rule == null)
        //                 {
        //                     cleanList.Add(null);
        //                     continue;
        //                 }
        //
        //                 // For actual rules, ensure only one per Type exists
        //                 if (!seenTypes.Contains(rule.GetType()))
        //                 {
        //                     cleanList.Add(rule);
        //                     seenTypes.Add(rule.GetType());
        //                 }
        //             }
        //             config.steeringRules = cleanList;
        //             speciesConfigs[i] = config; // Save back to struct
        //         }
        //     }
        // }
    }
}