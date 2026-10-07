using System;

namespace Hearthdelve.Shared.Game
{
    /// <summary>
    /// The world's seed (4h Checkpoint B, save version 10): one stable number per game, for what later varies by day but must
    /// come out the same for the same game (Checkpoint C's visits and good days). Made once and saved; never re-rolled.
    /// </summary>
    public static class WorldSeed
    {
        /// <summary>A new game's seed (never 0, which reads as "none").</summary>
        public static int New()
        {
            int seed = Guid.NewGuid().GetHashCode() ^ Environment.TickCount;
            return seed == 0 ? 1 : seed;
        }

        /// <summary>
        /// A save from before seeds (migrated to version 10): derived from the save's own text, so loading the same file again
        /// before it's ever re-saved gives the same seed (FNV-1a; never 0).
        /// </summary>
        public static int From(string text)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in text ?? string.Empty)
                {
                    hash ^= c;
                    hash *= 16777619;
                }
                int seed = (int)hash;
                return seed == 0 ? 1 : seed;
            }
        }
    }
}
