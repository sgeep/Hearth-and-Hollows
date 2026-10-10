namespace Hearthdelve.Shared.Game
{
    /// <summary>
    /// 4i-D's performance probe (<c>?perf</c> on the web, <c>-perf</c> on Windows): what it asks of the game that the game's own
    /// systems honour. Never set in play.
    /// </summary>
    public static class PerfOptions
    {
        /// <summary>The next delve starts in the arena (the troll fight, measured).</summary>
        public static bool StartInArena { get; set; }
    }
}
