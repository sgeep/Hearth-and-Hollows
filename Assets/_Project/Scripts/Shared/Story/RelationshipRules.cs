using System.Collections.Generic;
using Hearthdelve.Shared.Characters;
using UnityEngine;

namespace Hearthdelve.Shared.Story
{
    /// <summary>
    /// The Hearth &amp; Hollows side of relationships (4g, pure): which characters learn of a deed, how much Respect it earns,
    /// and the day clock memories age by. Love/Hate does the rest (affinity, acclimatization, memory) through the adapter.
    /// </summary>
    public static class RelationshipRules
    {
        /// <summary>A memory kept "for good" expires on this day, far past any playthrough.</summary>
        public const float Forever = 1000000f;

        /// <summary>
        /// How well a judge's values match what a deed shows, 0–1: Love/Hate's trait alignment (1 − |a − b| / 200, averaged),
        /// over only the traits the deed shows (non-zero), so a judge's other values don't count against it. A deed that shows
        /// nothing matches everyone fully.
        /// </summary>
        public static float Alignment(SocialTraits judge, SocialTraits deed)
        {
            float sum = 0f;
            int shown = 0;
            for (int i = 0; i < SocialTraits.Count; i++)
            {
                if (Mathf.Approximately(deed[i], 0f)) continue;
                sum += 1f - Mathf.Abs(judge[i] - deed[i]) / 200f;
                shown++;
            }
            return shown == 0 ? 1f : sum / shown;
        }

        /// <summary>The share of a deed's respect a judge gives: none at indifference (alignment ½) or below, all of it at a full match.</summary>
        public static float RespectWeight(float alignment) => Mathf.Clamp01((alignment - 0.5f) * 2f);

        /// <summary>The Respect a deed earns from one judge: its base, by how well it matches their values, by how fresh it still is (Love/Hate's acclimatization: 1 the first time).</summary>
        public static float RespectChange(float baseRespect, float alignment, float acclimatization) =>
            baseRespect * RespectWeight(alignment) * Mathf.Clamp01(acclimatization);

        /// <summary>
        /// The day a memory of a deed done on <paramref name="day"/> expires. Memories age by the game's days, never by real or
        /// paused time: a remembered deed lasts <paramref name="memoryDays"/> whole days after the one it happened on.
        /// </summary>
        public static float MemoryExpires(int day, int memoryDays) => memoryDays <= 0 ? Forever : day + memoryDays + 0.5f;

        public static bool IsForgotten(float expires, int day) => day > expires;

        /// <summary>The characters who learn of a deed: the tracked staff, or every tracked character.</summary>
        public static IEnumerable<string> Learners(DeedDefinition deed, IEnumerable<CharacterDefinition> cast)
        {
            if (deed == null || cast == null) yield break;
            foreach (CharacterDefinition c in cast)
            {
                if (c == null || !c.tracked || c.kind == CharacterKind.Player) continue;
                if (deed.learners == DeedLearners.Everyone || c.kind == CharacterKind.Staff) yield return c.id;
            }
        }

        /// <summary>The deeds a fact commits.</summary>
        public static IEnumerable<DeedDefinition> DeedsFor(DeedSource source, IEnumerable<DeedDefinition> deeds)
        {
            if (source == DeedSource.None || deeds == null) yield break;
            foreach (DeedDefinition d in deeds)
                if (d != null && d.source == source) yield return d;
        }

        /// <summary>The Love/Hate faction a deed targets.</summary>
        public static string TargetFaction(DeedTarget target) => target == DeedTarget.Village ? StoryFactions.Village : StoryFactions.Tavern;
    }

    /// <summary>The Love/Hate factions that aren't characters: the places deeds are done for.</summary>
    public static class StoryFactions
    {
        /// <summary>Tally Ho!</summary>
        public const string Tavern = "tavern";
        /// <summary>Kariaston.</summary>
        public const string Village = "village";
        public const string Player = CharacterIds.Player;

        /// <summary>The relationship traits: Love/Hate's own Affinity, and Respect (4g's one addition).</summary>
        public const string Affinity = "Affinity";
        public const string Respect = "Respect";
    }
}
