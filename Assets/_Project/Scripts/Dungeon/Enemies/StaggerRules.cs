using System;

namespace Hearthdelve.Dungeon.Enemies
{
    /// <summary>
    /// The simple hit response (4b): a hit pushes the enemy back in a short slide, interrupts its
    /// attack and holds it still for a moment, unless it has super armour while attacking.
    /// Deliberately not a poise system.
    /// </summary>
    public static class StaggerRules
    {
        /// <summary>How long a knockback slide lasts, in seconds.</summary>
        public const float KnockbackDuration = 0.15f;

        /// <summary>The slide's speed (tiles per second) at <paramref name="time"/>: starts at the force and falls to zero.</summary>
        public static float KnockbackSpeed(float force, float multiplier, float time)
        {
            if (time < 0f || time >= KnockbackDuration) return 0f;
            return Math.Max(0f, force) * Math.Max(0f, multiplier) * (1f - time / KnockbackDuration);
        }

        /// <summary>How far the slide goes in open ground, in tiles.</summary>
        public static float KnockbackDistance(float force, float multiplier) => Math.Max(0f, force) * Math.Max(0f, multiplier) * KnockbackDuration * 0.5f;

        /// <summary>Whether a hit is shrugged off: super armour only counts while an attack is under way.</summary>
        public static bool IgnoresHit(bool superArmorWhileAttacking, bool attacking) => superArmorWhileAttacking && attacking;

        /// <summary>When the stagger ends: a new hit extends it, but never shortens it, and hits don't add up.</summary>
        public static float StaggerUntil(float currentUntil, float now, float staggerTime, float multiplier) =>
            Math.Max(currentUntil, now + Math.Max(0f, staggerTime) * Math.Max(0f, multiplier));
    }
}
