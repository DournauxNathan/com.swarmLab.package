using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace SwarmLab
{

    public class SwarmManager : MonoBehaviour
    {
        public enum SimulationMode { Volumetric, Planar }

        public static SwarmManager Instance { get; private set; }

        [Header("General Settings")]
        [SerializeField] private bool drawSpawnZones = true;
        [SerializeField] private SwarmConfig swarmConfig;
        
        [Header("Simulation Mode")]
        [SerializeField] private SimulationMode simulationMode = SimulationMode.Volumetric;
        [SerializeField] private Transform planarBoundary;
        [SerializeField] private Vector2 planarSize = new Vector2(50f, 50f);

        public SwarmConfig Config => swarmConfig;
        
        // Runtime list of entities
        [FormerlySerializedAs("_entities")] [SerializeField] private List<Entity> entities = new List<Entity>();
        
        // Cache rules per species
        private Dictionary<SpeciesDefinition, List<SteeringRule>> _rulesMap = new Dictionary<SpeciesDefinition, List<SteeringRule>>();

        private void Awake()
        {
            if (Instance != null) Debug.LogError("SwarmManager is already initialized");
            Instance = this;
    
            // 1. SAFETY: Remove any entities whose GameObjects were deleted manually
            entities.RemoveAll(e => e.Transform == null);

            // 2. SAFETY: If the list is empty (e.g. first time setup), try to rebuild it
            // (Optional: if you trust yourself to always click "Generate", you can remove this)
            if (entities.Count == 0 && transform.childCount > 0)
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
            if (entities == null || entities.Count == 0) return;

            float dt = Time.deltaTime;

            // --- LOOP 1: CALCULATE FORCES ---
            // We calculate everyone's desired direction BEFORE moving anyone.
            // If we moved them while calculating, the last entity would react to 
            // the "future" position of the first entity, creating jitter.
            
            // Note: For 100-300 entities, this O(N^2) loop is fine. 
            // For 1000+, we would need a spatial grid (optimization for later).
            
            foreach (var entity in entities)
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
                        Vector3 force = rule.CalculateForce(entity, entities);
                        
                        totalAcceleration += force;
                    }
                }
                
                // 2D MODE: Project Force onto the plane so we don't accelerate "up" or "down"
                if (simulationMode == SimulationMode.Planar && planarBoundary != null)
                {
                    totalAcceleration = Vector3.ProjectOnPlane(totalAcceleration, planarBoundary.up);
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
            foreach (var entity in entities)
            {
                // Move
                entity.Position += entity.Velocity * dt;

                // 2D MODE: Constrain to Plane
                if (simulationMode == SimulationMode.Planar && planarBoundary != null)
                {
                    // 1. World -> Local
                    Vector3 localPos = planarBoundary.InverseTransformPoint(entity.Position);
                    
                    // 2. Flatten (remove elevation)
                    localPos.y = 0;
                    
                    // Flatten Velocity as well so rotation looks correct (no pitching up/down)
                    Vector3 localVel = planarBoundary.InverseTransformDirection(entity.Velocity);
                    localVel.y = 0;
                    entity.Velocity = planarBoundary.TransformDirection(localVel);
                    
                    // 3. Wrap bounds (Pac-man style)
                    float halfX = planarSize.x * 0.5f;
                    float halfZ = planarSize.y * 0.5f;

                    if (localPos.x > halfX) localPos.x -= planarSize.x;
                    else if (localPos.x < -halfX) localPos.x += planarSize.x;

                    if (localPos.z > halfZ) localPos.z -= planarSize.y;
                    else if (localPos.z < -halfZ) localPos.z += planarSize.y;
                    
                    // 4. Local -> World
                    entity.Position = planarBoundary.TransformPoint(localPos);
                }

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
            entities.Clear();

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
                    Vector3 spawnPosition;
                    Vector3 initVelocity;

                    // 1. Calculate Spawn Position & Velocity based on Mode
                    if (simulationMode == SimulationMode.Planar && planarBoundary != null)
                    {
                        // Planar Distribution
                        float rx = Random.Range(-planarSize.x * 0.5f, planarSize.x * 0.5f);
                        float rz = Random.Range(-planarSize.y * 0.5f, planarSize.y * 0.5f);
                        Vector3 localPosOnPlane = new Vector3(rx, 0, rz);
                        
                        // Convert Plane Local -> World -> Manager Local
                        Vector3 worldPos = planarBoundary.TransformPoint(localPosOnPlane);
                        spawnPosition = transform.InverseTransformPoint(worldPos);

                        // Planar Velocity
                        Vector3 randomDir = Random.insideUnitCircle.normalized; // Random 2D dir
                        Vector3 localVel = new Vector3(randomDir.x, 0, randomDir.y) * 2f;
                        initVelocity = planarBoundary.TransformDirection(localVel);
                    }
                    else
                    {
                        // Volumetric Distribution (Sphere)
                        spawnPosition = species.spawnOffset + (Random.insideUnitSphere * species.spawnRadius);
                        initVelocity = Random.onUnitSphere * 2f;
                    }
            
                    GameObject go = Instantiate(species.speciesDefinition.prefab, container.transform);
                    go.transform.localPosition = spawnPosition;
                    go.name = $"{species.speciesDefinition.name}_{i}";

#if UNITY_EDITOR
                    Undo.RegisterCreatedObjectUndo(go, "Spawn Entity");
#endif

                    // --- OPTIMIZATION: Create and Add Entity Here ---
                    Entity newEntity = new Entity(species.speciesDefinition, go.transform);
            
                    // Apply Velocity
                    newEntity.Velocity = initVelocity; 
            
                    // Add to the main list
                    entities.Add(newEntity);
                }
            }
        }
        
        private void OnDrawGizmos()
        {
            if (swarmConfig == null || swarmConfig.speciesConfigs == null) return;

            // Draw everything in Local Space (relative to the Manager's rotation/position)
            Matrix4x4 originalMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            // ONLY DRAW SPHERES IN VOLUMETRIC MODE
            if (simulationMode == SimulationMode.Volumetric)
            {
                foreach (var species in swarmConfig.speciesConfigs)
                {
                    if (species.speciesDefinition == null) continue;

                    if (drawSpawnZones)
                    {
                        // Generate a consistent color based on the species name
                        Color speciesColor = Color.HSVToRGB((species.speciesDefinition.name.GetHashCode() * 0.13f) % 1f,
                            0.7f, 1f);
                        Gizmos.color = speciesColor;

                        Gizmos.DrawWireSphere(species.spawnOffset, species.spawnRadius);

                        // Draw a small solid sphere at the center of the zone
                        Gizmos.color = new Color(speciesColor.r, speciesColor.g, speciesColor.b, 0.4f);
                        Gizmos.DrawSphere(species.spawnOffset, 0.05f);
                    }
                    
                    foreach (var rule in species.steeringRules)
                    {
                        if (rule != null) rule.DrawGizmos();
                    }
                }
            }
            
            Gizmos.matrix = originalMatrix;
            
            // Draw 2D Boundary if enabled
            if (simulationMode == SimulationMode.Planar && planarBoundary != null)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.matrix = planarBoundary.localToWorldMatrix;
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(planarSize.x, 0, planarSize.y));
                Gizmos.color = new Color(0, 1, 0, 0.1f);
                Gizmos.DrawCube(Vector3.zero, new Vector3(planarSize.x, 0, planarSize.y));
                Gizmos.matrix = originalMatrix;
            }
        }

        [ContextMenu("Create Simulation Plane")]
        public void CreatePlane()
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "SimulationBoundary";
            plane.transform.position = Vector3.zero;
            plane.transform.localScale = new Vector3(planarSize.x / 10f, 1, planarSize.y / 10f); // Plane default size is 10x10
            
            // Assign
            planarBoundary = plane.transform;
            simulationMode = SimulationMode.Planar;
            
            // Optional: transparent material or just collider, but for now default is fine
            Debug.Log("Created Simulation Plane and enabled 2D mode.");
        }
    }
}
