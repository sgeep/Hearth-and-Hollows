using UnityEngine;

namespace Hearthdelve.Tavern.Minigames
{
    /// <summary>The Butcher Block's tuning (4f Checkpoint C, D17). How many cuts a score gives is on each part's ingredient data.</summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Config/Butcher", fileName = "ButcherConfig")]
    public sealed class ButcherConfig : ScriptableObject
    {
        [Header("Design target: 5–10 s a part")]
        public ButcherSettings butcher = ButcherSettings.Default;
    }
}
