using System;
using UnityEngine;

namespace Hearthdelve.Dungeon.Harvest
{
    /// <summary>The Harvest Finisher's tuning (4e step 3, GDD §4.3).</summary>
    [Serializable]
    public struct FinisherSettings
    {
        [Range(0f, 1f), Tooltip("An enemy is low enough to finish at or under this share of its health...")]
        public float lowFraction;
        [Min(0f), Tooltip("...or at or under this much health, whichever is more.")]
        public float lowHealth;
        [Min(0.1f), Tooltip("Seconds after the player's last hit on it that it can be finished: the moment to take.")]
        public float window;
        [Min(0.5f), Tooltip("How close the player must be, in tiles.")]
        public float reach;
        [Min(0.1f), Tooltip("Seconds the player is committed to the finisher (no dodge, no i-frames: the risk).")]
        public float lockSeconds;
        [Min(0f), Tooltip("Seconds into the finisher that the blow lands.")]
        public float strikeAt;
        [Min(0.5f), Tooltip("A boss brought down (lethal damage): seconds it stays down for its optional finishing moment.")]
        public float bossDownedSeconds;

        public static FinisherSettings Default => new()
        {
            lowFraction = 0.25f,
            lowHealth = 15f,
            window = 1.2f,
            reach = 1.8f,
            lockSeconds = 0.5f,
            strikeAt = 0.2f,
            bossDownedSeconds = 3f,
        };
    }

    /// <summary>
    /// Pure rules for the Harvest Finisher (4e step 3): an ordinary enemy can be finished while it's low and the player's
    /// last hit on it is fresh; a boss only once it has taken lethal damage (it never skips boss health).
    /// </summary>
    public static class FinisherRules
    {
        public static bool IsLow(float health, float maxHealth, in FinisherSettings s) =>
            health > 0f && health <= Math.Max(maxHealth * s.lowFraction, s.lowHealth);

        public static bool Eligible(float health, float maxHealth, float secondsSinceHit, in FinisherSettings s) =>
            IsLow(health, maxHealth, s) && secondsSinceHit >= 0f && secondsSinceHit <= s.window;
    }
}
