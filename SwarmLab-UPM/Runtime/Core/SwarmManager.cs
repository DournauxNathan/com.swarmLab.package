using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SwarmLab
{

    public class SwarmManager : MonoBehaviour
    {
        public static SwarmManager Instance { get; private set; }

        [SerializeField] private bool drawSpawnZones = true;
        [SerializeField] private SwarmConfig swarmConfig;
        public SwarmConfig Config => swarmConfig;
        
        // Runtime list of entities
        [SerializeField] List<Entity> _entities = new List<Entity>();
        
        // Cache rules per species
        private Dictionary<SpeciesDefinition, List<SteeringRule>> _rulesMap = new Dictionary<SpeciesDefinition, List<SteeringRule>>();

        private void Awake()
        {
            if (Instance != null) Debug.LogError("SwarmManager is already initialized");
            Instance = this;
            
            InitializeRuntimeEntities();
        }
        
        private void InitializeRuntimeEntities()
        {
            _entities.Clear();
            _rulesMap.Clear();

            if (swarmConfig == null) return;

            // 1. Build Rule Map from Config
            foreach (var speciesConfig in swarmConfig.speciesConfigs)
            {
                if (speciesConfig.speciesDefinition != null)
                {
                    if (!_rulesMap.ContainsKey(speciesConfig.speciesDefinition))
                    {
                        // Clone the rules so runtime changes don't affect asset? 
                        // Or just reference them. For now, reference is fine, but deeper clone might be safer if rules had state. 
                        // Base rules are stateless or just have weights.
                        _rulesMap.Add(speciesConfig.speciesDefinition, speciesConfig.steeringRules);
                    }
                }
            }

            // 2. Find existing entities in scene (spawned by GenerateSwarm)
            // They are children of "Holder_X" GOs.
            // But we can just search all children of SwarmManager for simplicity or target the holders.
            // Let's iterate holders as per generation logic.
            
            foreach (var speciesConfig in swarmConfig.speciesConfigs)
            {
                if (speciesConfig.speciesDefinition == null) continue;

                Transform holder = transform.Find($"Holder_{speciesConfig.speciesDefinition.name}");
                if (holder != null)
                {
                    foreach (Transform child in holder)
                    {
                        var entity = new Entity(speciesConfig.speciesDefinition, child);
                        
                        // Initialize with zero velocity so they only move if rules are applied
                        entity.Velocity = Vector3.zero; 
                        
                        _entities.Add(entity);
                    }
                }
            }
        }
        
        #region Temporaire
        private void Update()
        {
            if (_entities == null || _entities.Count == 0)
            {
                Debug.Log("No entities found");
                return;
            }

            float dt = Time.deltaTime;

            // 1. Calculate Forces
            foreach (var entity in _entities)
            {
                Vector3 acceleration = Vector3.zero;

                if (_rulesMap.TryGetValue(entity.Species, out var rules) && rules != null)
                {
                    // For now, neighbors = all other entities. 
                    // Optimization: In real implementation, use spatial partition (Grid/Octree).
                    // We pass the full list. Rules should ideally handle "self" check or we filter here.
                    // My implemented rules (Cohesion/Alignment) check weights. 
                    // If species weights are properly set up (e.g. A reacts to A), it works.
                    // Implicitly, Cohesion/Alignment loops over 'neighbors'.
                    
                    foreach (var rule in rules)
                    {
                        if (rule != null)
                        {
                            acceleration += rule.CalculateForce(entity, _entities);
                        }
                    }
                }

                // Simple Euler Integration
                entity.Velocity += acceleration * dt;
                
                // Clamp Velocity to avoid explosion
                if (entity.Velocity.sqrMagnitude > 25f) // Max speed 5
                {
                    entity.Velocity = entity.Velocity.normalized * 5f;
                }
                
                entity.Position += entity.Velocity * dt;
            }

            // 2. Apply Transform
            foreach (var entity in _entities)
            {
                entity.UpdateTransform();
            }
        }
        #endregion
        
        // --- EDITOR TOOLS ---

        public void ClearSwarm()
        {
            // Loop backwards to destroy children safely
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                
                #if UNITY_EDITOR
                // Use Undo.DestroyObjectImmediate so you can Ctrl+Z the clear
                Undo.DestroyObjectImmediate(child);
                #else
                DestroyImmediate(child);
                #endif
            }
        }

        public void GenerateSwarm()
        {
            if (swarmConfig == null) return;
            
            ClearSwarm();

            foreach (var species in swarmConfig.speciesConfigs)
            {
                if (species.speciesDefinition == null || species.speciesDefinition.prefab == null) continue;

                // Create a container for this species (e.g. "Holder_RedAnts")
                GameObject container = new GameObject($"Holder_{species.speciesDefinition.name}");
                container.transform.SetParent(this.transform, false);
                
                #if UNITY_EDITOR
                Undo.RegisterCreatedObjectUndo(container, "Generate Swarm");
                #endif

                // Spawn individuals
                for (int i = 0; i < species.count; i++)
                {
                    // Calculate random position within sphere
                    Vector3 randomPos = species.spawnOffset + (Random.insideUnitSphere * species.spawnRadius);
                    
                    // Instantiate Prefab
                    GameObject go = Instantiate(species.speciesDefinition.prefab, container.transform);
                    go.transform.localPosition = randomPos; // Local position relative to Manager
                    go.name = $"{species.speciesDefinition.name}_{i}";

                    #if UNITY_EDITOR
                    Undo.RegisterCreatedObjectUndo(go, "Spawn Entity");
                    #endif
                }
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!drawSpawnZones || swarmConfig == null || swarmConfig.speciesConfigs == null) return;

            // Draw everything in Local Space (relative to the Manager's rotation/position)
            Matrix4x4 originalMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            foreach (var species in swarmConfig.speciesConfigs)
            {
                if (species.speciesDefinition == null) continue;

                // Generate a consistent color based on the species name
                Color speciesColor = Color.HSVToRGB((species.speciesDefinition.name.GetHashCode() * 0.13f) % 1f, 0.7f, 1f);
                Gizmos.color = speciesColor;

                Gizmos.DrawWireSphere(species.spawnOffset, species.spawnRadius);
                
                // Draw a small solid sphere at the center of the zone
                Gizmos.color = new Color(speciesColor.r, speciesColor.g, speciesColor.b, 0.4f);
                Gizmos.DrawSphere(species.spawnOffset, 0.05f);
            }
            
            Gizmos.matrix = originalMatrix;
        }
    }
}
