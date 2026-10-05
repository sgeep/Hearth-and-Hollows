using UnityEngine;

namespace Hearthdelve.Dungeon.Harvest
{
    [CreateAssetMenu(menuName = "Hearthdelve/Config/Harvest Rules", fileName = "HarvestRulesConfig")]
    public sealed class HarvestRulesConfig : ScriptableObject
    {
        public HarvestRuleSettings rules = HarvestRuleSettings.Default;
        [Tooltip("The Harvest Finisher (4e step 3).")]
        public FinisherSettings finisher = FinisherSettings.Default;
    }
}
