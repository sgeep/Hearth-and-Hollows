using Hearthdelve.Shared.Inventory;
using UnityEngine;

namespace Hearthdelve.Dungeon.Run
{
    [CreateAssetMenu(menuName = "Hearthdelve/Config/Delve", fileName = "DelveConfig")]
    public sealed class DelveConfig : ScriptableObject
    {
        public SatchelSettings satchel = SatchelSettings.Default;
        [Tooltip("How fast carried parts lose freshness (shared with the overnight storeroom loss).")]
        public FreshnessConfig freshness;
    }
}
