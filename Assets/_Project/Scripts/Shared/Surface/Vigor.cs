using System;
using UnityEngine;

namespace Hearthdelve.Shared.Surface
{
    /// <summary>The strenuous daytime work that costs Vigor (4h Checkpoint B). Later activities add an entry and a cost.</summary>
    public enum VigorActivity
    {
        /// <summary>Prepare a garden bed and plant it.</summary>
        PlantBed,
        /// <summary>Tend a growing bed (once a day per bed).</summary>
        TendBed,
        /// <summary>Harvest a ready bed.</summary>
        HarvestBed,
    }

    /// <summary>Vigor's tuning: the day's capacity and each activity's cost (B playtest values, not balance).</summary>
    [Serializable]
    public struct VigorSettings
    {
        [Min(1), Tooltip("Vigor each day (pips on the HUD).")]
        public int maxVigor;
        [Min(0)] public int plantBed;
        [Min(0)] public int tendBed;
        [Min(0)] public int harvestBed;

        public static VigorSettings Default => new() { maxVigor = 6, plantBed = 2, tendBed = 1, harvestBed = 0 };

        public int Cost(VigorActivity activity) => Math.Max(0, activity switch
        {
            VigorActivity.PlantBed => plantBed,
            VigorActivity.TendBed => tendBed,
            VigorActivity.HarvestBed => harvestBed,
            _ => 0,
        });
    }

    /// <summary>
    /// Vigor (4h Checkpoint B): the keeper's capacity for strenuous optional work on the surface today. Separate from Essence,
    /// which is the Hollows' life and time. Only what's been spent today is state (saved); the day's maximum comes from the
    /// tuning, so a change of tuning never strands a save. Refilled when the keeper sleeps, nowhere else. At 0 nothing ends,
    /// stops or weakens: only strenuous work is refused.
    /// </summary>
    public sealed class Vigor
    {
        public int Max { get; private set; } = VigorSettings.Default.maxVigor;
        public int Spent { get; private set; }
        public int Current => Math.Max(0, Max - Spent);
        public bool IsEmpty => Current == 0;

        /// <summary>The day's capacity from the tuning (spent Vigor is kept).</summary>
        public void Configure(in VigorSettings settings) => Max = Math.Max(1, settings.maxVigor);

        public bool CanAfford(int cost) => cost <= 0 || Current >= cost;

        /// <summary>Spends <paramref name="cost"/>; refused (and nothing spent) when it can't be afforded.</summary>
        public bool Spend(int cost)
        {
            if (cost <= 0) return true;
            if (!CanAfford(cost)) return false;
            Spent += cost;
            return true;
        }

        /// <summary>A night's sleep: full again.</summary>
        public void Refill() => Spent = 0;

        /// <summary>From a save.</summary>
        public void Restore(int spent) => Spent = Math.Clamp(spent, 0, Max);
    }
}
