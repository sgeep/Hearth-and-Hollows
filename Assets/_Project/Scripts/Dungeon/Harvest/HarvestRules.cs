using System;
using System.Collections.Generic;
using Hearthdelve.Core.Random;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Run;
using UnityEngine;

namespace Hearthdelve.Dungeon.Harvest
{
    /// <summary>Tuning for how a kill affects part quality (GDD §4.3).</summary>
    [Serializable]
    public struct HarvestRuleSettings
    {
        [Tooltip("Overkill ≥ this fraction of max health counts as an Overkill (quality penalty).")]
        [Min(0)] public float overkillRatio;
        [Tooltip("Overkill ≥ this fraction of max health may destroy parts outright.")]
        [Min(0)] public float destroyRatio;
        [Range(0, 1), Tooltip("Chance each part is destroyed on a heavy overkill.")]
        public float destroyChance;
        [Tooltip("Tiers gained on a Clean Kill.")]
        public int cleanKillTiers;
        [Tooltip("Tiers lost on an Overkill.")]
        public int overkillTiers;

        public static HarvestRuleSettings Default => new()
        {
            overkillRatio = 0.5f,
            destroyRatio = 1.5f,
            destroyChance = 0.5f,
            cleanKillTiers = 1,
            overkillTiers = 1,
        };
    }

    /// <summary>How the monster died.</summary>
    public struct KillContext
    {
        public Element Element;
        public IngredientCategory CleanKillCategories;
        /// <summary>Damage dealt beyond the health the monster had left.</summary>
        public float Overkill;
        public float MaxHealth;
        public bool IsFinisher;

        public float OverkillRatio => MaxHealth > 0f ? Overkill / MaxHealth : 0f;
    }

    /// <summary>One entry in a monster's harvest profile.</summary>
    [Serializable]
    public sealed class HarvestPart
    {
        public IngredientDefinition ingredient;
        public Quality baseQuality = Quality.Standard;
        [Range(0, 1)] public float dropChance = 1f;
        [Min(1)] public int minCount = 1;
        [Min(1)] public int maxCount = 1;
    }

    public readonly struct HarvestDrop
    {
        public readonly IngredientItem Item;
        /// <summary>0 when the part was destroyed by overkill.</summary>
        public readonly int Count;
        public readonly HarvestFlags Flags;

        public HarvestDrop(IngredientItem item, int count, HarvestFlags flags)
        {
            Item = item;
            Count = count;
            Flags = flags;
        }

        public bool Destroyed => (Flags & HarvestFlags.Destroyed) != 0;
    }

    /// <summary>
    /// Harvest rules: base quality, +tiers for a Clean Kill (weapon suits the part's category),
    /// −tiers for Overkill, possible destruction on heavy overkill, finisher guarantees Premium,
    /// and the killing element sets prep state (Fire → Seared, Ice → Chilled, Poison → Inedible).
    /// </summary>
    public static class HarvestRules
    {
        public static PrepState PrepFor(Element element) => element switch
        {
            Element.Fire => PrepState.Seared,
            Element.Ice => PrepState.Chilled,
            Element.Poison => PrepState.Inedible,
            _ => PrepState.Raw,
        };

        /// <summary>
        /// Would a hit of <paramref name="finishingDamage"/> kill without an Overkill penalty?
        /// Because the threshold is a fraction of max health, big hits only overkill on
        /// nearly-dead or fragile monsters; on tougher ones a heavy finisher stays clean.
        /// </summary>
        public static bool KillsWithoutOverkill(float currentHealth, float maxHealth, float finishingDamage, in HarvestRuleSettings settings)
        {
            if (currentHealth <= 0f || maxHealth <= 0f || finishingDamage < currentHealth) return false;
            return (finishingDamage - currentHealth) / maxHealth < settings.overkillRatio;
        }

        /// <summary>Does the weapon earn a Clean Kill bonus on any part this monster can drop?</summary>
        public static bool HasCleanKillAffinity(IReadOnlyList<HarvestPart> parts, IngredientCategory cleanKillCategories)
        {
            if (parts == null) return false;
            foreach (var part in parts)
                if (part?.ingredient != null && (part.ingredient.category & cleanKillCategories) != 0) return true;
            return false;
        }

        /// <summary>
        /// The clean-kill cue: the weapon's lightest hit would finish the monster without
        /// overkill, and the weapon suits at least one of its parts.
        /// </summary>
        public static bool InCleanKillRange(float currentHealth, float maxHealth, float lightestHit,
            IReadOnlyList<HarvestPart> parts, IngredientCategory cleanKillCategories, in HarvestRuleSettings settings) =>
            HasCleanKillAffinity(parts, cleanKillCategories) &&
            KillsWithoutOverkill(currentHealth, maxHealth, lightestHit, settings);

        public static List<HarvestDrop> Resolve(IReadOnlyList<HarvestPart> parts, in KillContext kill, in HarvestRuleSettings settings, IRandom random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            var drops = new List<HarvestDrop>();
            if (parts == null) return drops;

            float ratio = kill.OverkillRatio;
            bool overkill = !kill.IsFinisher && ratio >= settings.overkillRatio;
            bool heavy = !kill.IsFinisher && ratio >= settings.destroyRatio;
            var prep = PrepFor(kill.Element);

            foreach (var part in parts)
            {
                if (part == null || part.ingredient == null) continue;
                if (random.Value() >= part.dropChance) continue;

                var flags = HarvestFlags.None;
                bool clean = (kill.CleanKillCategories & part.ingredient.category) != 0;
                if (clean) flags |= HarvestFlags.CleanKill;
                if (overkill) flags |= HarvestFlags.Overkill;

                Quality quality;
                if (kill.IsFinisher)
                {
                    flags |= HarvestFlags.Finisher;
                    quality = Quality.Premium;
                }
                else
                {
                    int tiers = (clean ? settings.cleanKillTiers : 0) - (overkill ? settings.overkillTiers : 0);
                    quality = part.baseQuality.Shift(tiers);
                }

                var item = new IngredientItem(part.ingredient, quality, prep);

                if (heavy && random.Value() < settings.destroyChance)
                {
                    drops.Add(new HarvestDrop(item, 0, flags | HarvestFlags.Destroyed));
                    continue;
                }

                int count = random.Range(part.minCount, Math.Max(part.minCount, part.maxCount));
                drops.Add(new HarvestDrop(item, count, flags));
            }
            return drops;
        }
    }
}
