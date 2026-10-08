using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Shared.Village
{
    /// <summary>Tuning for village life (4h Checkpoint C; first-pass values, not balance).</summary>
    [Serializable]
    public struct VillageLifeSettings
    {
        [Min(1), Tooltip("Kaloren brings Ogrin herbs once in this many days (locked canon: 3).")]
        public int herbEveryDays;
        [Range(0f, 1f), Tooltip("How often Ogrin is well enough to be out, on an ordinary day.")]
        public float ogrinWellChance;
        [Range(0f, 1f), Tooltip("The same, the day after the herbs (they ease him; they never cure him).")]
        public float ogrinWellAfterHerbs;
        [Range(0f, 1f), Tooltip("How often Maximo keeps his evening vigil at the memorial instead of going home.")]
        public float maximoVigilChance;
        [Min(0.1f), Tooltip("How fast villagers walk (tiles a second; the keeper walks about 4).")]
        public float walkSpeed;
        [Min(0f), Tooltip("Villagers walk to their next place only when the keeper is this close (tiles) to either end; otherwise they're simply there.")]
        public float seenWithin;
        [Min(1f), Tooltip("A walk that takes longer than this (a blocked path) ends with them there.")]
        public float longestWalkSeconds;
        [Range(0f, 1f), Tooltip("4h Checkpoint D: how often Gimp would come up on a given day (never two days running, so fewer in practice).")]
        public float gimpVisitChance;
        [Range(0f, 1f), Tooltip("4h Checkpoint D: how often a light shows at Ogrin's window of an evening.")]
        public float glimmerChance;

        public static VillageLifeSettings Default => new()
        {
            herbEveryDays = 3,
            ogrinWellChance = 0.6f,
            ogrinWellAfterHerbs = 0.85f,
            maximoVigilChance = 0.35f,
            walkSpeed = 2.2f,
            seenWithin = 26f,
            longestWalkSeconds = 30f,
            gimpVisitChance = 0.4f,
            glimmerChance = 0.25f,
        };
    }

    /// <summary>What a schedule can ask about today: the day, the world seed, the story so far, the village's tuning.</summary>
    public readonly struct ScheduleWorld
    {
        public readonly int Day;
        public readonly int Seed;
        public readonly bool OpeningComplete;
        public readonly VillageLifeSettings Settings;
        readonly Func<string, string> m_QuestObject;
        readonly Func<string, bool> m_Beat;

        public ScheduleWorld(int day, int seed, bool openingComplete, VillageLifeSettings settings, Func<string, string> questObjectStatus = null,
            Func<string, bool> beatSeen = null)
        {
            Day = day;
            Seed = seed;
            OpeningComplete = openingComplete;
            Settings = settings;
            m_QuestObject = questObjectStatus;
            m_Beat = beatSeen;
        }

        public string QuestObjectStatus(string id) => m_QuestObject != null ? m_QuestObject(id) ?? "none" : "none";
        public bool BeatSeen(string id) => m_Beat != null && id != null && m_Beat(id);
    }

    /// <summary>
    /// The village's seeded days (4h Checkpoint C): pure functions of the day and the world seed (save version 10's), never of
    /// the clock or a random generator, so a reload of the same day finds the same village.
    /// </summary>
    public static class VillageDays
    {
        const uint k_Herbs = 0x48455242;   // "HERB"
        const uint k_Ogrin = 0x4F475249;   // "OGRI"
        const uint k_Vigil = 0x5649474C;   // "VIGL"
        const uint k_Gimp = 0x47494D50;    // "GIMP"
        const uint k_Glimmer = 0x474C494D; // "GLIM"

        /// <summary>A well-mixed number from the seed, the day and a salt (the same inputs, the same number, on every platform).</summary>
        public static uint Hash(int seed, int day, uint salt)
        {
            unchecked
            {
                uint h = (uint)seed * 0x9E3779B1u ^ (uint)day * 0x85EBCA77u ^ salt * 0xC2B2AE3Du;
                h ^= h >> 16;
                h *= 0x7FEB352Du;
                h ^= h >> 15;
                h *= 0x846CA68Bu;
                h ^= h >> 16;
                return h;
            }
        }

        /// <summary>A number in [0, 1) from the seed, the day and a salt.</summary>
        public static float Unit(int seed, int day, uint salt) => (Hash(seed, day, salt) >> 8) / (float)(1 << 24);

        /// <summary>Kaloren's herb day: exactly once every <c>herbEveryDays</c>, the first one placed by the world seed.</summary>
        public static bool HerbDay(int seed, int day, in VillageLifeSettings s)
        {
            int period = Math.Max(1, s.herbEveryDays);
            int offset = (int)(Hash(seed, 0, k_Herbs) % (uint)period);
            return ((day + offset) % period + period) % period == 0;
        }

        /// <summary>Ogrin is well enough to be out: seeded, more likely the day after the herbs. Never always: nothing here cures him.</summary>
        public static bool OgrinWell(int seed, int day, in VillageLifeSettings s)
        {
            float chance = HerbDay(seed, day - 1, s) ? s.ogrinWellAfterHerbs : s.ogrinWellChance;
            return Unit(seed, day, k_Ogrin) < Mathf.Min(chance, 0.95f);
        }

        public static bool MaximoVigil(int seed, int day, in VillageLifeSettings s) => Unit(seed, day, k_Vigil) < s.maximoVigilChance;

        /// <summary>
        /// Gimp comes up (4h Checkpoint D): a seeded roll each day, but never two days running, so the gaps are uneven (one day,
        /// three, five…) and nobody can set a clock by him. Pure and deterministic: worked forward from a few days back.
        /// </summary>
        public static bool GimpVisit(int seed, int day, in VillageLifeSettings s)
        {
            bool yesterday = false;
            for (int d = Math.Max(1, day - 14); d <= day; d++)
            {
                bool today = !yesterday && Unit(seed, d, k_Gimp) < s.gimpVisitChance;
                if (d == day) return today;
                yesterday = today;
            }
            return false;
        }

        /// <summary>A light at Ogrin's window this evening (4h Checkpoint D: seeded, now and then; never explained).</summary>
        public static bool GlimmerEvening(int seed, int day, in VillageLifeSettings s) => Unit(seed, day, k_Glimmer) < s.glimmerChance;

        public static bool Holds(DayRule rule, in ScheduleWorld world) => rule switch
        {
            DayRule.HerbDay => HerbDay(world.Seed, world.Day, world.Settings),
            DayRule.OgrinWell => OgrinWell(world.Seed, world.Day, world.Settings),
            DayRule.MaximoVigil => MaximoVigil(world.Seed, world.Day, world.Settings),
            DayRule.GimpVisit => GimpVisit(world.Seed, world.Day, world.Settings),
            DayRule.GlimmerEvening => GlimmerEvening(world.Seed, world.Day, world.Settings),
            _ => false,
        };
    }

    /// <summary>
    /// Where someone is (4h Checkpoint C): the first block of their schedule whose time and conditions match. Pure; the whole
    /// village is a function of day + minute + story + world seed, so no villager's position is ever saved.
    /// </summary>
    public static class ScheduleRules
    {
        public static ScheduleBlock Resolve(IReadOnlyList<ScheduleBlock> blocks, in ScheduleWorld world, int minute)
        {
            if (blocks == null) return null;
            foreach (ScheduleBlock block in blocks)
                if (block != null && block.Covers(minute) && Holds(block.conditions, world)) return block;
            return null;
        }

        public static ScheduleBlock Resolve(ScheduleDefinition schedule, in ScheduleWorld world, int minute) =>
            schedule != null ? Resolve(schedule.blocks, world, minute) : null;

        public static bool Holds(IReadOnlyList<ScheduleCondition> conditions, in ScheduleWorld world)
        {
            if (conditions == null) return true;
            foreach (ScheduleCondition c in conditions)
                if (Holds(c, world) == c.negate) return false;
            return true;
        }

        static bool Holds(in ScheduleCondition c, in ScheduleWorld world) => c.kind switch
        {
            ScheduleConditionKind.Day => VillageDays.Holds(c.rule, world),
            ScheduleConditionKind.DayAtLeast => world.Day >= c.number,
            ScheduleConditionKind.OpeningComplete => world.OpeningComplete,
            ScheduleConditionKind.QuestObject => string.Equals(world.QuestObjectStatus(c.id), c.status ?? "none", StringComparison.OrdinalIgnoreCase),
            ScheduleConditionKind.Beat => world.BeatSeen(c.id),
            _ => false,
        };

        /// <summary>
        /// Every place they move to between two minutes, in order (the first is where they start). For tuning and the tests that
        /// keep a four-minute day relaxed: a typical villager has two to four.
        /// </summary>
        public static List<ScheduleBlock> Day(IReadOnlyList<ScheduleBlock> blocks, in ScheduleWorld world, int fromMinute, int toMinute)
        {
            var visits = new List<ScheduleBlock>();
            ScheduleBlock last = null;
            for (int m = fromMinute; m <= toMinute; m++)
            {
                ScheduleBlock now = Resolve(blocks, world, m);
                if (now == last) continue;
                if (last == null || now == null || now.anchor != last.anchor || now.activity != last.activity) visits.Add(now);
                last = now;
            }
            return visits;
        }
    }
}
