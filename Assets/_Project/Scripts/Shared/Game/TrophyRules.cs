using System;
using System.Collections.Generic;
using Hearthdelve.Shared.Customization;

namespace Hearthdelve.Shared.Game
{
    /// <summary>A boss and the trophy its first defeat earns (4f, D10, D22). The boss's own data stays in the Dungeon assembly.</summary>
    [Serializable]
    public sealed class BossTrophy
    {
        public string bossId;
        public FurnitureDefinition trophy;
    }

    /// <summary>
    /// Boss trophies (pure): granted once a boss has been defeated, never lost afterwards (D10). The same rule runs when a
    /// delve completes and when a save loads, so a save that beat the boss before trophies existed (4e) gets it exactly
    /// once, and both paths end in the same inventory.
    /// </summary>
    public static class TrophyRules
    {
        /// <summary>Gives every earned trophy not yet owned; the first one given waits for its homecoming. Returns the ids given.</summary>
        public static List<string> GrantEarned(GameState state, IEnumerable<BossTrophy> trophies)
        {
            var given = new List<string>();
            if (state == null || trophies == null) return given;
            foreach (BossTrophy t in trophies)
            {
                if (t?.trophy == null || string.IsNullOrEmpty(t.bossId) || state.TimesDefeated(t.bossId) <= 0) continue;
                if (state.Furniture.OwnedCount(t.trophy.id) > 0) continue;
                if (!state.Furniture.Receive(t.trophy)) continue;
                given.Add(t.trophy.id);
                state.Furniture.PendingHomecoming ??= t.trophy.id;
            }
            return given;
        }

        /// <summary>The trophy a boss's defeat earns that isn't owned yet (for the delve result's line), or null.</summary>
        public static FurnitureDefinition Unearned(GameState state, IEnumerable<BossTrophy> trophies, string bossId)
        {
            if (state == null || trophies == null) return null;
            foreach (BossTrophy t in trophies)
                if (t?.trophy != null && t.bossId == bossId && state.Furniture.OwnedCount(t.trophy.id) == 0) return t.trophy;
            return null;
        }
    }
}
