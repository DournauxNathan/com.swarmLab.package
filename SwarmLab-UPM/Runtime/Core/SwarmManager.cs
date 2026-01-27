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
        private List<Entity> _entities = new List<Entity>();
        
        private void Awake()
        {
            if (Instance != null) Debug.LogError("SwarmManager is already initialized");
            Instance = this;
            
            InitializeRuntimeEntities();
        }
        
        private void InitializeRuntimeEntities()
        {
            _entities.Clear();
            // Logic to grab existing children and create Entity classes goes here
            // TODO
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
