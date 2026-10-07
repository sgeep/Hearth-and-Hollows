using UnityEngine;

namespace Hearthdelve.Shared.Village
{
    /// <summary>The village's tuning asset (4h Checkpoint C), on the <see cref="GameDatabase"/>.</summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Village/Village Life", fileName = "VillageLife")]
    public sealed class VillageLifeConfig : ScriptableObject
    {
        public VillageLifeSettings settings = VillageLifeSettings.Default;
    }
}
