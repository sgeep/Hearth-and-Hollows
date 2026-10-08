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
        /// <summary>Anyone else the story needs (Old Phi, Ser Aldric Vane).</summary>
        Story,
        /// <summary>4h Checkpoint D: someone who lives in the Hollows and isn't an enemy (Gimp); never shown as a category.</summary>
        Hollower,
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
        /// <summary>
        /// The voice of things looked at (4h): a speaker with no name and no portrait, so a line like "Phi's chair" can be written
        /// in the node editor and shown in the dialogue box. Not a person; never tracked.
        /// </summary>
        public const string Narration = "narration";

        /// <summary>
        /// Musashi (2026-10-07, the owner's canon): an elf who keeps the Kariaston market cart. Once of the Fortunate Five with Phi and
        /// Grim, a friend of Orik's; a curse from the Hollows took his taste. His brother Toshi is missing below (a later quest).
        /// </summary>
        public const string Musashi = "musashi";

        // 4h Checkpoint C: Kariaston's people (GDD §2.10; their canon is locked there, their routines in their schedules).
        /// <summary>Maximo: Kariaston's founder and watchman over the Hollows; Karias was his apprentice.</summary>
        public const string Maximo = "maximo";
        /// <summary>Kaloren Frosthand: the kind wizard in the tower, who brings Ogrin herbs every third day.</summary>
        public const string Kaloren = "kaloren";
        /// <summary>Grim: a dwarf, a former delver, who found Ogrin below and raised him. His livelihood is still open.</summary>
        public const string Grim = "grim";
        /// <summary>Ogrin: a boy of about ten, curious and often unwell; he calls Grim "Grim".</summary>
        public const string Ogrin = "ogrin";
        /// <summary>Bart: an orc bard, the first Visitor who stayed; lives in the painted wagon on the green.</summary>
        public const string Bart = "bart";
        /// <summary>4h Checkpoint D: Gimp, a half-elf ranger who lives in and around the Hollows and comes up the hatch to see Boog.</summary>
        public const string Gimp = "gimp";

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
