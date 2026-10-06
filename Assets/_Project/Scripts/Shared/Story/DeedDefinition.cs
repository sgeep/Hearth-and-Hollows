using UnityEngine;

namespace Hearthdelve.Shared.Story
{
    /// <summary>The gameplay fact a deed comes from (plan §7). <see cref="None"/>: only dialogue or a quest commits it.</summary>
    public enum DeedSource
    {
        None,
        /// <summary><c>TrophyDisplayed</c>: a boss trophy hung for the first time.</summary>
        TrophyDisplayed,
    }

    /// <summary>What a deed is done to or for: Love/Hate's target faction, whose friends are pleased.</summary>
    public enum DeedTarget
    {
        Tavern,
        Village,
        /// <summary>One character (4g Checkpoint B): a deed done for them (returning Boog's bomb); they care about themselves.</summary>
        Character,
    }

    /// <summary>Who learns of a deed (the relationship adapter decides; nobody needs to see it happen).</summary>
    public enum DeedLearners
    {
        /// <summary>The tavern's staff: it happened at home.</summary>
        Staff,
        /// <summary>Every tracked character.</summary>
        Everyone,
        /// <summary>Only the character it was done for (a <see cref="DeedTarget.Character"/> deed).</summary>
        Target,
    }

    /// <summary>
    /// A socially meaningful thing the player did (4g): Hearth &amp; Hollows decides which gameplay facts are deeds, who hears of
    /// them, and how much they move Respect; Love/Hate evaluates Affinity and keeps the memory. Its <see cref="id"/> is the
    /// Love/Hate deed tag and what dialogue asks about (<c>HH_Remembers("gunta", "displayed_trophy")</c>).
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Deed", fileName = "Deed_")]
    public sealed class DeedDefinition : ScriptableObject
    {
        [Tooltip("Stable id: the Love/Hate deed tag, and what dialogue and saves name.")]
        public string id;
        [Tooltip("The fact that commits it (None: dialogue or quests only).")]
        public DeedSource source;
        public DeedTarget target = DeedTarget.Tavern;
        [Tooltip("The character it's done for, when the target is Character (their id: \"gunta\" is Boog).")]
        public string character;
        public DeedLearners learners = DeedLearners.Staff;
        [Tooltip("What it shows about the player: judges who value the same respect it more.")]
        public SocialTraits shows;
        [Range(-100, 100), Tooltip("Good (+) or bad (−) for the target: Love/Hate turns it into affinity for those who care about the target.")]
        public float impact = 20f;
        [Range(-50, 50), Tooltip("Respect it earns from a judge whose values match what it shows (scaled down by mismatch and repetition).")]
        public float respect = 10f;
        [Min(0), Tooltip("Game days it's remembered for (0: for good).")]
        public int memoryDays;
    }
}
