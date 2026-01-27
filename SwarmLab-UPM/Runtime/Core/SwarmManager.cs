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
    
            // 1. SAFETY: Remove any entities whose GameObjects were deleted manually
            _entities.RemoveAll(e => e.Transform == null);

            // 2. SAFETY: If the list is empty (e.g. first time setup), try to rebuild it
            // (Optional: if you trust yourself to always click "Generate", you can remove this)
            if (_entities.Count == 0 && transform.childCount > 0)
            {
                // RebuildFromScene(); // Only needed if you want to support manual scene editing
            }

            // 3. Rebuild the Rule Map (This is still needed because Dictionaries are not serialized!)
            _rulesMap.Clear();
            if (swarmConfig != null)
            {
                foreach (var speciesConfig in swarmConfig.speciesConfigs)
                {
                    if (speciesConfig.speciesDefinition != null && !_rulesMap.ContainsKey(speciesConfig.speciesDefinition))
                    {
                        _rulesMap.Add(speciesConfig.speciesDefinition, speciesConfig.steeringRules);
                    }
                }
            }
        }
        
        private void Update()
        {
            if (_entities == null || _entities.Count == 0) return;

            float dt = Time.deltaTime;

            // --- LOOP 1: CALCULATE FORCES ---
            // We calculate everyone's desired direction BEFORE moving anyone.
            // If we moved them while calculating, the last entity would react to 
            // the "future" position of the first entity, creating jitter.
            
            // Note: For 100-300 entities, this O(N^2) loop is fine. 
            // For 1000+, we would need a spatial grid (optimization for later).
            
            foreach (var entity in _entities)
            {
                Vector3 totalAcceleration = Vector3.zero;

                // check if we have rules for this species
                if (_rulesMap.TryGetValue(entity.Species, out var rules))
                {
                    foreach (var rule in rules)
                    {
                        if (rule == null) continue;
                        
                        // Accumulate the force from this rule
                        // We pass ALL entities as neighbors for now.
                        // The Rule is responsible for filtering who is close enough.
                        Vector3 force = rule.CalculateForce(entity, _entities);
                        
                        totalAcceleration += force;
                    }
                }
                
                // Apply acceleration to velocity
                entity.Velocity += totalAcceleration * dt;

                // LIMIT SPEED (Crucial!)
                // Without this, they will accelerate infinitely and disappear.
                float maxSpeed = 5f; // We can expose this in Config later
                if (entity.Velocity.sqrMagnitude > maxSpeed * maxSpeed)
                {
                    entity.Velocity = entity.Velocity.normalized * maxSpeed;
                }
            }

            // --- LOOP 2: APPLY MOVEMENT ---
            foreach (var entity in _entities)
            {
                // Move
                entity.Position += entity.Velocity * dt;

                // Rotate to face velocity (Visual Polish)
                // If moving fast enough to have a direction
                if (entity.Velocity.sqrMagnitude > 0.1f)
                {
                     Quaternion targetRotation = Quaternion.LookRotation(entity.Velocity);
                     // Smooth rotation looks better than instant snapping
                     entity.Transform.rotation = Quaternion.Slerp(entity.Transform.rotation, targetRotation, dt * 5f);
                }

                // Apply to Unity Transform
                entity.UpdateTransform();
            }
        }
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
    
            // 1. Clear the brain list immediately so we can refill it
            _entities.Clear();

            foreach (var species in swarmConfig.speciesConfigs)
            {
                if (species.speciesDefinition == null || species.speciesDefinition.prefab == null) continue;

                GameObject container = new GameObject($"Holder_{species.speciesDefinition.name}");
                container.transform.SetParent(this.transform, false);
        
#if UNITY_EDITOR
                Undo.RegisterCreatedObjectUndo(container, "Generate Swarm");
#endif

                for (int i = 0; i < species.count; i++)
                {
                    Vector3 randomPos = species.spawnOffset + (Random.insideUnitSphere * species.spawnRadius);
            
                    GameObject go = Instantiate(species.speciesDefinition.prefab, container.transform);
                    go.transform.localPosition = randomPos;
                    go.name = $"{species.speciesDefinition.name}_{i}";

#if UNITY_EDITOR
                    Undo.RegisterCreatedObjectUndo(go, "Spawn Entity");
#endif

                    // --- OPTIMIZATION: Create and Add Entity Here ---
                    Entity newEntity = new Entity(species.speciesDefinition, go.transform);
            
                    // Apply the Physics Kick immediately
                    newEntity.Velocity = Random.onUnitSphere * 2f; 
            
                    // Add to the main list
                    _entities.Add(newEntity);
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
