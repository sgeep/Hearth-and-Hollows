using System.Text.RegularExpressions;

namespace Hearthdelve.Shared.Characters
{
    /// <summary>Who a character is, as far as the story is concerned (GDD §2.8): the one abstraction every person shares.</summary>
    public enum CharacterKind
    {
        /// <summary>The keeper (Bram by default): named and dressed at character creation, never a Dialogue System speaker with a portrait.</summary>
        Player,
        /// <summary>The tavern's own people (Orik, Boog).</summary>
        Staff,
        /// <summary>A named Kariaston villager (4h): authored, persistent.</summary>
        Villager,
        /// <summary>A generated Visitor: transient, never saved, unless promoted (then a <see cref="Resident"/> or a persistent Visitor record).</summary>
        Visitor,
        /// <summary>A Visitor who settled on one of the village's plots (Phase 5): persistent, generated look, authored modular dialogue.</summary>
        Resident,
        /// <summary>Anyone else the story needs (Old Tamsin, Ser Aldric Vane).</summary>
        Story,
    }

    /// <summary>
    /// Stable character ids (4g): what saves, Dialogue System actors, Love/Hate factions, quests and gameplay facts use. An id never
    /// changes once shipped, even when the character is renamed: Boog's is still <c>gunta</c>.
    /// </summary>
    public static class CharacterIds
    {
        public const string Player = "player";
        /// <summary>Boog, the cook (renamed from Gunta on 2026-10-06; the id stays).</summary>
        public const string Boog = "gunta";
        /// <summary>Orik, the dwarf server and bookkeeper (he replaced Pip on 2026-10-06; the id stays).</summary>
        public const string Orik = "pip";

        /// <summary>Generated Visitors are <c>visitor/&lt;day&gt;/&lt;visit&gt;</c>: unique for the evening, never saved.</summary>
        public const string VisitorPrefix = "visitor/";

        static readonly Regex k_Authored = new("^[a-z][a-z0-9_]*$");
        static readonly Regex k_Visitor = new("^visitor/[0-9]+/[0-9]+$");

        public static string Visitor(int day, int visitId) => $"{VisitorPrefix}{day}/{visitId}";

        public static bool IsVisitor(string id) => id != null && k_Visitor.IsMatch(id);

        /// <summary>An authored id: lower case, digits and underscores, starting with a letter (the Dialogue System's actor field and Love/Hate's faction name).</summary>
        public static bool IsAuthored(string id) => id != null && k_Authored.IsMatch(id);

        public static bool IsValid(string id) => IsAuthored(id) || IsVisitor(id);
    }
}
