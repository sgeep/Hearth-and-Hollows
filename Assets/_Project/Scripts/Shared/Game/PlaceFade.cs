namespace Hearthdelve.Shared.Game
{
    /// <summary>
    /// 4i-C: one timing for every change of place inside the world (Tally Ho!'s front door and stairs, the hatch, the Hollows' rooms),
    /// so every crossing feels the same: a quick fade to black and back, near-instant as the doors were locked to be (4h). Changes of
    /// scene (the day moving on: the evening, the delve, the night) keep the transition screen's longer, captioned fade.
    /// </summary>
    public static class PlaceFade
    {
        /// <summary>Seconds to cover, and seconds to uncover.</summary>
        public const float Seconds = 0.2f;
    }
}
