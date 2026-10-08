using System.Collections.Generic;
using System.Reflection;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Story;
using PixelCrushers.DialogueSystem;
using UnityEngine.Scripting;

namespace Hearthdelve.Story.Dialogue
{
    /// <summary>
    /// The functions conversations use to ask about the game and act on it (4g, plan §4), registered with the Dialogue System's
    /// Lua. They read Hearth &amp; Hollows state at the moment of asking (nothing is copied into Lua variables, so nothing goes stale)
    /// and act only through Hearth &amp; Hollows adapters. Conversations never call Love/Hate or Quest Machine directly: what they
    /// see here survives either being replaced behind its adapter.
    /// <list type="bullet">
    /// <item><c>HH_Affinity("gunta")</c>, <c>HH_Respect("gunta")</c>: how much they like and respect the player (−100 to 100).</item>
    /// <item><c>HH_Remembers("gunta", "displayed_trophy")</c>: they remember the player doing that deed.</item>
    /// <item><c>HH_QuestState("proof_trophy_wall")</c>: "unassigned", "active", "successful", "failed"…</item>
    /// <item><c>HH_GiveQuest("proof_trophy_wall", "gunta")</c>: gives the player the quest, from that character.</item>
    /// <item><c>HH_PlayerName()</c>, <c>HH_Day()</c>, <c>HH_TimesDefeated("larder_troll")</c>.</item>
    /// <item>4g Checkpoint B: <c>HH_OpeningStage()</c> ("Arrival", "FirstDelve", "Homecoming", "FirstEvening", "Complete");
    /// <c>HH_QuestObject("boogs_bomb")</c> ("none", "wanted", "home", "delivered") and <c>HH_HasQuestObject("boogs_bomb")</c> (it's home,
    /// not yet handed over); <c>HH_DeliverQuestObject("boogs_bomb")</c> (hand it over: its reward, once); <c>HH_Deed("returned_boogs_bomb")</c>
    /// (commit a deed to whoever learns of it); <c>HH_PartsHome()</c> (parts the last delve brought home today); <c>HH_Doing(id)</c> and <c>HH_Today(rule)</c> (4h Checkpoint C: the village's day).</item>
    /// </list>
    /// </summary>
    /// <remarks>Called only by reflection from Lua: <see cref="PreserveAttribute"/> keeps the web build's code stripping off them.</remarks>
    [Preserve]
    public static class StoryLua
    {
        public static readonly string[] Names =
        {
            "HH_Affinity", "HH_Respect", "HH_Remembers", "HH_QuestState", "HH_GiveQuest", "HH_PlayerName", "HH_Day", "HH_TimesDefeated",
            "HH_OpeningStage", "HH_QuestObject", "HH_HasQuestObject", "HH_DeliverQuestObject", "HH_Deed", "HH_PartsHome",
            "HH_Doing", "HH_Today",
        };

        static StoryHost Host => StoryHost.Instance;
        static GameState Game => GameFlow.Instance != null ? GameFlow.Instance.State : null;

        public static void Register()
        {
            var flags = BindingFlags.Static | BindingFlags.Public;
            foreach (string name in Names)
            {
                MethodInfo method = typeof(StoryLua).GetMethod(name, flags);
                Lua.RegisterFunction(name, null, method);
            }
        }

        public static void Unregister()
        {
            foreach (string name in Names) Lua.UnregisterFunction(name);
        }

        [Preserve] public static double HH_Affinity(string characterId) => Host != null && Host.Relationships != null ? Host.Relationships.Affinity(characterId) : 0d;

        [Preserve] public static double HH_Respect(string characterId) => Host != null && Host.Relationships != null ? Host.Relationships.Respect(characterId) : 0d;

        [Preserve] public static bool HH_Remembers(string characterId, string deedId) => Host != null && Host.Relationships != null && Host.Relationships.Remembers(characterId, deedId);

        [Preserve] public static string HH_QuestState(string questId) => Host != null && Host.Quests != null ? Host.Quests.State(questId) : "unassigned";

        [Preserve] public static void HH_GiveQuest(string questId, string giverId) => Host?.GiveQuest(questId, giverId);

        [Preserve] public static string HH_PlayerName() => Game?.Story.Player?.name ?? PlayerProfile.DefaultName;

        [Preserve] public static double HH_Day() => Game?.Day ?? 1;

        [Preserve] public static double HH_TimesDefeated(string bossId) => Game?.TimesDefeated(bossId) ?? 0;

        [Preserve] public static string HH_OpeningStage() => (Game?.Story.Opening ?? OpeningStage.Complete).ToString();

        [Preserve] public static string HH_QuestObject(string id) => Game == null ? "none" : Game.QuestObjects.Status(id) switch
        {
            Shared.Quests.QuestObjectStatus.Wanted => "wanted",
            Shared.Quests.QuestObjectStatus.Home => "home",
            Shared.Quests.QuestObjectStatus.Delivered => "delivered",
            _ => "none",
        };

        [Preserve] public static bool HH_HasQuestObject(string id) => Game != null && Game.QuestObjects.IsHome(id);

        [Preserve] public static bool HH_DeliverQuestObject(string id) => GameFlow.Instance != null && GameFlow.Instance.DeliverQuestObject(id);

        [Preserve] public static bool HH_Deed(string deedId) => Host != null && Host.CommitDeed(deedId);

        [Preserve] public static double HH_PartsHome() => Game?.Today.PartsBroughtBack ?? 0;

        /// <summary>
        /// 4h Checkpoint C: what a villager is doing now, from their schedule (an activity word: "herbs", "bed", "lunch"…; empty when
        /// they have none). The schedule decides where people are; the conversation only reads it.
        /// </summary>
        [Preserve] public static string HH_Doing(string characterId) => Shared.Village.VillageLife.Doing(characterId);

        /// <summary>Whether today is one of the village's seeded days: "herbs" (Kaloren's visit), "ogrin_well", "vigil" (4h C); "gimp", "glimmer" (D).</summary>
        [Preserve] public static bool HH_Today(string rule)
        {
            Shared.Village.ScheduleWorld? world = Shared.Village.VillageLife.World();
            if (world == null) return false;
            return rule switch
            {
                "herbs" => Shared.Village.VillageDays.Holds(Shared.Village.DayRule.HerbDay, world.Value),
                "ogrin_well" => Shared.Village.VillageDays.Holds(Shared.Village.DayRule.OgrinWell, world.Value),
                "vigil" => Shared.Village.VillageDays.Holds(Shared.Village.DayRule.MaximoVigil, world.Value),
                // 4h Checkpoint D: both only once Gimp has come up (as the schedules have them).
                "gimp" => world.Value.BeatSeen(Shared.Village.CommunityRules.GimpIntro) && Shared.Village.VillageDays.Holds(Shared.Village.DayRule.GimpVisit, world.Value),
                "glimmer" => world.Value.BeatSeen(Shared.Village.CommunityRules.GimpIntro) && Shared.Village.VillageDays.Holds(Shared.Village.DayRule.GlimmerEvening, world.Value),
                _ => false,
            };
        }

        /// <summary>The registered names (tests).</summary>
        public static IReadOnlyList<string> All => Names;
    }
}
