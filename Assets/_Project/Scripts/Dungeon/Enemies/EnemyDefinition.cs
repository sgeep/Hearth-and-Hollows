using System;
using System.Collections.Generic;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Shared.Animation;
using UnityEngine;
using UnityEngine.Localization;

namespace Hearthdelve.Dungeon.Enemies
{
    /// <summary>What an enemy attack does in its active phase.</summary>
    public enum EnemyAttackKind
    {
        /// <summary>Jumps at where the player was when the telegraph started, hurting on contact (slime).</summary>
        Leap,
        /// <summary>Dashes along the telegraphed line, hurting on contact (bat).</summary>
        Swoop,
        /// <summary>A short-range hitbox in front of the enemy (spider).</summary>
        Bite,
        /// <summary>Fires a projectile at where the player was when the telegraph started (spider web).</summary>
        Spit,
    }

    /// <summary>
    /// One telegraphed enemy attack: Telegraph → Active → Recovery → Cooldown (see
    /// <see cref="AttackCycle"/>). Fields for other kinds are ignored.
    /// </summary>
    [Serializable]
    public sealed class EnemyAttackSettings
    {
        public string debugName = "Attack";
        public EnemyAttackKind kind = EnemyAttackKind.Leap;

        [Header("When")]
        [Min(0), Tooltip("Starts only when the player is at least this far away (tiles, feet to feet).")]
        public float minRange;
        [Min(0), Tooltip("Starts only when the player is at most this far away.")]
        public float maxRange = 3f;

        [Header("Timing (seconds)")]
        [Min(0), Tooltip("Wind-up the player can read and react to. Nothing can hurt the player before it ends.")]
        public float telegraph = 0.55f;
        [Min(0)] public float active = 0.25f;
        [Min(0)] public float recovery = 0.4f;
        [Min(0)] public float cooldown = 1.2f;

        [Header("Hit")]
        [Min(0)] public float damage = 12f;
        [Tooltip("Bite: hitbox centre along the attack direction (x) and up (y). Others: the body's hitbox offset.")]
        public Vector2 hitboxOffset = new(0f, 0.2f);
        public Vector2 hitboxSize = new(0.9f, 0.7f);

        [Header("Animation")]
        [Tooltip("The animation played across the telegraph and the attack.")]
        public CharacterAnim animation = CharacterAnim.Attack;
        [Min(0), Tooltip("The animation frame the attack lands on: earlier frames are spread across the telegraph.")]
        public int releaseFrame = 1;

        [Header("Leap / Swoop")]
        [Min(0), Tooltip("Leap: the most ground covered. Swoop: speed is distance over the active time.")]
        public float travelDistance = 4f;
        [Min(0), Tooltip("Leap: how high the body arcs, in tiles (visual only).")]
        public float arcHeight = 0.8f;

        [Header("Spit")]
        [Min(0.1f)] public float projectileSpeed = 7f;
        [Min(0.1f)] public float projectileRange = 8f;
        [Tooltip("Where the projectile starts, along the attack direction (x) and up (y).")]
        public Vector2 projectileSpawnOffset = new(0.5f, 0.3f);
    }

    /// <summary>Combat and harvest profile of one monster type (GDD §4.7).</summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Enemy Definition", fileName = "Enemy_")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        /// <summary>Data version, so the generator fills in new fields once without overwriting tuning.</summary>
        [HideInInspector] public int schema;

        public string id;
        public LocalizedString displayName;

        [Header("Stats")]
        [Min(1)] public float maxHealth = 20f;
        [Min(0)] public float moveSpeed = 2.5f;
        [Min(0)] public float aggroRange = 7f;
        [Min(0), Tooltip("Scales the knockback this enemy takes.")]
        public float knockbackMultiplier = 1f;
        [Min(0), Tooltip("Scales how long hits stagger this enemy.")]
        public float staggerMultiplier = 1f;
        [Tooltip("Hits during an attack don't interrupt it or knock the enemy back (the slime's leap).")]
        public bool superArmorWhileAttacking;

        [Header("Movement")]
        [Tooltip("Keeps between these distances from the player (min, max). Zero: closes in.")]
        public Vector2 keepDistance;
        [Range(0f, 1f), Tooltip("Erratic side-to-side wobble while chasing (the bat).")]
        public float flutter;
        [Tooltip("Hangs asleep until the player comes within wake range or hits it (the bat).")]
        public bool startsAsleep;
        [Min(0)] public float wakeRange = 3.5f;

        [Header("Attacks")]
        public EnemyAttackSettings attack = new();
        [Tooltip("More attacks, each chosen when the player is in its range (the spider's web spit).")]
        public List<EnemyAttackSettings> otherAttacks = new();

        [Header("Harvest")]
        public List<HarvestPart> harvest = new();

        [Header("Placeholder")]
        public Color placeholderColor = Color.gray;

        /// <summary>Every attack, primary first.</summary>
        public IEnumerable<EnemyAttackSettings> AllAttacks
        {
            get
            {
                if (attack != null) yield return attack;
                foreach (EnemyAttackSettings other in otherAttacks)
                    if (other != null) yield return other;
            }
        }
    }
}
