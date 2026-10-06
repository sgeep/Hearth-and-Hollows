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
    /// </list>
    /// </summary>
    /// <remarks>Called only by reflection from Lua: <see cref="PreserveAttribute"/> keeps the web build's code stripping off them.</remarks>
    [Preserve]
    public static class StoryLua
    {
        public static readonly string[] Names = { "HH_Affinity", "HH_Respect", "HH_Remembers", "HH_QuestState", "HH_GiveQuest", "HH_PlayerName", "HH_Day", "HH_TimesDefeated" };

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

        [Preserve] public static void HH_GiveQuest(string questId, string giverId) => Host?.Quests?.Give(questId, giverId);

        [Preserve] public static string HH_PlayerName() => Game?.Story.Player?.name ?? PlayerProfile.DefaultName;

        [Preserve] public static double HH_Day() => Game?.Day ?? 1;

        [Preserve] public static double HH_TimesDefeated(string bossId) => Game?.TimesDefeated(bossId) ?? 0;

        /// <summary>The registered names (tests).</summary>
        public static IReadOnlyList<string> All => Names;
    }
}
