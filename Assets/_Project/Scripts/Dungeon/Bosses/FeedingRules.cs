using System;

namespace Hearthdelve.Dungeon.Bosses
{
    /// <summary>Pure rules for a boss eating the parts on the floor (4e step 2, the Larder Troll).</summary>
    public static class FeedingRules
    {
        /// <summary>Health one part gives back: a share of the boss's maximum, never past it.</summary>
        public static float Heal(float health, float maxHealth, float fraction) =>
            Math.Max(0f, Math.Min(maxHealth - health, maxHealth * Math.Max(0f, fraction)));

        /// <summary>The meal is spoiled once the damage taken since it began reaches <paramref name="spoilDamage"/>.</summary>
        public static bool Spoiled(float healthAtStart, float healthNow, float spoilDamage) => healthAtStart - healthNow >= spoilDamage;

        /// <summary>Whether a slam may shake parts loose now: the interval has passed and the floor isn't already full.</summary>
        public static bool MayDrop(float secondsSinceLastDrop, float dropEvery, int onFloor, int maxOnFloor) =>
            secondsSinceLastDrop >= dropEvery && onFloor < maxOnFloor;
    }
}
