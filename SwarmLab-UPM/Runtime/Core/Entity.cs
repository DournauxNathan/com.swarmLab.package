using UnityEngine;

namespace SwarmLab.Core
{
    [System.Serializable]
    public class Entity
    {
        public SpeciesDefinition Species;
        public Vector3 Position;
        public Vector3 Velocity;
    
        // The visual link
        public Transform Transform;
        
        public Entity(SpeciesDefinition species, Transform transform)
        {
            Species = species;
            Transform = transform;
            Position = transform.position;
            Velocity = Vector3.zero;
        }

        // Helper to sync data -> visual
        public void UpdateTransform()
        {
            if (Transform != null) Transform.position = Position;
        }
    }
}