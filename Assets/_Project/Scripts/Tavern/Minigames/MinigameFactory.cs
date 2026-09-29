using System;
using Hearthdelve.Core.Minigames;
using Hearthdelve.Core.Random;
using Hearthdelve.Shared.Recipes;

namespace Hearthdelve.Tavern.Minigames
{
    /// <summary>Creates station minigames and matching auto-players from tuning data.</summary>
    public sealed class MinigameFactory
    {
        public MinigameFactory(GrillSettings grill, TapSettings tap, ServingSettings serving)
        {
            Grill = grill;
            Tap = tap;
            Serving = serving;
        }

        public GrillSettings Grill { get; }
        public TapSettings Tap { get; }
        public ServingSettings Serving { get; }

        public IMinigame CreateCook(CookStation station) => station switch
        {
            CookStation.Grill => new GrillMinigame(Grill),
            CookStation.Tap => new TapMinigame(Tap),
            _ => throw new ArgumentOutOfRangeException(nameof(station)),
        };

        public ServingMinigame CreateServing(float fromX, float toX) => new(Serving, fromX, toX);

        /// <summary>An auto-player for any minigame this factory makes (staff auto-resolve).</summary>
        public static IMinigameAutoPlayer CreateAutoPlayer(IMinigame game, float skill, IRandom random) => game switch
        {
            GrillMinigame g => new GrillAutoPlayer(g, skill, random),
            TapMinigame t => new TapAutoPlayer(t, skill, random),
            ServingMinigame s => new ServingAutoPlayer(s, skill, random),
            _ => throw new ArgumentException($"No auto-player for {game?.GetType().Name}", nameof(game)),
        };
    }
}
