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

        /// <summary>
        /// Tracks the walk to a part (4e playtest: a part against a pillar can be out of the troll's reach): progress is getting
        /// <paramref name="step"/> closer than its best so far. Returns true when it has gone <paramref name="giveUpSeconds"/>
        /// without progress and should give up on the part.
        /// </summary>
        public static bool Stalled(ref float best, ref float stuckFor, float distance, float deltaTime, float step, float giveUpSeconds)
        {
            if (distance <= best - step)
            {
                best = distance;
                stuckFor = 0f;
                return false;
            }
            stuckFor += deltaTime;
            return stuckFor >= giveUpSeconds;
        }

        /// <summary>Whether a slam may shake parts loose now: the interval has passed and the floor isn't already full.</summary>
        public static bool MayDrop(float secondsSinceLastDrop, float dropEvery, int onFloor, int maxOnFloor) =>
            secondsSinceLastDrop >= dropEvery && onFloor < maxOnFloor;
    }
}
