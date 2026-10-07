using UnityEngine;

namespace Hearthdelve.Shared.Surface
{
    /// <summary>Vigor's tuning (4h Checkpoint B): the day's pips and what strenuous work costs.</summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Surface/Vigor Config", fileName = "VigorConfig")]
    public sealed class VigorConfig : ScriptableObject
    {
        public VigorSettings settings = VigorSettings.Default;
    }
}
