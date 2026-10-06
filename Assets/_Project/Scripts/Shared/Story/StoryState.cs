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

        public PlayerProfile Clone() => new() { name = name, body = body };
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
        /// <summary>The Act I opening has been played (or skipped, for a save from before 4g). A new game starts without it.</summary>
        public bool OpeningComplete { get; set; }
        public PlayerProfile Player { get; set; } = new();
        /// <summary>The Dialogue System's recorded state (its Lua variables): written and read only by the dialogue adapter.</summary>
        public string Dialogue { get; set; } = string.Empty;
        /// <summary>Quest Machine's recorded journal: written and read only by the quest adapter.</summary>
        public string Quests { get; set; } = string.Empty;
        public RelationshipData Relationships { get; set; } = new();
    }
}
