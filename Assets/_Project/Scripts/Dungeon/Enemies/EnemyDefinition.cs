using System;
using System.Collections.Generic;
using Hearthdelve.Dungeon.Harvest;
using UnityEngine;
using UnityEngine.Localization;

namespace Hearthdelve.Dungeon.Enemies
{
    /// <summary>Timing and shape of an enemy's attack. Behaviour-specific fields are ignored by others.</summary>
    [Serializable]
    public sealed class EnemyAttackSettings
    {
        [Header("Timing (seconds)")]
        [Min(0), Tooltip("Wind-up the player can read and react to.")]
        public float telegraph = 0.55f;
        [Min(0)] public float active = 0.25f;
        [Min(0)] public float recovery = 0.4f;
        [Min(0)] public float cooldown = 1.2f;

        [Header("Hit")]
        [Min(0)] public float damage = 12f;
        public Vector2 hitboxOffset = new(0.6f, 0.4f);
        public Vector2 hitboxSize = new(0.9f, 0.7f);
        public Vector2 knockback = new(6f, 5f);

        [Header("Lunge (Rat)")]
        public float lungeSpeed = 11f;

        [Header("Leap (Slime)")]
        [Min(0)] public float leapHeight = 2.5f;
        [Min(0)] public float leapMaxDistance = 5f;

        [Header("Projectile (Shroom)")]
        [Min(0.1f)] public float projectileFlightTime = 0.8f;
        public Vector2 projectileSpawnOffset = new(0.3f, 1.0f);
    }

    /// <summary>Combat and harvest profile of one monster type (GDD §4.7).</summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Enemy Definition", fileName = "Enemy_")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        public string id;
        public LocalizedString displayName;

        [Header("Stats")]
        [Min(1)] public float maxHealth = 20f;
        [Min(0)] public float moveSpeed = 2.5f;
        [Min(0)] public float gravity = 40f;
        [Min(0)] public float aggroRange = 7f;
        [Min(0), Tooltip("Starts an attack when the player is within this horizontal distance.")]
        public float attackRange = 2.5f;
        [Min(0)] public float knockbackMultiplier = 1f;
        [Tooltip("Hits during a telegraph/attack don't interrupt it (heavy enemies).")]
        public bool superArmorWhileAttacking;
        [Tooltip("Health refills when not hit for a while (training dummy).")]
        public bool regenerates;

        public EnemyAttackSettings attack = new();

        [Header("Harvest")]
        public List<HarvestPart> harvest = new();

        [Header("Placeholder")]
        public Color placeholderColor = Color.gray;
    }
}
