using UnityEngine;

namespace Hearthdelve.Shared.Surface
{
    /// <summary>The surface day's tuning (4h): one asset, read by <see cref="Game.SurfaceTime"/>. Test values, not balance.</summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Surface Clock", fileName = "SurfaceClockConfig")]
    public sealed class SurfaceClockConfig : ScriptableObject
    {
        public SurfaceClockSettings settings = SurfaceClockSettings.Default;
    }
}
