using UnityEngine;

namespace SwarmLab
{

    [CreateAssetMenu(fileName = "Species Definition", menuName = "SwarmLab/Species Definition")]
    public class SpeciesDefinition : ScriptableObject
    {
        public GameObject prefab;
        public string speciesName;
        
        [Header("Behavior Settings")]
        public float maxSpeed;
    }
}