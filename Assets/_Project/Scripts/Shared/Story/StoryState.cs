using System;
using System.Collections.Generic;

namespace Hearthdelve.Shared.Story
{
    /// <summary>
    /// The player's character-creation choices (4g). Saves from before 4g get the defaults: Bram in the townsfolk's clothes.
    /// Character creation (Step 4) adds the palette choices.
    /// </summary>
    [Serializable]
    public sealed class PlayerProfile
    {
        public const string DefaultName = "Bram";
        public const string DefaultBody = "townsfolk";

        public string name = DefaultName;
        public string body = DefaultBody;
        /// <summary>
        /// 4g Checkpoint B: the keeper's colours, one Minifantasy ramp per channel ("keeper_hair=red;keeper_skin=brown"), in the
        /// furniture palettes' format. Empty: the body as drawn.
        /// </summary>
        public string palette = string.Empty;

        public PlayerProfile Clone() => new() { name = name, body = body, palette = palette ?? string.Empty };
    }

    /// <summary>
    /// Where the Act I opening has got to (4g Checkpoint B), saved by name. Explicit story state: never inferred from the day,
    /// bosses or furniture. A game saved before the opening existed is <see cref="Complete"/>.
    /// </summary>
    public enum OpeningStage
    {
        /// <summary>The keeper has just arrived at Tally Ho! (day 1, the tavern, walking): Orik and Boog, then the hatch down.</summary>
        Arrival,
        /// <summary>The first delve (the onboarding prompts).</summary>
        FirstDelve,
        /// <summary>Home from the first delve (day 1's night): Boog sees what came up.</summary>
        Homecoming,
        /// <summary>The first evening's service (day 2): the menu, cooking, serving, getting paid.</summary>
        FirstEvening,
        /// <summary>The opening is over (Boog has asked about his bomb).</summary>
        Complete,
    }

    /// <summary>One relationship value, by stable ids and trait name: how <see cref="judge"/> feels about <see cref="subject"/>.</summary>
    [Serializable]
    public sealed class RelationshipValueData
    {
        public string judge;
        public string subject;
        public string trait;
        public float value;
    }

    /// <summary>
    /// A remembered deed: who remembers it, what it was, how often, and the game day it's forgotten after
    /// (<see cref="RelationshipRules.MemoryExpires"/>). Days, not seconds, so pausing or idling never ages a memory.
    /// </summary>
    [Serializable]
    public sealed class SocialMemoryData
    {
        public string judge;
        public string deed;
        public string actor;
        public string target;
        public int count = 1;
        public float impact;
        public float pleasure;
        public float expires;
    }

    /// <summary>
    /// Relationships as Hearth &amp; Hollows saves them (4g): the values that differ from where characters start, and the
    /// memories, keyed by stable ids and trait names, never by Love/Hate's positional format or faction numbers. The adapter
    /// fills it from Love/Hate before a save and gives it back after a load.
    /// </summary>
    [Serializable]
    public sealed class RelationshipData
    {
        public List<RelationshipValueData> values = new();
        public List<SocialMemoryData> memories = new();

        public bool IsEmpty => (values == null || values.Count == 0) && (memories == null || memories.Count == 0);
    }

    /// <summary>
    /// The story's persistent state (4g), held in <c>GameState</c> and saved by <c>SaveSystem</c> like everything else. Hearth &amp;
    /// Hollows owns the opening flag, the player's profile and the relationships; the Dialogue System's variables and Quest
    /// Machine's journal are kept as the strings their adapters record, opaque to everything else.
    /// </summary>
    public sealed class StoryState
    {
        /// <summary>Where the Act I opening is (4g Checkpoint B). A bare state (tests, tools) has none ahead.</summary>
        public OpeningStage Opening { get; set; } = OpeningStage.Complete;

        /// <summary>The Act I opening has been played (or skipped, for a save from before it existed).</summary>
        public bool OpeningComplete
        {
            get => Opening == OpeningStage.Complete;
            set => Opening = value ? OpeningStage.Complete : OpeningStage.Arrival;
        }

        /// <summary>The keeper was made at character creation (or is a legacy keeper, Bram as before). Continue never reopens creation.</summary>
        public bool CreationComplete { get; set; } = true;

        /// <summary>Onboarding prompts already shown (by id, <see cref="OnboardingHints"/>): each is shown once.</summary>
        public HashSet<string> SeenHints { get; } = new();
        public PlayerProfile Player { get; set; } = new();
        /// <summary>The Dialogue System's recorded state (its Lua variables): written and read only by the dialogue adapter.</summary>
        public string Dialogue { get; set; } = string.Empty;
        /// <summary>Quest Machine's recorded journal: written and read only by the quest adapter.</summary>
        public string Quests { get; set; } = string.Empty;
        public RelationshipData Relationships { get; set; } = new();
    }
}
