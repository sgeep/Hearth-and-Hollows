using UnityEngine;

namespace Hearthdelve.Tavern.Minigames
{
    [CreateAssetMenu(menuName = "Hearthdelve/Config/Tap", fileName = "TapConfig")]
    public sealed class TapConfig : ScriptableObject
    {
        [Header("Design target: 5–10 s per drink at default tuning (≈5.3 s for good play)")]
        public TapSettings tap = TapSettings.Default;
    }
}
