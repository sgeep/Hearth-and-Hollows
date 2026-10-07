using System.Collections.Generic;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Surface;
using UnityEngine;

namespace Hearthdelve.Shared.Village
{
    /// <summary>
    /// The village right now (4h Checkpoint C): who is where, from the game's day, clock, story and world seed and the cast's
    /// schedules. Read by the village's presence, the story's <c>HH_Doing</c> and the tests; it holds no state of its own.
    /// </summary>
    public static class VillageLife
    {
        /// <summary>Tests: a schedule list to use instead of the database's.</summary>
        public static IReadOnlyList<ScheduleDefinition> SchedulesOverride { get; set; }

        static GameDatabase Database => GameFlow.Instance != null ? GameFlow.Instance.Database : null;

        public static VillageLifeSettings Settings => Database != null && Database.villageLife != null ? Database.villageLife.settings : VillageLifeSettings.Default;

        public static IReadOnlyList<ScheduleDefinition> Schedules =>
            SchedulesOverride ?? (Database != null ? Database.schedules : (IReadOnlyList<ScheduleDefinition>)System.Array.Empty<ScheduleDefinition>());

        public static ScheduleDefinition Schedule(string character)
        {
            foreach (ScheduleDefinition s in Schedules)
                if (s != null && s.character == character) return s;
            return null;
        }

        /// <summary>Today, for the schedules (null outside a game).</summary>
        public static ScheduleWorld? World()
        {
            GameFlow flow = GameFlow.Instance;
            if (flow == null || !flow.InGame) return null;
            GameState state = flow.State;
            return new ScheduleWorld(state.Day, state.WorldSeed, state.Story.OpeningComplete, Settings, id => state.QuestObjects.Status(id) switch
            {
                Quests.QuestObjectStatus.Wanted => "wanted",
                Quests.QuestObjectStatus.Home => "home",
                Quests.QuestObjectStatus.Delivered => "delivered",
                _ => "none",
            });
        }

        /// <summary>The minute the village lives by: the clock's displayed step (routines change on it, never between).</summary>
        public static int Minute => SurfaceTime.ShownMinute;

        /// <summary>Where <paramref name="character"/> is now, or null (no schedule, or nowhere to be seen).</summary>
        public static ScheduleBlock Now(string character)
        {
            ScheduleWorld? world = World();
            return world == null ? null : ScheduleRules.Resolve(Schedule(character), world.Value, Minute);
        }

        /// <summary>What they're doing now (the block's activity), or empty.</summary>
        public static string Doing(string character) => Now(character)?.activity ?? string.Empty;
    }
}
