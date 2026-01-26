using UnityEngine;

namespace SwarmLab
{

    public class SwarmManager : MonoBehaviour
    {
        public static SwarmManager Instance { get; private set; }

        [SerializeField] private SwarmConfig swarmConfig;

        private void Awake()
        {
            if (Instance != null) Debug.LogError("SwarmManager is already initialized");
            Instance = this;
        }
    }
}
