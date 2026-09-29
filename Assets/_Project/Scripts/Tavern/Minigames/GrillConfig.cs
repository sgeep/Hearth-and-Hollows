using UnityEngine;

namespace Hearthdelve.Tavern.Minigames
{
    [CreateAssetMenu(menuName = "Hearthdelve/Config/Grill", fileName = "GrillConfig")]
    public sealed class GrillConfig : ScriptableObject
    {
        [Header("Design target: 5–10 s per dish at default tuning (≈6.3 s for good play)")]
        public GrillSettings grill = GrillSettings.Default;
    }
}
