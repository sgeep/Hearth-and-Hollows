using System;
using Hearthdelve.Core.Minigames;
using Hearthdelve.Core.Random;
using Hearthdelve.Shared.Recipes;

namespace Hearthdelve.Tavern.Minigames
{
    /// <summary>Creates station minigames and matching auto-players from tuning data.</summary>
    public sealed class MinigameFactory
    {
        public MinigameFactory(GrillSettings grill, TapSettings tap, ServingSettings serving, ChopSettings? chop = null)
        {
            Grill = grill;
            Tap = tap;
            Serving = serving;
            Chop = chop ?? ChopSettings.Default;
        }

        public GrillSettings Grill { get; }
        public TapSettings Tap { get; }
        public ServingSettings Serving { get; }
        public ChopSettings Chop { get; }

        public IMinigame CreateCook(CookStation station) => station switch
        {
            CookStation.Grill => new GrillMinigame(Grill),
            CookStation.Tap => new TapMinigame(Tap),
            _ => throw new ArgumentOutOfRangeException(nameof(station)),
        };

        /// <summary>A plate to carry; the carrier serves it with <see cref="ServingMinigame.Deliver"/>.</summary>
        public ServingMinigame CreateServing() => new(Serving);

        /// <summary>Chopping <paramref name="items"/> ingredients for a stew batch.</summary>
        public ChopMinigame CreateChop(int items, IRandom random) => new(Chop, items, random);

        /// <summary>An auto-player for any minigame this factory makes (staff auto-resolve).</summary>
        public static IMinigameAutoPlayer CreateAutoPlayer(IMinigame game, float skill, IRandom random) => game switch
        {
            GrillMinigame g => new GrillAutoPlayer(g, skill, random),
            TapMinigame t => new TapAutoPlayer(t, skill, random),
            ServingMinigame s => new ServingAutoPlayer(s, skill, random),
            ChopMinigame c => new ChopAutoPlayer(c, skill, random),
            _ => throw new ArgumentException($"No auto-player for {game?.GetType().Name}", nameof(game)),
        };
    }
}
