using Hearthdelve.Shared.Inventory;
using UnityEngine;

namespace Hearthdelve.Dungeon.Run
{
    [CreateAssetMenu(menuName = "Hearthdelve/Config/Delve", fileName = "DelveConfig")]
    public sealed class DelveConfig : ScriptableObject
    {
        public SatchelSettings satchel = SatchelSettings.Default;
    }
}
