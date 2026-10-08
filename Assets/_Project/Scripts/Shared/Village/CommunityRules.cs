using System.Collections.Generic;

namespace Hearthdelve.Shared.Village
{
    /// <summary>
    /// The community's story beats and evenings (4h Checkpoint D), pure: when Gimp first comes up through the hatch in the keeper's
    /// room, and which familiar faces come to dinner.
    /// </summary>
    public static class CommunityRules
    {
        /// <summary>Gimp's first night (once per save): <c>StoryState.SeenHints</c> holds it, so the save format doesn't change.</summary>
        public const string GimpIntro = "beat:gimp_intro";

        /// <summary>
        /// The first morning that follows a delve of the keeper's own (day 3: the opening's delve is night 1, the first free day's
        /// is night 2), on waking (the day's first minutes, not a mid-day Continue), once. Every morning in the daily loop comes after
        /// a completed delve, so a keeper who hasn't been below never meets him.
        /// </summary>
        public static bool GimpIntroDue(bool openingComplete, int day, bool seen, int minute, int dayStartMinute) =>
            openingComplete && !seen && day >= 3 && minute <= dayStartMinute + 5;

        /// <summary>A named villager who may come to dinner: how often, each evening (seeded).</summary>
        public readonly struct Patron
        {
            public readonly string Character;
            public readonly float Chance;

            public Patron(string character, float chance)
            {
                Character = character;
                Chance = chance;
            }
        }

        const uint k_Patrons = 0x50415452; // "PATR"

        /// <summary>
        /// Tonight's familiar faces, in arrival order: each candidate rolls (seeded by the world and the day, so a reload keeps the
        /// same evening), at most <paramref name="max"/>, each at most once. Usually none or one; two now and then.
        /// </summary>
        public static List<string> Tonight(int seed, int day, IReadOnlyList<Patron> candidates, int max = 2)
        {
            var tonight = new List<string>();
            if (candidates == null) return tonight;
            for (int i = 0; i < candidates.Count && tonight.Count < max; i++)
            {
                Patron p = candidates[i];
                if (string.IsNullOrEmpty(p.Character) || tonight.Contains(p.Character)) continue;
                if (VillageDays.Unit(seed, day, k_Patrons + (uint)i * 7919u) < p.Chance) tonight.Add(p.Character);
            }
            // Arrival order varies with the evening, not always the first listed.
            if (tonight.Count == 2 && VillageDays.Unit(seed, day, k_Patrons ^ 0xFFu) < 0.5f) tonight.Reverse();
            return tonight;
        }
    }
}
