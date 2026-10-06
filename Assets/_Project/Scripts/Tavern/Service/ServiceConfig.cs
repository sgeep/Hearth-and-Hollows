using UnityEngine;

namespace Hearthdelve.Tavern.Service
{
    [CreateAssetMenu(menuName = "Hearthdelve/Config/Service", fileName = "ServiceConfig")]
    public sealed class ServiceConfig : ScriptableObject
    {
        public ServiceSettings service = ServiceSettings.Default;
        [Tooltip("Special requests (4f Checkpoint D): how often, how many, and the thanks.")]
        public CustomerRequestSettings requests = CustomerRequestSettings.Default;
        public TavernPlayerSettings player = TavernPlayerSettings.Default;
    }

    [System.Serializable]
    public struct TavernPlayerSettings
    {
        [Min(0.1f), Tooltip("Walking speed when not carrying a plate (carrying speed is in ServingConfig).")]
        public float walkSpeed;
        [Min(0.1f), Tooltip("How close to a station you need to be to use it.")]
        public float interactRange;
        [Min(0.05f), Tooltip("A plate carrier and a walking customer closer than this bump.")]
        public float bumpDistance;

        public static TavernPlayerSettings Default => new() { walkSpeed = 4.5f, interactRange = 0.9f, bumpDistance = 0.45f };
    }
}
