using UnityEngine;

namespace SwarmLab
{

    [CreateAssetMenu(fileName = "SpecieDefinition", menuName = "SwarmLab/SpecieDefinition")]
    public class SpeciesDefinition : ScriptableObject
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private string speciesName;
    }
}