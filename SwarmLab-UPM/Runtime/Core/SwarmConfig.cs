using System.Collections.Generic;
using UnityEngine;

namespace SwarmLab.Core
{
    // The Configuration for a single species in this specific swarm
    [System.Serializable]
    public struct SpeciesConfig
    {
        [Tooltip("The visual identity (Prefab, Name, Mesh)")]
        public SpeciesDefinition speciesDefinition;
        
        [Header("Spawn Settings")]
        public int count;
        public float spawnRadius;
        public Vector3 spawnOffset;

        [Header("Behavior rules")]
        [Tooltip("List of behavior rules this species follows in this simulation")]
        [SerializeReference] public List<SteeringRule> steeringRules;

    }

    // The Main Config Asset
    [CreateAssetMenu(fileName = "NewSwarmConfig", menuName = "SwarmLab/Swarm Config")]
    public class SwarmConfig : ScriptableObject
    {
        [Header("Population")]
        public List<SpeciesConfig> speciesConfigs;
        
    }
}