using System;
using Hearthdelve.Shared.Ingredients;
using UnityEngine;

namespace Hearthdelve.Dungeon.Combat
{
    /// <summary>Everything a hit carries into the damage pipeline and, on a kill, into Harvest.</summary>
    public struct DamageInfo
    {
        public float Amount;
        public Element Element;
        /// <summary>Part categories this weapon cuts cleanly (e.g. Cleaver → Meat | Offal).</summary>
        public IngredientCategory CleanKillCategories;
        /// <summary>World-space knockback; x is already signed away from the attacker.</summary>
        public Vector2 Knockback;
        public float StaggerTime;
        public float HitStop;
        public float ScreenShake;
        public bool IsFinisher;
        public Vector2 HitPoint;
        public GameObject Instigator;
    }

    public static class DamageCalculator
    {
        /// <summary>Scale a base damage value by a multiplier (weapon rarity, affixes, buffs later).</summary>
        public static float Scale(float baseDamage, float multiplier) => Math.Max(0f, baseDamage * multiplier);
    }

    /// <summary>Anything that can receive hits (player, enemies, breakables).</summary>
    public interface IDamageable
    {
        /// <summary>Returns false if the hit was ignored (i-frames, already dead).</summary>
        bool ReceiveHit(in DamageInfo hit);
        /// <summary>Which side this belongs to, so hitboxes don't hurt their owner's team.</summary>
        Team Team { get; }
    }

    public enum Team
    {
        Player,
        Enemy,
    }
}
