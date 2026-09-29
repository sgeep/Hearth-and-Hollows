using UnityEngine;

namespace Hearthdelve.Shared.Economy
{
    [CreateAssetMenu(menuName = "Hearthdelve/Config/Economy", fileName = "EconomyConfig")]
    public sealed class EconomyConfig : ScriptableObject
    {
        public DishScoringSettings dishScoring = DishScoringSettings.Default;
        public ServiceEconomySettings service = ServiceEconomySettings.Default;
    }
}
