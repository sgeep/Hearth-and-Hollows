using UnityEngine;

namespace Hearthdelve.Tavern.Service
{
    [CreateAssetMenu(menuName = "Hearthdelve/Config/Service", fileName = "ServiceConfig")]
    public sealed class ServiceConfig : ScriptableObject
    {
        public ServiceSettings service = ServiceSettings.Default;
    }
}
