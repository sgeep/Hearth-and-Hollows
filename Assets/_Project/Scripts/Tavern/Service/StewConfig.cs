using Hearthdelve.Tavern.Minigames;
using UnityEngine;

namespace Hearthdelve.Tavern.Service
{
    [CreateAssetMenu(menuName = "Hearthdelve/Config/Stew", fileName = "StewConfig")]
    public sealed class StewConfig : ScriptableObject
    {
        [Header("Design target: 5–10 s to chop a two-ingredient batch")]
        public ChopSettings chop = ChopSettings.Default;
        public StewPotSettings pot = StewPotSettings.Default;
    }
}
